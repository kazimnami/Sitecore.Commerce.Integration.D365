using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Entitlements;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using System.Reflection;

namespace SampleIntegrationD365.Feature.Orders.Engine
{
    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);
            services.RegisterAllCommands(assembly);

            services.Sitecore().Pipelines(config => config
                // Order Transmission
                .ConfigurePipeline<IReleasedOrdersMinionPipeline>(p =>
                {
                    p.Add<SendOrderToD365>().Before<MoveReleasedOrderBlock>();
                    p.Remove<GenerateOrderShipmentBlock>();
                    p.Remove<GenerateOrderLinesShipmentBlock>();
                    p.Remove<GenerateOrderEntitlementsBlock>();
                    p.Remove<MoveReleasedOrderBlock>();
                })
            );
        }
    }
}