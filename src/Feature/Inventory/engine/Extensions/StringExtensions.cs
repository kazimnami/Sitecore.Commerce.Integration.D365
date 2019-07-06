using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public static class StringExtensions
    {
        public static string CatalogNameFromCategoryId(this string categoryId)
        {
            Condition.Requires(categoryId, nameof(categoryId)).IsNotNullOrWhiteSpace();
            string[] strArray = categoryId.RemoveIdPrefix<Category>().Split('-');
            if (strArray.Length <= 1)
                return strArray[0];
            return strArray[0];
        }
    }
}
