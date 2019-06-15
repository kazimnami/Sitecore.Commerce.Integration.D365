using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using Sitecore.Commerce.Plugin.Inventory;
using System.Collections.Generic;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    [PipelineDisplayName(InventoryConstants.GetInventoryInformationBlock)]
    public class GetInventoryInformationBlock : PipelineBlock<SellableItemInventorySetArgument, InventoryInformation, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commander;

        public GetInventoryInformationBlock(CommerceCommander commander)
        {
            _commander = commander;
        }

        public override async Task<InventoryInformation> Run(
            SellableItemInventorySetArgument arg,
            CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            var sellableItem = context.CommerceContext.GetEntity<SellableItem>(x => x.Id.Equals(arg.SellableItemId, StringComparison.OrdinalIgnoreCase))
                ?? await _commander.Pipeline<IFindEntityPipeline>()
                    .Run(new FindEntityArgument(typeof(SellableItem), arg.SellableItemId.EnsurePrefix(CommerceEntity.IdPrefix<SellableItem>())), context)
                    .ConfigureAwait(false) as SellableItem;

            if (sellableItem == null)
            {
                return null;
            }

            var inventoryInformation = await GetSellableItemInventoryInformation(sellableItem, arg.VariationId, arg, context).ConfigureAwait(false);
            if (inventoryInformation != null)
            {
                context.CommerceContext.AddUniqueEntity(inventoryInformation);
            }

            return inventoryInformation;
        }

        protected virtual async Task<InventoryInformation> GetSellableItemInventoryInformation(
            SellableItem sellableItem,
            string variationId,
            SellableItemInventorySetArgument arg,
            CommercePipelineExecutionContext context)
        {
            Condition.Requires(sellableItem, nameof(sellableItem)).IsNotNull();
            Condition.Requires(arg, nameof(arg)).IsNotNull();
            Condition.Requires(context, nameof(context)).IsNotNull();

            var IsD365Product = sellableItem.Tags.Any(t=> t.Name.Equals("D365"));
            if (!IsD365Product)
                return null;

            try
            {
                var connection = context.CommerceContext.GetPolicy<ConnectionPolicy>();

                var url = new Uri(new Uri(connection.BaseUrl), connection.StockAvailabilityRelativeUrl);

                var request = new Dictionary<string, string>
                {
                    {"itemId", sellableItem.ProductId},
                    {"dataareaid", "au"},
                };

                var stringResponse = await connection.PostJson(url, request);
                if (!decimal.TryParse(stringResponse, out decimal stockAmount))
                {
                    throw new Exception($"Error from URL: '{url}', unable to get stock information for product ID '{sellableItem.ProductId}'. Response is: '{stringResponse}'.");
                }

                var inventoryInformation = new InventoryInformation
                {
                    Id = "Habitat_Inventory-" + sellableItem.ProductId,
                    FriendlyId = "Entity-InventoryInformation-Habitat_Inventory-" + sellableItem.ProductId,
                    SellableItem = new EntityReference { EntityTarget = sellableItem.Id },
                    InventorySet = new EntityReference { EntityTarget = arg.InventorySetId },
                    VariationId = "", //sellableItem.ProductId + "_0",
                    Quantity = decimal.ToInt32(stockAmount),
                    Published = true,
                };

                return inventoryInformation;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception($"Error, unable to get stock information for product ID '{sellableItem.ProductId}'.", ex);
            }
        }
    }
}
