using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using HttpMultipartParser;
using VsdxTools;

namespace VisioWebToolsAzureFunctions
{
    public class SplitPagesAzureFunction
    {
        private readonly ILogger<SplitPagesAzureFunction> log;
        public SplitPagesAzureFunction(ILogger<SplitPagesAzureFunction> log)
        {
            this.log = log;
        }

        [Function("SplitPagesAzureFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            log.LogInformation("SplitPagesAzureFunction");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parser = await MultipartFormDataParser.ParseAsync(req.Body);

            var vsdx = parser.Files.FirstOrDefault(f => f.Name == "vsdx");
            if (vsdx == null)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                errorResponse.WriteString("File(s) not received");
                return errorResponse;
            }

            var output = SplitPagesService.SplitPages(vsdx.Data);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Disposition", "attachment; filename=pages.zip");
            response.Headers.Add("Content-Type", "application/zip");
            response.WriteBytes(output);
            return response;
        }
    }
}
