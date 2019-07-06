using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class GetEntityBulkCommand : CommerceCommand
    {
        public GetEntityBulkCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<IEnumerable<T>> Process<T>(CommerceContext commerceContext, IEnumerable<T> items) where T : CommerceEntity
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                var returnedItems = new List<T>();
                foreach (var item in items)
                {
                    var findEntityArgument = new FindEntityArgument(typeof(T), item.Id, false);

                    var commerceEntity = await Pipeline<IFindEntityPipeline>().Run(findEntityArgument, commerceContext.PipelineContextOptions);
                    if (commerceEntity != null) returnedItems.Add(commerceEntity as T);
                }

                return returnedItems;
            }
        }
    }
}
