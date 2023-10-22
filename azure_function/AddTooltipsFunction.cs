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
using VisioWebTools;

namespace VisioWebToolsAzureFunctions
{
    public static class AddTooltipsFunction
    {
        [FunctionName("AddTooltipsFunction")]
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

            var pdf = GetParam("pdf");
            var vsdx = GetParam("vsdx");

            var options = new PdfOptions();

            var paramX = GetParam("x");
            if (paramX != null)
            {
                options.HorizontalLocation = Convert.ToInt32(await paramX.ReadAsStringAsync(), CultureInfo.InvariantCulture);
            }

            var paramY = GetParam("y");
            if (paramY != null)
            {
                options.VerticalLocation = Convert.ToInt32(await paramY.ReadAsStringAsync(), CultureInfo.InvariantCulture);
            }

            var paramIcon = GetParam("icon");
            if (paramIcon != null)
            {
                options.Icon = await paramIcon.ReadAsStringAsync();
            }

            var paramColor = GetParam("color");
            if (paramColor != null)
            {
                options.Color = ColorTranslator.FromHtml(await paramColor.ReadAsStringAsync());
            }

            if (pdf == null || vsdx == null)
            {
                return new BadRequestObjectResult("File(s) not received");
            }

            // Example process: Concatenate the contents of the files.
            var pdfStream = await pdf.ReadAsStreamAsync();
            var vsdxStream = await vsdx.ReadAsStreamAsync();

            var pdfOutput = PdfUpdater.Process(pdfStream, vsdxStream, options);

            return new FileContentResult(pdfOutput, "application/pdf")
            {
                FileDownloadName = "result.pdf",
            };
        }
    }
}
