using System.Drawing;
using System.Globalization;
using System.Text;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VisioWebTools;

namespace VisioWebToolsAzureFunctions
{
    public class AddTooltipsFunction
    {
        private readonly ILogger<AddTooltipsFunction> log;
        public AddTooltipsFunction(ILogger<AddTooltipsFunction> log)
        {
            this.log = log;            
        }

        [Function("AddTooltipsFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData  req)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parser = await MultipartFormDataParser.ParseAsync(req.Body);

            var vsdx = parser.Files.FirstOrDefault(f => f.Name == "vsdx");
            var pdf = parser.Files.FirstOrDefault(f => f.Name == "pdf");

            var options = new PdfOptions();

            var paramX = parser.GetParameterValue("x");
            if (paramX != null)
            {
                options.HorizontalLocation = Convert.ToInt32(paramX, CultureInfo.InvariantCulture);
            }

            var paramY = parser.GetParameterValue("y");
            if (paramY != null)
            {
                options.VerticalLocation = Convert.ToInt32(paramY, CultureInfo.InvariantCulture);
            }

            var paramIcon = parser.GetParameterValue("icon");
            if (paramIcon != null)
            {
                options.Icon = paramIcon;
            }

            var paramColor = parser.GetParameterValue("color");
            if (paramColor != null)
            {
                options.Color = ColorTranslator.FromHtml(paramColor);
            }

            if (pdf == null || vsdx == null)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                errorResponse.WriteString("File(s) not received");
                return errorResponse;
            }

            var output = PdfUpdater.Process(pdf.Data, vsdx.Data, options);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Disposition", "attachment; filename=result.pdf");
            response.Headers.Add("Content-Type", "application/pdf");
            response.WriteBytes(output);
            return response;
        }
    }
}
