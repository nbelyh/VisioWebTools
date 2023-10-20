using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Drawing;
using System;
using System.Globalization;
using VisioMediaExtractor;

namespace VisioWebTools
{
    public static class ExtractImagesAzureFunction
    {


        [FunctionName("ExtractImagesAzureFunction")]
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

            // Example process: Concatenate the contents of the files.
            var vsdxStream = await vsdx.ReadAsStreamAsync();

            var output = ImageExtractor.ExtractMediaFromVisio(vsdxStream);

            return new FileContentResult(output, "application/zip")
            {
                FileDownloadName = "images.zip",
            };
        }
    }
}
