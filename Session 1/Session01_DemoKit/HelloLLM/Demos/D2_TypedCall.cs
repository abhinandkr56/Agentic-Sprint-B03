namespace HelloLLM.Demos;

/// DEMO 2 — Same call, but with C# records + System.Text.Json.
/// Teaching point: it's a normal DTO mapping problem; read content, usage and finish_reason.
public static class D2_TypedCall
{
    public static async Task Run(Llm llm)
    {
        var request = new ChatRequest(llm.Model,
        [
            ChatMessage.System("You are a helpful assistant for .NET developers. Answer in 3 short bullet points."),
            ChatMessage.User("What are 3 ways an LLM could help in a typical ASP.NET Core project?")
        ])
        { Temperature = 0.3 };

        Ui.Step("Sending a typed ChatRequest record...");
        var (response, elapsed) = await llm.ChatAsync(request);

        var choice = response.Choices[0];
        Ui.Step("choices[0].message.content");
        Ui.Answer(choice.Message.Content);

        Ui.Step("Metadata every production app should log");
        Console.WriteLine($"  id            : {response.Id}");
        Console.WriteLine($"  model         : {response.Model}");
        Console.WriteLine($"  finish_reason : {choice.FinishReason}   (stop = finished naturally, length = hit max_tokens)");
        Ui.Usage(response.Usage, elapsed);
    }
}
