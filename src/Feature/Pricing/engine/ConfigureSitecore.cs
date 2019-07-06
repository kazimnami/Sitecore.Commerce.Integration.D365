using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using System.Reflection;

namespace SampleIntegrationD365.Feature.Pricing.Engine
{
    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);
            services.RegisterAllCommands(assembly);

            services.Sitecore().Pipelines(config => config
                .ConfigurePipeline<ICalculateSellableItemPricesPipeline>(p =>
                {
                    p.Add<CalculateSellableItemD365PriceBlock>().Before<ReconcileSellableItemPricesBlock>();
                })
            );
        }
    }
}