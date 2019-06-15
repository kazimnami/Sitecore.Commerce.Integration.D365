using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Framework.Pipelines;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    //[PipelineDisplayName(AvailabilityConstants.PopulateDefaultItemAvailabilityComponentBlock)]
    public class CreateD365InventorySetBlock : PipelineBlock<ItemAvailabilityComponent, ItemAvailabilityComponent, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commander;

        public CreateD365InventorySetBlock(CommerceCommander commander)
        {
            _commander = commander;
        }

        public override async Task<ItemAvailabilityComponent> Run(ItemAvailabilityComponent arg, CommercePipelineExecutionContext context)
        {
            if (arg == null || string.IsNullOrEmpty(arg.ItemId))
            {
                return arg;
            }

            var productArgument = ProductArgument.FromItemId(arg.ItemId);
            var sellableItem = context.CommerceContext.GetEntity<SellableItem>(x => x.Id.Equals($"{CommerceEntity.IdPrefix<SellableItem>()}{productArgument.ProductId}", StringComparison.OrdinalIgnoreCase))
                               ?? await _commander.Pipeline<IGetSellableItemPipeline>().Run(productArgument, context).ConfigureAwait(false);

            if (sellableItem == null || !sellableItem.Tags.Any(t => t.Name.Equals("D365")))
            {
                return arg;
            }

            // This is needed if not having an inventory set assoicated to the sellable-item.
            // Without the InventoryComponent on the sellabl-item, inventory won't show on the PDP or within cart. 
            if (!string.IsNullOrEmpty(productArgument.VariantId))
            {
                sellableItem.GetComponent<InventoryComponent>(productArgument.VariantId);
            }
            else
            {
                sellableItem.GetComponent<InventoryComponent>();
            }

            return arg;
        }

    }
}