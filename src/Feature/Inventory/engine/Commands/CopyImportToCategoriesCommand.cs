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
    public class CopyImportToCategoriesCommand : CommerceCommand
    {
        public CopyImportToCategoriesCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public void Process(CommerceContext commerceContext, IEnumerable<Category> allImportItems, IEnumerable<Category> changedItems)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                if (!changedItems.Any()) return;

                foreach (var item in changedItems)
                {
                    var itemNewData = allImportItems.FirstOrDefault(i => i.Id.Equals(item.Id));

                    if (itemNewData == null) throw new Exception($"Error, can't find category Id {item.Id} in import list. This should never happen.");

                    CopyCore(itemNewData, item);
                }
            }
        }

        private void CopyCore(Category itemNewData, Category item)
        {
            // These fields are not included
            // item.ProductId
            // item.Id
            // item.FriendlyId 
            // item.Name 
            // item.SitecoreId
            item.DisplayName = itemNewData.DisplayName;
            // item.Description = itemNewData.Description; // Not being imported at the moment so expect it will be managed manually.
        }
    }
}
