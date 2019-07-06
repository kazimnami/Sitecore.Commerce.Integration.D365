using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public interface IImportDataPipeline : IPipeline<string, string, CommercePipelineExecutionContext>
    {
    }
}