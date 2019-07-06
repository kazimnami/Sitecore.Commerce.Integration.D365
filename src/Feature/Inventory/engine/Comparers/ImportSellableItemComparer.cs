using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ImportSellableItemComparer : IEqualityComparer<SellableItem>
    {
        private readonly SellableItemComparerConfiguration Configuration;
        private readonly MoneyComparer SellableItemMoneyComparer;

        public ImportSellableItemComparer(SellableItemComparerConfiguration configuration)
        {
            Configuration = configuration;
            SellableItemMoneyComparer = new MoneyComparer();
        }

        public bool Equals(SellableItem x, SellableItem y)
        {
            if (x == null || y == null) return false;

            switch (Configuration)
            {
                case SellableItemComparerConfiguration.ByProductId:
                    return x.ProductId == y.ProductId;

                case SellableItemComparerConfiguration.ByImportData:
                    return SellableItemCoreMemberEquality(x, y)
                        && ListPriceEquality(x.GetPolicy<ListPricingPolicy>(), y.GetPolicy<ListPricingPolicy>())
                        && ImagesEquality(x.GetComponent<ImagesComponent>(), y.GetComponent<ImagesComponent>());

                default:
                    throw new ArgumentException($"Comparer configuration cannot be handled");
            }
        }

        private bool SellableItemCoreMemberEquality(SellableItem x, SellableItem y)
        {
            return x.ProductId == y.ProductId
                && x.Name == y.Name
                && x.DisplayName == y.DisplayName
                && x.Description == y.Description
                && x.Brand == y.Brand
                && x.Manufacturer == y.Manufacturer
                && x.TypeOfGood == y.TypeOfGood
                && StringListEquality(x.ParentCatalogList, y.ParentCatalogList)
                && StringListEquality(x.ParentCategoryList, y.ParentCategoryList);
        }

        private bool StringListEquality(string x, string y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Split('|').OrderBy(i => i, StringComparer.OrdinalIgnoreCase).SequenceEqual(y.Split('|').OrderBy(i => i, StringComparer.OrdinalIgnoreCase));
        }

        private bool ListPriceEquality(ListPricingPolicy x, ListPricingPolicy y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.Prices == null && y.Prices == null) return true;
            if (x.Prices == null || y.Prices == null) return false;
            return x.Prices.OrderBy(p => p.CurrencyCode, StringComparer.OrdinalIgnoreCase).SequenceEqual(y.Prices.OrderBy(p => p.CurrencyCode, StringComparer.OrdinalIgnoreCase), SellableItemMoneyComparer);
        }

        private bool ImagesEquality(ImagesComponent x, ImagesComponent y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.Images == null && y.Images == null) return true;
            if (x.Images == null || y.Images == null) return false;
            return x.Images.OrderBy(i => i, StringComparer.OrdinalIgnoreCase).SequenceEqual(y.Images.OrderBy(i => i, StringComparer.OrdinalIgnoreCase));
        }

        public int GetHashCode(SellableItem obj)
        {
            if (obj == null) return base.GetHashCode();

            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            unchecked
            {
                int hash = 17;

                switch (Configuration)
                {
                    case SellableItemComparerConfiguration.ByProductId:
                        if (obj.ProductId != null) hash = hash * 23 + obj.ProductId.GetHashCode();
                        break;

                    case SellableItemComparerConfiguration.ByImportData:
                        if (obj.ProductId != null) hash = hash * 23 + obj.ProductId.GetHashCode();
                        if (obj.Name != null) hash = hash * 23 + obj.Name.GetHashCode();
                        if (obj.DisplayName != null) hash = hash * 23 + obj.DisplayName.GetHashCode();
                        if (obj.Description != null) hash = hash * 23 + obj.Description.GetHashCode();
                        if (obj.Brand != null) hash = hash * 23 + obj.Brand.GetHashCode();
                        if (obj.Manufacturer != null) hash = hash * 23 + obj.Manufacturer.GetHashCode();
                        if (obj.TypeOfGood != null) hash = hash * 23 + obj.TypeOfGood.GetHashCode();
                        if (obj.ParentCatalogList != null) obj.ParentCatalogList.Split('|').OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ForEach(t => hash = hash * 23 + t.GetHashCode());
                        if (obj.ParentCategoryList != null) obj.ParentCategoryList.Split('|').OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ForEach(t => hash = hash * 23 + t.GetHashCode());
                        obj.GetPolicy<ListPricingPolicy>().Prices.ForEach(price => hash = hash * 23 + SellableItemMoneyComparer.GetHashCode(price)); // View tests - Null exception is not possible
                        obj.GetComponent<ImagesComponent>().Images?.ForEach(image => hash = hash * 23 + image.GetHashCode());                      
                        break;

                    default:
                        throw new ArgumentException($"Comparer configuration cannot be handled");
                }

                return hash;
            }
        }
    }

    public enum SellableItemComparerConfiguration
    {
        ByProductId,
        ByImportData
    }
}
