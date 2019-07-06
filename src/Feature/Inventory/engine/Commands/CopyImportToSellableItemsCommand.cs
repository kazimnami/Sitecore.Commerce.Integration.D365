using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class CopyImportToSellableItemsCommand : CommerceCommand
    {
        public CopyImportToSellableItemsCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task Process(CommerceContext commerceContext, IEnumerable<SellableItem> allImportItems, IEnumerable<SellableItem> changedItems)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                if (!changedItems.Any()) return;

                var catalogNameList = changedItems.SelectMany(i => i.GetComponent<CatalogsComponent>().ChildComponents).Select(c => c.Name).Distinct().ToList();
                var catalogContextList = await Command<GetCatalogContextCommand>().Process(commerceContext, catalogNameList);

                foreach (var item in changedItems)
                {
                    var itemNewData = allImportItems.FirstOrDefault(i => i.ProductId.Equals(item.ProductId));

                    if (itemNewData == null) throw new Exception($"Error, can't find product Id {item.ProductId} in import list. This should never happen.");

                    CopyCore(itemNewData, item);
                    CopyListPrice(itemNewData, item);
                    await CopyCategory(commerceContext, catalogContextList, itemNewData, item); // Must copy categories before catalog
                    CopyCatalog(itemNewData, item);
                    CopyImages(itemNewData, item);
                }
            }
        }

        private void CopyCore(SellableItem itemNewData, SellableItem item)
        {
            // These fields are not included
            // item.ProductId
            // item.Id
            // item.FriendlyId 
            // item.Name 
            // item.SitecoreId
            item.DisplayName = itemNewData.DisplayName;
            item.Description = itemNewData.Description;
            item.Brand = itemNewData.Brand;
            item.Manufacturer = itemNewData.Manufacturer;
            item.TypeOfGood = itemNewData.TypeOfGood;
            item.Tags = itemNewData.Tags;
        }

        private void CopyListPrice(SellableItem itemNewData, SellableItem item)
        {
            item.RemovePolicy(typeof(ListPricingPolicy));
            item.Policies.Add(itemNewData.GetPolicy<ListPricingPolicy>());
        }

        private async Task CopyCategory(CommerceContext commerceContext, IEnumerable<CatalogContextModel> catalogContextList, SellableItem itemNewData, SellableItem item)
        {
            var existingCategoryList = string.IsNullOrEmpty(item.ParentCategoryList) ? new string[0] : item.ParentCategoryList.Split('|');
            var newCategoryList = string.IsNullOrEmpty(itemNewData.ParentCategoryList) ? new string[0] : itemNewData.ParentCategoryList.Split('|');

            var associationsToCreate = newCategoryList.Except(existingCategoryList).ToList();
            var associationsToRemove = existingCategoryList.Except(newCategoryList).ToList();

            CopyCategoryAddressAdded(commerceContext, item, associationsToCreate);
            await CopyCategoryAddressRemoved(commerceContext, item, associationsToRemove);

            item.ParentCategoryList = itemNewData.ParentCategoryList;
        }

        private void CopyCategoryAddressAdded(CommerceContext commerceContext, SellableItem existingItem, List<string> associationsToCreate)
        {
            var transientData = existingItem.GetPolicy<TransientImportSellableItemDataPolicy>();
            transientData.ParentAssociationsToCreateList = transientData.ParentAssociationsToCreateList.Where(a => associationsToCreate.Contains(a.ParentSitecoreId)).ToList();
        }

        private async Task CopyCategoryAddressRemoved(CommerceContext commerceContext, SellableItem existingItem, List<string> associationsToRemove)
        {
            if (associationsToRemove == null || associationsToRemove.Count().Equals(0))
                return;

            var transientData = existingItem.GetPolicy<TransientImportSellableItemDataPolicy>();

            var allCatalogs = commerceContext.GetObject<IEnumerable<Sitecore.Commerce.Plugin.Catalog.Catalog>>();
            if (allCatalogs == null)
            {
                allCatalogs = await Command<GetCatalogsCommand>().Process(commerceContext);
                commerceContext.AddObject(allCatalogs);
            }

            var existingCatalogSitecoreIdList = existingItem.ParentCatalogList.Split('|');

            var catalogNameList = new List<string>();
            foreach (var catalogSitecoreId in existingCatalogSitecoreIdList)
            {
                var catalog = allCatalogs.FirstOrDefault(c => c.SitecoreId.Equals(catalogSitecoreId));
                catalogNameList.Add(catalog.Name);
            }

            var catalogContextList = await Command<GetCatalogContextCommand>().Process(commerceContext, catalogNameList);
            foreach (var categoryToRemoveSitecoreId in associationsToRemove)
            {
                bool found = false;
                foreach (var catalogContext in catalogContextList)
                {
                    catalogContext.CategoriesBySitecoreId.TryGetValue(categoryToRemoveSitecoreId, out Category category);

                    if (category != null)
                    {
                        transientData.ParentAssociationsToRemoveList.Add(new CatalogItemParentAssociationModel(existingItem.Id, catalogContext.Catalog.Id, category));

                        found = true;
                        break;
                    }
                }

                if (!found)
                    commerceContext.Logger.LogWarning($"Unable to find category with SitecoreId {categoryToRemoveSitecoreId}. We need to disacciate it from SellableItem {existingItem.Id}");
            }
        }

        private void CopyCatalog(SellableItem itemNewData, SellableItem item)
        {
            item.ParentCatalogList = itemNewData.ParentCatalogList;
        }

        private void CopyImages(SellableItem itemNewData, SellableItem item)
        {
            item.RemoveComponent(typeof(ImagesComponent));
            item.Components.Add(itemNewData.GetComponent<ImagesComponent>());
        }
    }
}
