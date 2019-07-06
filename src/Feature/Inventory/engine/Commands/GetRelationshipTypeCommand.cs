using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using System;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class GetRelationshipTypeCommand : CommerceCommand
    {
        public GetRelationshipTypeCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public string Process(CommerceContext commerceContext, string parentId, string itemId)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                if (parentId.StartsWith(CommerceEntity.IdPrefix<Sitecore.Commerce.Plugin.Catalog.Catalog>()))
                {
                    if (itemId.StartsWith(CommerceEntity.IdPrefix<SellableItem>())) return CatalogConstants.CatalogToSellableItem;
                    else if (itemId.StartsWith(CommerceEntity.IdPrefix<Category>())) return CatalogConstants.CatalogToCategory;
                }
                else if (parentId.StartsWith(CommerceEntity.IdPrefix<Category>()))
                {
                    if (itemId.StartsWith(CommerceEntity.IdPrefix<SellableItem>())) return CatalogConstants.CategoryToSellableItem;
                    else if (itemId.StartsWith(CommerceEntity.IdPrefix<Category>())) return CatalogConstants.CategoryToCategory;
                }
                else if (parentId.StartsWith(CommerceEntity.IdPrefix<InventorySet>()))
                {
                    if (itemId.StartsWith(CommerceEntity.IdPrefix<InventoryInformation>())) return InventoryConstants.InventorySetToInventoryInformation;
                }
                throw new Exception($"Error, can not determine relationship type between ItemId '{itemId}' and ParentId '{parentId}'.");
            }
        }
    }
}
