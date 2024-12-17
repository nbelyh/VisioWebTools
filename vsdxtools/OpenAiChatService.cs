using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VsdxTools.OpenAi.Models;

namespace VsdxTools.OpenAi;

public class ChatException : Exception
{
    public string Json { get; }
    public ChatException(string message, string json) : base(message)
    {
        this.Json = json;
    }
}

public class ChatService
{
    public static async Task<ChatResponse> MakeRequest(
        string url,
        string apiKey,
        ChatRequest chatRequest)
    {
        // Serialize the request body to JSON
        string jsonRequestBody = JsonSerializer.Serialize(chatRequest, ChatRequestJsonContext.Context.ChatRequest);

        // Create the HTTP content with the serialized request body
        var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Send the request
        HttpResponseMessage response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize(jsonResponse, ChatResponseJsonContext.Context.ChatResponse);
            return chatResponse;
        }
        else 
        {
            if (response.Content != null)
            {
                string jsonErrorResponse = await response.Content.ReadAsStringAsync();
                throw new ChatException("Unable to call OpenAI API", jsonErrorResponse);
            }
            else 
            {
                throw new ChatException("Unable to call OpenAI API", "{\"error\": { \"message\": \"There is no OpenAI response\"}}");
            }
        }
    }

    public static ChatRequest CreateChatRequest(string json, string language)
    {
        // Prepare the request body
        var chatRequest = new ChatRequest
        {
            Model = "gpt-4o-mini",
            MaxTokens = 4000,
            ResponseFormat = new ChatRequestResponseFormat { Type = "json_object" },
            Messages =
            [
                new Message { Role = "system", Content = $"Translate the user JSON input into {language}. Keep the tags intact. Output JSON." },
                new Message { Role = "user", Content = json }
            ]
        };

        return chatRequest;
    }

    public static string ParseChatResponse(ChatResponse chatResponse)
    {
        return chatResponse?.Choices?[0]?.Message?.Content;
    }
}
