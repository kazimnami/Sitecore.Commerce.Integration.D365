using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleIntegrationD365.Foundation.D365.Engine;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ImportSellableItemsFromD365Block : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        private CommerceCommander CommerceCommander { get; set; }

        public ImportSellableItemsFromD365Block(IServiceProvider serviceProvider)
        {
            CommerceCommander = serviceProvider.GetService<CommerceCommander>();
        }

        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            var sellableItemComparerByProductId = new ImportSellableItemComparer(SellableItemComparerConfiguration.ByProductId);
            var sellableItemComparerByImportData = new ImportSellableItemComparer(SellableItemComparerConfiguration.ByImportData);

            try
            {
                var importRawLines = await context.GetPolicy<ConnectionPolicy>().GetProducts();
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, Name, null, $"{Name} API responded with '{importRawLines.Count()}' records.");

                var importItems = await CommerceCommander.Command<TransformImportToSellableItemsCommand>().Process(context.CommerceContext, importRawLines);
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, Name, null, $"{Name} Processing '{importItems.Count()}' Sellable-items.");

                var existingItems = await CommerceCommander.Command<GetEntityBulkCommand>().Process(context.CommerceContext, importItems);

                var newItems = importItems.Except(existingItems, sellableItemComparerByProductId).ToList();
                var changedItems = existingItems.Except(importItems, sellableItemComparerByImportData).ToList();

                await CommerceCommander.Command<CopyImportToSellableItemsCommand>().Process(context.CommerceContext, importItems, changedItems);

                var newAndChangedItemes = newItems.Union(changedItems);
                var associationsToCreate = newAndChangedItemes.SelectMany(i => i.GetPolicy<TransientImportSellableItemDataPolicy>().ParentAssociationsToCreateList).ToList();
                var associationsToRemove = newAndChangedItemes.SelectMany(i => i.GetPolicy<TransientImportSellableItemDataPolicy>().ParentAssociationsToRemoveList).ToList();

                RemoveTransientData(importItems);

                await CommerceCommander.Command<PersistEntityBulkCommand>().Process(context.CommerceContext, newAndChangedItemes);
                await CommerceCommander.Command<AssociateToParentBulkCommand>().Process(context.CommerceContext, associationsToCreate);
                await CommerceCommander.Command<DisassociateToParentBulkCommand>().Process(context.CommerceContext, associationsToRemove);
            }
            catch (Exception ex)
            {
                context.Abort(await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Error,
                    Name,
                    new object[1] { ex },
                    $"{Name} Import Exception: {ex.Message}"),
                    context);
            }

            return null;
        }

        private void RemoveTransientData(IEnumerable<SellableItem> importItems)
        {
            foreach (var item in importItems)
            {
                if (item.HasPolicy<TransientImportSellableItemDataPolicy>())
                    item.RemovePolicy(typeof(TransientImportSellableItemDataPolicy));
            }
        }
    }
}
