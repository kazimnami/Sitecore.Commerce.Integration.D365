using Sitecore.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class TransientImportSellableItemDataPolicy : Policy
    {
        public IList<CatalogAssociationModel> CatalogAssociationList { get; set; } = new List<CatalogAssociationModel>();
        public IList<CategoryAssociationModel> CategoryAssociationList { get; set; } = new List<CategoryAssociationModel>();
        public IList<string> ImageNameList { get; set; } = new List<string>();

        public IList<CatalogItemParentAssociationModel> ParentAssociationsToCreateList { get; set; } = new List<CatalogItemParentAssociationModel>();
        public IList<CatalogItemParentAssociationModel> ParentAssociationsToRemoveList { get; set; } = new List<CatalogItemParentAssociationModel>();

    }
}
