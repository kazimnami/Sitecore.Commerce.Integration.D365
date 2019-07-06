using Sitecore.Commerce.Plugin.Catalog;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class CatalogItemParentAssociationModel : ParentAssociationModel
    {
        public string CatalogId { get; set; }
        public string ParentSitecoreId { get; set; }

        public CatalogItemParentAssociationModel() { }

        public CatalogItemParentAssociationModel(string itemId, string catalogId, CatalogItemBase parent)
        {
            ItemId = itemId;
            CatalogId = catalogId;
            ParentId = parent.Id;
            ParentSitecoreId = parent.SitecoreId;
        }

        public CatalogItemParentAssociationModel(string itemId, string catalogId, string parentId)
        {
            ItemId = itemId;
            CatalogId = catalogId;
            ParentId = parentId;
        }
    }
}
