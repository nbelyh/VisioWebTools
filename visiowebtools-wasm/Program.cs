using System.Runtime.InteropServices.JavaScript;
using System.Drawing;
using VsdxTools;
using System.Runtime.Versioning;
using System.Text.Json;
using VsdxTools.OpenAi;
using VsdxTools.Serialization;

// Create a "Main" method. This is required by the tooling.
return;

public partial class FileProcessor
{
    // Make the method accessible from JS
    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] SplitPages(byte[] vsdx)
    {
        using (var stream = new MemoryStream(vsdx))
        {
            return SplitPagesService.SplitPages(stream);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] ExtractImages(byte[] vsdx)
    {
        using (var stream = new MemoryStream(vsdx))
        {
            return ExtractMediaService.ExtractMedia(stream);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] AddTooltips(byte[] pdf, byte[] vsdx, string color, string icon, int x, int y)
    {
        var options = new PdfOptions
        {
            Color = ColorTranslator.FromHtml(color),
            Icon = icon,
            HorizontalLocation = x,
            VerticalLocation = y
        };

        using (var pdfStream = new MemoryStream(pdf))
        using (var vsdxStream = new MemoryStream(vsdx))
        {
            return PdfUpdater.Process(pdfStream, vsdxStream, options);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] CipherFile(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, CipherOptionsJsonContext.Context.CipherOptions);
        return CipherService.Process(vsdx, options);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static string GetTranslationJson(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, TranslateOptionsJsonContext.Context.TranslateOptions);
        var translations = TranslateService.GetTranslationJson(vsdx, options);
        return translations;
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] ApplyTranslationJson(byte[] vsdx, string optionsJson, string json)
    {
        var options = JsonSerializer.Deserialize(optionsJson, TranslateOptionsJsonContext.Context.TranslateOptions);
        var bytes = TranslateService.ApplyTranslationJson(vsdx, options, json);
        return bytes;
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static async Task<string> Translate(string apiUrl, string apiKey, string json, string language)
    {
        var chatRequest = ChatService.CreateChatRequest(json, language);
        try {
            var chatResponse = await ChatService.MakeRequest(apiUrl, apiKey, chatRequest);    
            var translated = ChatService.ParseChatResponse(chatResponse);
            return translated;
        } 
        catch (ChatException ex) 
        {
            var response = JsonSerializer.Deserialize(ex.Json, ChatErrorResponseJsonContext.Context.ChatErrorResponse);
            throw new Exception(response?.Error?.Message ?? ex.Message);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static string ExtractJson(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, JsonExportOptionsJsonContext.Context.JsonExportOptions);

        if (options?.TranslatableOnly == true)
        {
            var translationOptions = new TranslateOptions
            {
                EnableTranslateShapeText = options.IncludeShapeText,
                EnableTranslateShapeFields = options.IncludeShapeFields,
                EnableTranslateUserRows = options.IncludeUserRows,
                EnableTranslatePropertyValues = options.IncludePropertyRows,
                EnableTranslatePropertyLabels = false,
            };
            var result = TranslateService.GetTranslationJson(vsdx, translationOptions);
            return result;
        } 
        else 
        {
            var result = JsonExportService.Process(vsdx, options);
            return result;
        }
    }
}
