using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SampleIntegrationD365.Foundation.D365.Engine;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class TransformImportToSellableItemsCommand : CommerceCommand
    {
        // Core
        private const string ProductIdIndex = "ItemNumber";
        private const string ProductNameIndex = "SearchName";
        private const string DisplayNameIndex = "ProductName";
        private const string TypeOfGoodIndex = "ItemModelGroupId";
        private const string ListPriceIndex = "SalesPrice";

        // Product to Category Association
        private const string ProductNumberIndex = "ProductNumber";
        private const string ProductCatalogNameIndex = "ProductCategoryHierarchyName";
        private const string ProductCategoryDisplayNameIndex = "ProductCategoryName";

        // Category
        private const string CategoryDisplayNameIndex = "Name";
        private const string CategoryNameIndex = "AxRecId";
        private const string CategoryCatalogName = "EcoResCategoryHierarchy_Name";

        public TransformImportToSellableItemsCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<IEnumerable<SellableItem>> Process(CommerceContext commerceContext, JToken importRawLines)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                var importItems = new List<SellableItem>();
                var transientDataList = new List<TransientImportSellableItemDataPolicy>();

                foreach (var rawLine in importRawLines)
                {
                    var item = TransformCore(commerceContext, rawLine, importItems);
                }

                await TransformD365Category(commerceContext, importItems, transientDataList);
                await TransformCatalog(commerceContext, importItems);
                await TransformCategory(commerceContext, transientDataList, importItems);

                return importItems;
            }
        }

        private SellableItem TransformCore(CommerceContext commerceContext, JToken rawLine, List<SellableItem> importItems)
        {
            var productId = rawLine[ProductIdIndex].ToString();
            var id = productId.ToEntityId<SellableItem>();

            var item = importItems.FirstOrDefault(i => i.Id.Equals(id));
            if (item == null)
            {
                item = new SellableItem();
                item.ProductId = productId;
                item.Id = id;
                item.FriendlyId = item.ProductId;
                item.Name = rawLine[ProductNameIndex].ToString();
                item.SitecoreId = GuidUtils.GetDeterministicGuidString(item.Id);
                item.DisplayName = rawLine[DisplayNameIndex].ToString();
                item.TypeOfGood = rawLine[TypeOfGoodIndex].ToString();

                var listPricePolicy = item.GetPolicy<ListPricingPolicy>();
                listPricePolicy.AddPrice(new Money
                {
                    CurrencyCode = "USD",
                    Amount = decimal.Parse(rawLine[ListPriceIndex].ToString())
                });

                var component = item.GetComponent<ListMembershipsComponent>();
                component.Memberships.Add(string.Format("{0}", CommerceEntity.ListName<SellableItem>()));
                component.Memberships.Add(commerceContext.GetPolicy<KnownCatalogListsPolicy>().CatalogItems);

                importItems.Add(item);
            }

            return item;
        }

        private async Task TransformD365Category(CommerceContext commerceContext, List<SellableItem> importItems, List<TransientImportSellableItemDataPolicy> transientDataList)
        {
            var d365Response = await commerceContext.GetPolicy<ConnectionPolicy>().GetCategories();
            var d365CategoriesLookup = (d365Response).ToLookup(c => c[CategoryDisplayNameIndex].ToString());
            var d365ProductCategoryAssignmentsLookup = (await commerceContext.GetPolicy<ConnectionPolicy>().GetProductCategoryAssignments()).ToLookup(a => a[ProductNumberIndex].ToString());

            var defaultCatalogName = d365Response.First()[CategoryCatalogName].ToString();

            foreach (var item in importItems)
            {
                var data = item.GetPolicy<TransientImportSellableItemDataPolicy>();

                var assignmentList = d365ProductCategoryAssignmentsLookup[item.ProductId];
                if (assignmentList != null && !assignmentList.Count().Equals(0))
                {
                    foreach (var assignment in assignmentList)
                    {
                        var categoryDisplayName = assignment[ProductCategoryDisplayNameIndex].ToString();
                        var d365CategoryList = d365CategoriesLookup[categoryDisplayName];
                        if (d365CategoryList == null || d365CategoryList.Count().Equals(0))
                        {
                            throw new Exception($"Error, product with ID '{item.ProductId}' is a assigned to a category '{categoryDisplayName}' that can't be found.");
                        }

                        data.CategoryAssociationList.Add(new CategoryAssociationModel
                        {
                            CatalogName = assignment[ProductCatalogNameIndex].ToString(),
                            CategoryName = d365CategoryList.First()[CategoryNameIndex].ToString()
                        });

                        Console.WriteLine($"Product Id '{item.ProductId}' associating to '{categoryDisplayName}'.");
                        await commerceContext.AddMessage(commerceContext.GetPolicy<KnownResultCodes>().Information, null, null, $"Product Id '{item.ProductId}' associating to '{categoryDisplayName}'.");
                    }

                    foreach (var catalogName in data.CategoryAssociationList.Select(a => a.CatalogName).Distinct())
                    {
                        data.CatalogAssociationList.Add(new CatalogAssociationModel { Name = catalogName });
                    }
                }
                else
                {
                    data.CatalogAssociationList.Add(new CatalogAssociationModel { Name = defaultCatalogName });

                }

                transientDataList.Add(data);
            }
        }

        private async Task TransformCatalog(CommerceContext commerceContext, List<SellableItem> importItems)
        {
            var allCatalogs = commerceContext.GetObject<IEnumerable<Sitecore.Commerce.Plugin.Catalog.Catalog>>();
            if (allCatalogs == null)
            {
                allCatalogs = await Command<GetCatalogsCommand>().Process(commerceContext);
                commerceContext.AddObject(allCatalogs);
            }

            var allCatalogsDictionary = allCatalogs.ToDictionary(c => c.Name);

            foreach (var item in importItems)
            {
                var transientData = item.GetPolicy<TransientImportSellableItemDataPolicy>();

                if (transientData.CatalogAssociationList == null || transientData.CatalogAssociationList.Count().Equals(0))
                    throw new Exception($"{item.Name}: needs to have at least one definied catalog");

                var catalogList = new List<string>();
                var parentCatalogList = new List<string>();
                foreach (var catalogAssociation in transientData.CatalogAssociationList)
                {
                    allCatalogsDictionary.TryGetValue(catalogAssociation.Name, out Sitecore.Commerce.Plugin.Catalog.Catalog catalog);
                    if (catalog != null)
                    {
                        catalogList.Add(catalog.SitecoreId);
                        item.GetComponent<CatalogsComponent>().ChildComponents.Add(new CatalogComponent { Name = catalogAssociation.Name });

                        if (catalogAssociation.IsParent)
                        {
                            transientData.ParentAssociationsToCreateList.Add(new CatalogItemParentAssociationModel(item.Id, catalog.Id, catalog));
                            parentCatalogList.Add(catalog.SitecoreId);
                        }
                    }
                    else
                    {
                        commerceContext.Logger.LogWarning($"Warning, Product with id {item.ProductId} attempting import into catalog {catalogAssociation.Name} which doesn't exist.");
                    }
                }

                item.CatalogToEntityList = catalogList.Any() ? string.Join("|", catalogList) : null;
                item.ParentCatalogList = parentCatalogList.Any() ? string.Join("|", parentCatalogList) : null;
            }
        }

        private async Task TransformCategory(CommerceContext commerceContext, List<TransientImportSellableItemDataPolicy> transientDataList, List<SellableItem> importItems)
        {
            var catalogNameList = transientDataList.SelectMany(d => d.CatalogAssociationList).Select(a => a.Name).Distinct();
            var catalogContextList = await Command<GetCatalogContextCommand>().Process(commerceContext, catalogNameList);

            foreach (var item in importItems)
            {
                var transientData = item.GetPolicy<TransientImportSellableItemDataPolicy>();

                var itemsCategoryList = new List<string>();
                foreach (var categoryAssociation in transientData.CategoryAssociationList)
                {
                    var catalogContext = catalogContextList.FirstOrDefault(c => c.CatalogName.Equals(categoryAssociation.CatalogName));
                    if (catalogContext == null) throw new Exception($"Error, catalog not found {categoryAssociation.CatalogName}. This should not happen.");

                    // Find category
                    catalogContext.CategoriesByName.TryGetValue(categoryAssociation.CategoryName, out Category category);
                    if (category != null)
                    {
                        // Found category
                        itemsCategoryList.Add(category.SitecoreId);
                        transientData.ParentAssociationsToCreateList.Add(new CatalogItemParentAssociationModel(item.Id, categoryAssociation.CatalogName.ToEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>(), category));
                    }
                    else
                    {
                        commerceContext.Logger.LogWarning($"Warning, Product with id {item.ProductId} attempting import into category {categoryAssociation.CategoryName} which doesn't exist.");
                    }
                }

                item.ParentCategoryList = itemsCategoryList.Any() ? string.Join("|", itemsCategoryList) : null;
            }
        }


    }
}
