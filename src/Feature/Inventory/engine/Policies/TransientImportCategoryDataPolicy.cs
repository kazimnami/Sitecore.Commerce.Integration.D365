using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class TransientImportCategoryDataPolicy : Policy
    {
        public IList<CatalogAssociationModel> CatalogAssociationList { get; set; } = new List<CatalogAssociationModel>();
        public IList<CategoryAssociationModel> CategoryAssociationList { get; set; } = new List<CategoryAssociationModel>();
        public IList<CatalogItemParentAssociationModel> ParentAssociationsToCreateList { get; set; } = new List<CatalogItemParentAssociationModel>();
        public IList<CatalogItemParentAssociationModel> ParentAssociationsToRemoveList { get; set; } = new List<CatalogItemParentAssociationModel>();
    }
}
