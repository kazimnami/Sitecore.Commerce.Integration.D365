using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    //[PipelineDisplayName(FulfillmentConstants.GenerateOrderLinesShipmentBlock)]
    public class SendOrderToD365 : PipelineBlock<Order, Order, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commander;

        public SendOrderToD365(CommerceCommander commander)
        {
            _commander = commander;
        }

        public override async Task<Order> Run(Order arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name}: The order cannot be null");

            if (!arg.Lines.Any()
                || !arg.Status.Equals(context.GetPolicy<KnownOrderStatusPolicy>().Released, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            try
            {
                var connection = context.CommerceContext.GetPolicy<ConnectionPolicy>();

                var url = new Uri(new Uri(connection.BaseUrl), connection.CreateOrderHeaderRelativeUrl);

                var request = new Dictionary<string, dynamic>
                {
                    {"dataAreaId","au"},
                    {"CurrencyCode","AUD"},
                    {"OrderingCustomerAccountNumber","104526"},
                    {"CustomersOrderReference",arg.OrderConfirmationId},
                    {"Email",arg.GetComponent<ContactComponent>().Email},
                    {"IsSalesProcessingStopped","Yes"},
                    {"SalesTaxGroupCode","GST"},
                };

                var stringResponse = await connection.PostJson(url, request);
                var tokenResponse = JsonConvert.DeserializeObject<JToken>(stringResponse);
                if (tokenResponse == null || tokenResponse["SalesOrderNumber"] == null)
                {
                    throw new Exception($"Error from URL: '{url}', unable to create order header in D365 for order '{arg.OrderConfirmationId}'. Response is: '{stringResponse}'.");
                }

                var orderNumber = tokenResponse["SalesOrderNumber"].ToString();
                arg.GetComponent<ErpOrderDetails>().OrderNumber = orderNumber;
                arg.GetComponent<MessagesComponent>().AddMessage("ERP Interface", $"Order header successfully created in D365 with order number '{orderNumber}'");

                url = new Uri(new Uri(connection.BaseUrl), connection.CreateOrderLineRelativeUrl);

                foreach (var line in arg.Lines.Where(l => l != null))
                {
                    request = new Dictionary<string, dynamic>
                    {
                        {"SalesOrderNumber", orderNumber},
                        {"ItemNumber", line.ItemId.Split('|')[1]},
                        {"OrderedSalesQuantity", decimal.ToInt32(line.Quantity)},
                    };

                    stringResponse = await connection.PostJson(url, request);
                    tokenResponse = JsonConvert.DeserializeObject<JToken>(stringResponse);
                    if (tokenResponse == null || tokenResponse["LineCreationSequenceNumber"] == null)
                    {
                        throw new Exception($"Error from URL: '{url}', unable to create order line '{line.ItemId}' in D365 for order '{arg.OrderConfirmationId}'. Response is: '{stringResponse}', expecting LineCreationSequenceNumber.");
                    }

                    var lineNumber = tokenResponse["LineCreationSequenceNumber"].ToString();
                    arg.GetComponent<ErpOrderDetails>().OrderNumber = orderNumber;
                    arg.GetComponent<MessagesComponent>().AddMessage("ERP Interface", $"Order line successfully created in D365. Line number confirmation '{lineNumber}'");
                }

 
            }
            catch (Exception ex)
            {
                arg.Status = context.GetPolicy<KnownOrderStatusPolicy>().Problem;
                arg.GetComponent<MessagesComponent>().AddMessage("ERP Interface Error", ex.ToString());
                context.Logger.LogError($"{this.Name}: Error, unable to create order in D365 for order '{arg.OrderConfirmationId}'", ex);
            }

            await _commander.Pipeline<IPersistOrderPipeline>().Run(arg, context).ConfigureAwait(false);

            return arg;
        }
    }
}
