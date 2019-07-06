using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class DataImportMinion : Minion
    {
        private CommerceCommander CommerceCommander { get; set; }

        public override void Initialize(IServiceProvider serviceProvider, ILogger logger, MinionPolicy policy, CommerceEnvironment environment, CommerceContext globalContext)
        {
            base.Initialize(serviceProvider, logger, policy, environment, globalContext);
            CommerceCommander = serviceProvider.GetService<CommerceCommander>();
            LogInitialization();
        }

        private void LogInitialization()
        {
            var log = new StringBuilder();
            log.Append($"{Name} settings:{System.Environment.NewLine}");
            log.AppendLine($"\t WakeupInterval = {Policy.WakeupInterval}");
            log.AppendLine($"\t ListToWatch = {Policy.ListToWatch}");
            log.AppendLine($"\t ItemsPerBatch {Policy.ItemsPerBatch}");
            log.AppendLine($"\t SleepBetweenBatches {Policy.SleepBetweenBatches}");
            Logger.LogInformation(log.ToString());
        }

        protected override async Task<MinionRunResultsModel> Execute()
        {
            try
            {
                this.Logger.LogInformation($"{this.Name} - Starting");

                var commerceContext = new CommerceContext(this.Logger, this.MinionContext.TelemetryClient, null) { Environment = this.Environment };
                commerceContext.GetPolicy<AutoApprovePolicy>();
                await CommerceCommander.Pipeline<IImportDataPipeline>().Run(null, commerceContext.PipelineContextOptions);

                this.Logger.LogInformation($"{this.Name} - Finished");

                return new MinionRunResultsModel();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{Name}-Error occured: {ex.Message}", ex);
                throw;
            }
        }

        [Obsolete]
        public override Task<MinionRunResultsModel> Run()
        {
            throw new NotImplementedException();
        }
    }
}
