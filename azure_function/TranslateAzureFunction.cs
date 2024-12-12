using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VisioWebToolsAzureFunctions
{
    public class TranslateAzureFunction
    {
        private ILogger<ExtractImagesAzureFunction> log;
        private IConfiguration config;
        
        public TranslateAzureFunction(ILogger<ExtractImagesAzureFunction> log, IConfiguration config)
        {
            this.log = log;
            this.config = config;
        }

        [Function("TranslateAzureFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var chatRequest = await req.ReadFromJsonAsync<ChatRequest>();

            var key = this.config["OPEN_AI_KEY"];

            var chatResponse = await OpenAIChatService.MakeRequest(chatRequest, key);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(chatResponse);
            return response;
        }
    }
}
