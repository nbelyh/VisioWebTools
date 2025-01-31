using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VsdxTools.Serialization;
using VsdxTools.OpenAi;
using System.Diagnostics;

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
            log.LogInformation("TranslateAzureFunction");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var chatRequestJson = await req.ReadAsStringAsync();
            var chatRequest = JsonSerializer.Deserialize(chatRequestJson, ChatRequestJsonContext.Context.ChatRequest);

            log.LogMetric("TranslateAzureFunction OpenAPI Chat Request Size", chatRequestJson.Length);

            try
            {
                var key = this.config["OPEN_AI_KEY"];
                if (string.IsNullOrEmpty(key))
                    throw new Exception("You must provide an OpenAI key to be able to use this function.");

                var stopwatch = Stopwatch.StartNew();
                var chatResponse = await ChatService.MakeRequest("https://api.openai.com/v1/chat/completions", key, chatRequest);
                stopwatch.Stop();
                
                log.LogMetric("TranslateAzureFunction OpenAPI Chat Request", stopwatch.ElapsedMilliseconds);
                
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                var responseJson = JsonSerializer.Serialize(chatResponse, ChatResponseJsonContext.Context.ChatResponse);
                await response.WriteStringAsync(responseJson);
                return response;
            }
            catch (ChatException ex)
            {
                log.LogError(ex, "Error in TranslateAzureFunction");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(ex.Json);
                return response;
            }
        }
    }
}
