using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class MoneyComparer : IEqualityComparer<Money>
    {
        public bool Equals(Money x, Money y)
        {
            if (x == null || y == null) return false;

            return x.CurrencyCode == y.CurrencyCode
                && x.Amount == y.Amount;
        }

        public int GetHashCode(Money obj)
        {
            if (obj == null) return base.GetHashCode();

            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            unchecked
            {
                int hash = 17;
                if (obj.CurrencyCode != null) hash = hash * 23 + obj.CurrencyCode.GetHashCode();
                hash = hash * 23 + obj.Amount.GetHashCode();
                return hash;
            }
        }
    }
}
