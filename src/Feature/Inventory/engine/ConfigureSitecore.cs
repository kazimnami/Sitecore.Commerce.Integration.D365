using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Entitlements;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using System.Reflection;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config
                // Inventory
                .ConfigurePipeline<IGetInventoryInformationPipeline>(p =>
                {
                    p.Replace<Sitecore.Commerce.Plugin.Inventory.GetInventoryInformationBlock, GetInventoryInformationBlock>();
                })
                // Inventory
                .ConfigurePipeline<IPopulateItemAvailabilityComponentPipeline>(p =>
                {
                    p.Add<CreateD365InventorySetBlock>().Before<PopulateItemAvailabilityComponentBlock>();
                })
                // Pricing
                .ConfigurePipeline<ICalculateSellableItemPricesPipeline>(p =>
                {
                    p.Add<CalculateSellableItemD365PriceBlock>().Before<ReconcileSellableItemPricesBlock>();
                })
                // Order Transmission
                .ConfigurePipeline<IReleasedOrdersMinionPipeline>(p =>
                {
                    p.Add<SendOrderToD365>().Before<MoveReleasedOrderBlock>();
                    p.Remove<GenerateOrderShipmentBlock>();
                    p.Remove<GenerateOrderLinesShipmentBlock>();
                    p.Remove<GenerateOrderEntitlementsBlock>();
                })
            );
        }
    }
}