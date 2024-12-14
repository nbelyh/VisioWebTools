using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HttpMultipartParser;
using VisioWebTools;
using System.Text.Json;

namespace VisioWebToolsAzureFunctions
{
    public class CipherFileAzureFunction
    {
        private readonly ILogger<CipherFileAzureFunction> log;
        public CipherFileAzureFunction(ILogger<CipherFileAzureFunction> log)
        {
            this.log = log;
        }

        [Function("CipherFileAzureFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            log.LogInformation("CipherFileAzureFunction");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parser = await MultipartFormDataParser.ParseAsync(req.Body);

            var vsdx = parser.Files.FirstOrDefault(f => f.Name == "vsdx");
            if (vsdx == null)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                errorResponse.WriteString("File(s) not received");
                return errorResponse;
            }

            var optionsJson = parser.GetParameterValue("optionsJson");
            var options = JsonSerializer.Deserialize(optionsJson, CipherOptionsJsonContext.Context.CipherOptions);

            using (var memoryStream = new MemoryStream())
            {
                await vsdx.Data.CopyToAsync(memoryStream);
                var vsdxData = memoryStream.ToArray();

                var output = CipherService.Process(vsdxData, options);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Headers.Add("Content-Disposition", $"attachment; filename={vsdx.FileName}");
                response.Headers.Add("Content-Type", "application/vnd.ms-visio.drawing");
                response.WriteBytes(output);
                return response;
            }
        }
    }
}
