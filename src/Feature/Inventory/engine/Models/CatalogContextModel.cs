using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using System.Collections.Generic;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class CatalogContextModel
    {
        public string CatalogName { get; set; }
        public Sitecore.Commerce.Plugin.Catalog.Catalog Catalog { get; set; }
        public Dictionary<string, Category> CategoriesByName { get; set; }
        public Dictionary<string, Category> CategoriesBySitecoreId { get; set; }
    }
}
