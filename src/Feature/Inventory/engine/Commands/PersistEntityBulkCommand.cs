using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class PersistEntityBulkCommand : CommerceCommand
    {
        public PersistEntityBulkCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<bool> Process(CommerceContext commerceContext, IEnumerable<CommerceEntity> items)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                commerceContext.Logger.LogInformation($"Called - {nameof(PersistEntityBulkCommand)}.");

                foreach (var item in items)
                {
                    commerceContext.ClearMessages();

                    await PerformTransaction(commerceContext, async () =>
                    {
                        var arg = new PersistEntityArgument(item);
                        await Pipeline<IPersistEntityPipeline>().Run(arg, commerceContext.PipelineContextOptions);
                    });
                }

                commerceContext.Logger.LogInformation($"Completed - {nameof(PersistEntityBulkCommand)}.");

                return true;
            }
        }
    }
}
