using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    //[PipelineDisplayName(CatalogConstants.ReconcileSellableItemPricesBlock)]
    public class CalculateSellableItemD365PriceBlock : PipelineBlock<SellableItem, SellableItem, CommercePipelineExecutionContext>
    {
        public override async Task<SellableItem> Run(SellableItem arg, CommercePipelineExecutionContext context)
        {
            if (arg == null)
            {
                return arg;
            }

            var IsD365Product = arg.Tags.Any(t => t.Name.Equals("D365"));
            if (!IsD365Product)
            {
                return arg;
            }

            try
            {
                var connection = context.CommerceContext.GetPolicy<ConnectionPolicy>();

                var url = new Uri(new Uri(connection.BaseUrl), connection.CustomerPriceRelativeUrl);

                var request = new Dictionary<string, string>
                {
                    {"custAccount", "134387"},
                    {"itemId", arg.ProductId},
                    {"qty", "1"},
                };

                var stringResponse = await connection.PostJson(url, request);
                if (!decimal.TryParse(stringResponse, out decimal price))
                {
                    throw new Exception($"Error from URL: '{url}', unable to get price information for Product ID '{arg.ProductId}'. Response is: '{stringResponse}'.");
                }

                arg.ListPrice = new Money(context.CommerceContext.CurrentCurrency(), price);
                arg.GetComponent<MessagesComponent>().AddMessage(context.GetPolicy<KnownMessageCodePolicy>().Pricing, $"ListPrice<=D365Price: Price={arg.ListPrice.AsCurrency()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception($"Error, unable to get price information for product ID '{arg.ProductId}'.", ex);
            }

            // TODO Variations

            return arg;
        }
    }
}