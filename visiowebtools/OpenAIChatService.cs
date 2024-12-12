using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VisioWebTools;

public class OpenAIChatService
{
    public static async Task<ChatResponse> MakeRequest(ChatRequest chatRequest, string apiKey)
    {
        // Set the endpoint URL for the Chat Completion API
        string url = "https://api.openai.com/v1/chat/completions";

        // Serialize the request body to JSON
        string jsonRequestBody = JsonSerializer.Serialize(chatRequest, ChatRequestJsonContext.Default.ChatRequest);

        // Create the HTTP content with the serialized request body
        var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Send the request
        HttpResponseMessage response = await httpClient.PostAsync(url, content);

        // Ensure the response is successful
        response.EnsureSuccessStatusCode();

        // Read and deserialize the response content
        string jsonResponse = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonSerializer.Deserialize(jsonResponse, ChatResponseJsonContext.Default.ChatResponse);

        return chatResponse;
    }

    public static async Task<string> Translate(string json, string language, string apiKey)
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

        var chatResponse = await MakeRequest(chatRequest, apiKey);

        return chatResponse?.Choices?[0]?.Message?.Content;
    }
}

public class ChatRequest
{
    public string Model { get; set; }
    public int MaxTokens { get; set; }
    public Message[] Messages { get; set; }
    public ChatRequestResponseFormat ResponseFormat { get; set; }
}

public class ChatRequestResponseFormat
{
    public string Type { get; set; }
}

// Define the structure of the response
public class ChatResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public int Created { get; set; }
    public Choice[] Choices { get; set; }
    public Usage Usage { get; set; }
}

public class Choice
{
    public int Index { get; set; }
    public Message Message { get; set; }
    public string FinishReason { get; set; }
}

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}