using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Text;


namespace paypalInvoice.Controllers
{
    public class CheckoutController : Controller
    {
        private string PayPalClientId { get; set; } = "";
        private string PayPalSecret { get; set; } = "";
        private string PayPalUrl { get; set; } = "";

        public CheckoutController(IConfiguration configuration) 
        {
            PayPalClientId = configuration["PayPalSettings:ClientId"]!;
            PayPalSecret = configuration["PayPalSettings:Secret"]!;
            PayPalUrl = configuration["PayPalSettings:Url"]!;

            // Imprime los valores de configuración en la consola
            Console.WriteLine("PayPal Client ID: " + PayPalClientId);
            Console.WriteLine("PayPal Secret: " + PayPalSecret);
            Console.WriteLine("PayPal URL: " + PayPalUrl);
        }


        public IActionResult Index()
        {
            /**Esto se lo pasamos a la vista para que lo use javascript */
            ViewBag.PayPalClientId = PayPalClientId;
            return View(); 
        }


        [HttpPost]
        public async Task<JsonResult> CreateOrder([FromBody] JsonObject data)
        {
            var totalAmount = data?["amount"]?.ToString();
            if (totalAmount == null)
            {
                return new JsonResult(new { id = "" });
            }


            // Create the request body
            JsonObject createOrderRequest = new JsonObject();
            createOrderRequest.Add("intent", "CAPTURE");

            JsonObject amount = new JsonObject();
            amount.Add("currency_code", "USD");
            amount.Add("value", totalAmount);

            JsonObject purchaseUnit1 = new JsonObject();
            purchaseUnit1.Add("amount", amount);

            JsonArray purchaseUnits = new JsonArray();
            purchaseUnits.Add(purchaseUnit1);

            createOrderRequest.Add("purchase_units", purchaseUnits);

            //get access token
            string accessToken = await GetPayPalAccessToken();

            //send request
            string url = PayPalUrl + "/v2/checkout/orders";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent(createOrderRequest.ToString(), null, "application/json");

                var httpResponse = await client.SendAsync(requestMessage);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);

                    if (jsonResponse != null)
                    {
                        string paypalOrderId = jsonResponse["id"]?.ToString() ?? "";

                        return new JsonResult(new { id = paypalOrderId });
                    }
                }
            }
            return new JsonResult(new { id = "" });
        }


        /**Lo utilizamos solo una vez para generar el token
         * 
         * public async Task<string> Token()
        {
            return await GetPayPalAccessToken();
        }*/

        private async Task<string> GetPayPalAccessToken() 
        {
            string accessToken = "";

            string url = PayPalUrl + "/v1/oauth2/token";

            using (var client  = new HttpClient())
            {
                string credentials64 =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(PayPalClientId + ":" + PayPalSecret));

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials64);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("grant_type=client_credentials", null, "application/x-www-form-urlencoded");

                var httpResponse = await client.SendAsync(requestMessage);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);
 
                    if (jsonResponse != null)
                    {
                        accessToken = jsonResponse["access_token"]?.ToString() ?? "";
                    }
                }
            }
            return accessToken;
        }
    }
}
