using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class ConnectionPolicy : Policy
    {
        public string ClientId { get; set; } 
        public string ClientSecret { get; set; } 
        public string BaseUrl { get; set; }
        public string TokenUrl { get; set; }
        public string StockAvailabilityRelativeUrl { get; set; } 
        public string CustomerPriceRelativeUrl { get; set; }
        public string CreateOrderHeaderRelativeUrl { get; set; } = @"data/SalesOrderHeadersV2";
        public string CreateOrderLineRelativeUrl { get; set; } = @"data/SalesOrderLines";
        private JToken TokenResponse { get; set; } = null;

        private async Task<string> GetBearerToken()
        {
            try
            {
                if (TokenResponse == null)
                {
                    var form = new Dictionary<string, string>
                    {
                        {"grant_type", "client_credentials"},
                        {"client_id", ClientId},
                        {"client_secret", ClientSecret},
                        {"resource", BaseUrl},
                    };

                    using (var client = new HttpClient())
                    {
                        using (var httpResponse = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(form)))
                        {
                            if (!httpResponse.IsSuccessStatusCode)
                                throw new Exception($"Error from TokenUrl: '{TokenUrl}', received StatusCode: '{httpResponse.StatusCode}' & ReasonPhrase: '{httpResponse.ReasonPhrase}'");

                            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
                            TokenResponse = JsonConvert.DeserializeObject<JToken>(stringResponse);
                        }
                    }
                }

                // Future enhancement, need to check expiration of tokem
                //string tokenString = System.Text.Encoding.UTF8.GetString(t.TokenValue);

                var token = TokenResponse["access_token"].ToString();

                if (string.IsNullOrEmpty(token))
                {
                    TokenResponse = null;
                    throw new Exception($"Error, unable to retrieve bearer token from TokenUrl: '{TokenUrl}'. Token response is null or empty.");
                }
                return token;
            }
            catch (Exception ex)
            {
                TokenResponse = null;
                Console.WriteLine(ex.ToString());
                throw new Exception($"Error, unable to retrieve bearer token from TokenUrl: '{TokenUrl}'.Exception: '{ex.Message}'", ex);
            }
        }

        public async Task<string> PostJson<T>(Uri url, T request)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetBearerToken());
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var httpResponse = await client.PostAsJsonAsync(url, request))
                {
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        if (httpResponse.StatusCode.Equals(System.Net.HttpStatusCode.Unauthorized))
                        {
                            TokenResponse = null;
                        }

                        throw new Exception($"Error from URL: '{url}'. Received StatusCode: '{httpResponse.StatusCode}' & ReasonPhrase: '{httpResponse.ReasonPhrase}'");
                    }

                    return await httpResponse.Content.ReadAsStringAsync();
                }
            }
        }
    }
}