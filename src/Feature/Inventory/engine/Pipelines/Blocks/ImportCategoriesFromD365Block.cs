using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleIntegrationD365.Foundation.D365.Engine;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ImportCategoriesFromD365Block : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        private CommerceCommander CommerceCommander { get; set; }

        public ImportCategoriesFromD365Block(IServiceProvider serviceProvider)
        {
            CommerceCommander = serviceProvider.GetService<CommerceCommander>();
        }

        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            var categoryComparerById = new ImportCategoryComparer(CategoryComparerConfiguration.ById);
            var categoryComparerByData = new ImportCategoryComparer(CategoryComparerConfiguration.ByData);

            try
            {
                var importRawLines = await context.GetPolicy<ConnectionPolicy>().GetCategories();
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, Name, null, $"{Name} API responded with '{importRawLines.Count()}' records.");

                var importItems = await CommerceCommander.Command<TransformImportToCategoryCommand>().Process(context.CommerceContext, importRawLines);
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, Name, null, $"{Name} Processing '{importItems.Count()}' Categories.");

                var existingItems = await CommerceCommander.Command<GetEntityBulkCommand>().Process(context.CommerceContext, importItems);

                var newItems = importItems.Except(existingItems, categoryComparerById).ToList();
                var changedItems = existingItems.Except(importItems, categoryComparerByData).ToList();

                CommerceCommander.Command<CopyImportToCategoriesCommand>().Process(context.CommerceContext, importItems, changedItems);

                var newAndChangedItemes = newItems.Union(changedItems);
                var associationsToCreate = newAndChangedItemes.SelectMany(i => i.GetPolicy<TransientImportCategoryDataPolicy>().ParentAssociationsToCreateList).ToList();
                var associationsToRemove = newAndChangedItemes.SelectMany(i => i.GetPolicy<TransientImportCategoryDataPolicy>().ParentAssociationsToRemoveList).ToList();

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

        private void RemoveTransientData(IEnumerable<Category> importItems)
        {
            foreach (var item in importItems)
            {
                if (item.HasPolicy<TransientImportCategoryDataPolicy>())
                    item.RemovePolicy(typeof(TransientImportCategoryDataPolicy));
            }
        }
    }
}