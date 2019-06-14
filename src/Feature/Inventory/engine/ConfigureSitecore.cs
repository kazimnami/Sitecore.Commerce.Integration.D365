using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
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
                .ConfigurePipeline<IGetInventoryInformationPipeline>(p =>
                {
                    p.Replace<Sitecore.Commerce.Plugin.Inventory.GetInventoryInformationBlock, SampleIntegrationD365.Feature.Inventory.Engine.GetInventoryInformationBlock>();
                })
                //TODO: Remove
                .ConfigurePipeline<IPopulateItemAvailabilityComponentPipeline>(p =>
                {
                    p.Add<SampleIntegrationD365.Feature.Inventory.Engine.CreateD365InventorySetBlock>().Before<Sitecore.Commerce.Plugin.Inventory.PopulateItemAvailabilityComponentBlock>();
                    p.Replace<Sitecore.Commerce.Plugin.Inventory.PopulateItemAvailabilityComponentBlock, SampleIntegrationD365.Feature.Inventory.Engine.PopulateItemAvailabilityComponentBlock>();
                })
                //TODO: Remove
                .ConfigurePipeline<IPopulateItemAvailabilityPipeline>(p =>
                {
                    p.Replace<Sitecore.Commerce.Plugin.Inventory.PopulateItemAvailabilityBlock, SampleIntegrationD365.Feature.Inventory.Engine.PopulateItemAvailabilityBlock>();
                })
                //TODO: Remove
                .ConfigurePipeline<IPopulateLineItemPipeline>(p =>
                {
                    p.Replace<Sitecore.Commerce.Plugin.Inventory.PopulateLineItemInventoryBlock, SampleIntegrationD365.Feature.Inventory.Engine.PopulateLineItemInventoryBlock>();
                })
                //TODO: Remove
                .ConfigurePipeline<IPopulateLineItemPipeline>(p =>
                {
                    p.Add<TestBlock>().After<CalculateCartLinePriceBlock>();
                    p.Add<TestBlock>().After<ValidateCartLinePriceBlock>();
                })
                .ConfigurePipeline<ICalculateSellableItemPricesPipeline>(p =>
                {
                    p.Add<CalculateSellableItemD365PriceBlock>().Before<ReconcileSellableItemPricesBlock>();
                })
            );
        }
    }
}