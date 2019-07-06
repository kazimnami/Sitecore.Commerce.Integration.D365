using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class DisassociateToParentBulkCommand : CommerceCommand
    {
        public DisassociateToParentBulkCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<bool> Process(CommerceContext commerceContext, IEnumerable<CatalogItemParentAssociationModel> associationList)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                foreach (var association in associationList)
                {
                    commerceContext.ClearMessages();

                    await PerformTransaction(commerceContext, async () =>
                    {
                        var relationshipType = Command<GetRelationshipTypeCommand>().Process(commerceContext, association.ParentId, association.ItemId);
                        await Command<DeleteRelationshipCommand>().Process(commerceContext, association.ParentId, association.ItemId, relationshipType);
                    });
                }

                return true;
            }
        }
    }
}
