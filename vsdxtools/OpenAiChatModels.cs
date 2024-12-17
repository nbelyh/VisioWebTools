namespace VsdxTools.OpenAi.Models;

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

public class ChatErrorResponse
{
    public ChatError Error { get; set; }
}

public class ChatError
{
    public string Message { get; set; }
    public string Type { get; set; }
    public string Code { get; set; }
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