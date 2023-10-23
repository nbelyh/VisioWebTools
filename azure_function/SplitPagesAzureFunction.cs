using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Text;
using VisioWebTools;

namespace VisioWebToolsAzureFunctions
{
    public static class SplitPagesAzureFunction
    {
        [FunctionName("SplitPagesAzureFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var provider = new MultipartMemoryStreamProvider();
            await req.Content.ReadAsMultipartAsync(provider);

            HttpContent GetParam(string name)
            {
                return provider.Contents.FirstOrDefault(f => f.Headers.ContentDisposition.Name.Trim('"') == name);
            }

            var vsdx = GetParam("vsdx");
            if (vsdx == null)
            {
                return new BadRequestObjectResult("File(s) not received");
            }

            var vsdxBytes = await vsdx.ReadAsByteArrayAsync();

            var output = SplitPagesService.SplitPages(vsdxBytes);

            return new FileContentResult(output, "application/zip")
            {
                FileDownloadName = "pages.zip"
            };
        }
    }
}
