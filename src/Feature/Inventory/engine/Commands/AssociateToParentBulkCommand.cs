using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class AssociateToParentBulkCommand : CommerceCommand
    {
        public AssociateToParentBulkCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<bool> Process(CommerceContext commerceContext, IEnumerable<ParentAssociationModel> associationList)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                commerceContext.Logger.LogInformation($"Called - {nameof(AssociateToParentBulkCommand)}.");

                foreach (var association in associationList)
                {
                    // Need to clear message as any prior error will cause all transactions to abort.
                    commerceContext.ClearMessages();

                    await PerformTransaction(commerceContext, async () =>
                    {
                        var relationshipType = Command<GetRelationshipTypeCommand>().Process(commerceContext, association.ParentId, association.ItemId);
                        await Command<CreateRelationshipCommand>().Process(commerceContext, association.ParentId, association.ItemId, relationshipType);
                    });
                }

                commerceContext.Logger.LogInformation($"Completed - {nameof(AssociateToParentBulkCommand)}.");

                return true;
            }
        }
    }
}
