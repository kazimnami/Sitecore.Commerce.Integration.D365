using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class TransformImportToCategoryCommand : CommerceCommand
    {
        private const string CatalogNameIndex = "EcoResCategoryHierarchy_Name"; //"CategoryHierarchy";
        private const string CategoryNameIndex = "AxRecId";
        private const string ParentCategoryNameIndex = "ParentCategory";
        private const string CategoryDisplayNameIndex = "Name";

        public TransformImportToCategoryCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<IEnumerable<Category>> Process(CommerceContext commerceContext, JToken importRawLines)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                var importItems = new List<Category>();
                var transientDataList = new List<TransientImportCategoryDataPolicy>();
                foreach (var rawLine in importRawLines)
                {
                    var item = TransformCore(commerceContext, rawLine, importItems);
                    TransformParentAssociations(rawLine, item, transientDataList);
                }

                TransformParentCatalogData(importItems, transientDataList);
                await TransformCatalog(commerceContext, importItems);
                await TransformCategory(commerceContext, transientDataList, importItems);

                return importItems;
            }
        }

        private Category TransformCore(CommerceContext commerceContext, JToken rawLine, List<Category> importItems)
        {
            var catalogName = rawLine[CatalogNameIndex].ToString();
            var name = rawLine[CategoryNameIndex].ToString();
            var id = name.ToCategoryId(catalogName);

            var item = importItems.FirstOrDefault(i => i.Id.Equals(id));
            if (item == null)
            {
                item = new Category();
                item.Name = name;
                item.Id = id;
                item.FriendlyId = item.Name.ToCategoryFriendlyId(catalogName);
                item.SitecoreId = GuidUtils.GetDeterministicGuidString(item.Id);
                item.DisplayName = rawLine[CategoryDisplayNameIndex].ToString();

                var component = item.GetComponent<ListMembershipsComponent>();
                component.Memberships.Add(string.Format("{0}", CommerceEntity.ListName<Category>()));
                component.Memberships.Add(commerceContext.GetPolicy<KnownCatalogListsPolicy>().CatalogItems);

                importItems.Add(item);
            }

            return item;
        }

        private void TransformParentAssociations(JToken rawLine, Category item, List<TransientImportCategoryDataPolicy> transientDataList)
        {
            var catalogName = rawLine[CatalogNameIndex].ToString();
            var parentCategoryName = rawLine[ParentCategoryNameIndex].ToString();

            var data = item.GetPolicy<TransientImportCategoryDataPolicy>();

            // Set to 0 in D365 when no parent category
            if (!parentCategoryName.Equals("0"))
            {
                // Category Association
                var categoryAssociation = data.CategoryAssociationList.FirstOrDefault(a => a.CatalogName.Equals(catalogName) && a.CategoryName.Equals(parentCategoryName));
                if (categoryAssociation == null)
                {
                    data.ParentAssociationsToCreateList.Add(new CatalogItemParentAssociationModel(item.Id, catalogName.ToEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>(), parentCategoryName.ToCategoryId(catalogName)));
                    data.CategoryAssociationList.Add(new CategoryAssociationModel { CatalogName = catalogName, CategoryName = parentCategoryName });
                }
            }

            // Catalog Association
            var catalogAssociation = data.CatalogAssociationList.FirstOrDefault(a => a.Name.Equals(catalogName));
            if (catalogAssociation == null)
            {
                data.CatalogAssociationList.Add(new CatalogAssociationModel { Name = catalogName });
            }
        }

        private void TransformParentCatalogData(List<Category> importItems, List<TransientImportCategoryDataPolicy> transientDataList)
        {
            foreach (var item in importItems)
            {
                var data = item.GetPolicy<TransientImportCategoryDataPolicy>();
                foreach(var catalogAssociation in data.CatalogAssociationList)
                {
                    if (!data.CategoryAssociationList.Any(a => a.CatalogName.Equals(catalogAssociation.Name)))
                    {
                        data.ParentAssociationsToCreateList.Add(new CatalogItemParentAssociationModel(item.Id, catalogAssociation.Name.ToEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>(), catalogAssociation.Name.ToEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>()));
                        catalogAssociation.IsParent = true;
                    }
                }

                transientDataList.Add(data);
            }
        }

        private async Task TransformCatalog(CommerceContext commerceContext, List<Category> importItems)
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
                var transientData = item.GetPolicy<TransientImportCategoryDataPolicy>();

                var catalogList = new List<string>();
                foreach (var association in transientData.CatalogAssociationList)
                {
                    allCatalogsDictionary.TryGetValue(association.Name, out Sitecore.Commerce.Plugin.Catalog.Catalog catalog);
                    if (catalog != null)
                    {
                        catalogList.Add(catalog.SitecoreId);
                    }
                    else
                    {
                        commerceContext.Logger.LogWarning($"Warning, Category with id '{item.Id}' attempting import into catalog '{association.Name}' which doesn't exist.");
                    }
                }

                var parentCatalogList = new List<string>();
                foreach (var association in transientData.ParentAssociationsToCreateList)
                {
                    if (association.ParentId.IsEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>())
                    {
                        allCatalogsDictionary.TryGetValue(association.ParentId.RemoveIdPrefix<Sitecore.Commerce.Plugin.Catalog.Catalog>(), out Sitecore.Commerce.Plugin.Catalog.Catalog catalog);
                        if (catalog != null)
                        {
                            parentCatalogList.Add(catalog.SitecoreId);
                        }
                        else
                        {
                            commerceContext.Logger.LogWarning($"Warning, Category with id {item.Id} attempting import into catalog {association.ParentId} which doesn't exist.");
                        }
                    }
                }

                // Primary responsibility of this method is to set these IDs
                item.CatalogToEntityList = catalogList.Any() ? string.Join("|", catalogList) : null;
                item.ParentCatalogList = parentCatalogList.Any() ? string.Join("|", parentCatalogList) : null;
            }
        }

        private async Task TransformCategory(CommerceContext commerceContext, List<TransientImportCategoryDataPolicy> transientDataList, List<Category> importItems)
        {
            var catalogNameList = transientDataList.SelectMany(d => d.CatalogAssociationList).Select(a => a.Name).Distinct().ToList();
            var catalogContextList = await Command<GetCatalogContextCommand>().Process(commerceContext, catalogNameList, false);

            foreach (var item in importItems)
            {
                var transientData = item.GetPolicy<TransientImportCategoryDataPolicy>();

                var itemsCategoryList = new List<string>();
                foreach (var categoryAssociation in transientData.CategoryAssociationList)
                {
                    var catalogContext = catalogContextList.FirstOrDefault(c => c.CatalogName.Equals(categoryAssociation.CatalogName));
                    if (catalogContext == null) throw new Exception($"Error, catalog not found {categoryAssociation.CatalogName}. This should not happen.");

                    // Find category, existing
                    catalogContext.CategoriesByName.TryGetValue(categoryAssociation.CategoryName, out Category category);
                    if (category == null)
                    {
                        // Find category, new
                        category = importItems.FirstOrDefault(c => c.Name.Equals(categoryAssociation.CategoryName));
                    }

                    if (category != null)
                    {
                        // Found category
                        itemsCategoryList.Add(category.SitecoreId);
                        transientData.ParentAssociationsToCreateList.Add(new CatalogItemParentAssociationModel(item.Id, categoryAssociation.CatalogName.ToEntityId<Sitecore.Commerce.Plugin.Catalog.Catalog>(), category));
                    }
                    else
                    {
                        commerceContext.Logger.LogWarning($"Warning, Category with id {item.Id} attempting import into category {categoryAssociation.CategoryName} which doesn't exist.");
                    }
                }

                // Primary responsibility of this method is to set these IDs
                item.ParentCategoryList = itemsCategoryList.Any() ? string.Join("|", itemsCategoryList) : null;
            }
        }
    }
}
