using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Availability;
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
            services.RegisterAllCommands(assembly);

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
                // Data Import
                .AddPipeline<IImportDataPipeline, ImportDataPipeline>(p =>
                {
                    p.Add<ImportCategoriesFromD365Block>();
                    p.Add<ImportSellableItemsFromD365Block>();
                })
            );
        }
    }
}