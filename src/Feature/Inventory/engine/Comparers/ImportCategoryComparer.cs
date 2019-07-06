using Sitecore.Commerce.Plugin.Catalog;
using System;
using System.Collections.Generic;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ImportCategoryComparer : IEqualityComparer<Category>
    {
        private readonly CategoryComparerConfiguration Configuration;

        public ImportCategoryComparer(CategoryComparerConfiguration configuration)
        {
            Configuration = configuration;
        }

        public bool Equals(Category x, Category y)
        {
            if (x == null || y == null) return false;

            switch (Configuration)
            {
                case CategoryComparerConfiguration.ById:
                    return x.Id == y.Id;

                case CategoryComparerConfiguration.ByData:
                    return x.Id == y.Id
                        && x.Name == y.Name
                        && x.DisplayName == y.DisplayName;

                default:
                    throw new ArgumentException($"Comparer configuration cannot be handled");
            }
        }

        public int GetHashCode(Category obj)
        {
            if (obj == null) return base.GetHashCode();

            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            unchecked
            {
                int hash = 17;

                switch (Configuration)
                {
                    case CategoryComparerConfiguration.ById:
                        if (obj.Id != null) hash = hash * 23 + obj.Id.GetHashCode();
                        break;

                    case CategoryComparerConfiguration.ByData:
                        if (obj.Id != null) hash = hash * 23 + obj.Id.GetHashCode();
                        if (obj.Name != null) hash = hash * 23 + obj.Name.GetHashCode();
                        if (obj.DisplayName != null) hash = hash * 23 + obj.DisplayName.GetHashCode();
                        break;

                    default:
                        throw new ArgumentException($"Comparer configuration cannot be handled");
                }

                return hash;
            }
        }
    }

    public enum CategoryComparerConfiguration
    {
        ById,
        ByData
    }
}
