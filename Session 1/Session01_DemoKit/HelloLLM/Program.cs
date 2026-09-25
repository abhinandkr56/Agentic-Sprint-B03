// ─────────────────────────────────────────────────────────────
//  Session 01 · LLM Fundamentals for .NET Developers
//  "An LLM is just another HTTP API" — raw HttpClient, no SDKs.
//
//  Run the menu:        dotnet run
//  Run one demo:        dotnet run -- 3
// ─────────────────────────────────────────────────────────────
using System.Net.Http.Headers;
using HelloLLM;
using HelloLLM.Demos;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var config = LlmConfig.FromEnvironment();
if (config is null) return 1;

// One HttpClient for the whole app. BaseAddress + auth header set once.
using var http = new HttpClient
{
    BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/"),
    Timeout = TimeSpan.FromSeconds(90)
};
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

var llm = new Llm(http, config);

var demos = new (string Title, Func<Task> Run)[]
{
    ("Raw call        – what a request/response really looks like", () => D1_RawCall.Run(http, config)),
    ("Typed call      – JSON → C# records, tokens & finish_reason",  () => D2_TypedCall.Run(llm)),
    ("Tokens          – why Hindi/Malayalam/code cost more",        () => D3_Tokens.Run(llm)),
    ("Temperature     – same prompt, different answers",            () => D4_Temperature.Run(llm)),
    ("Memory          – the model forgets; YOU resend history",     () => D5_Memory.Run(llm)),
    ("Streaming       – tokens as they are generated (SSE)",        () => D6_Streaming.Run(llm)),
    ("Limits & errors – max_tokens, bad model, 401",                () => D7_LimitsAndErrors.Run(llm, config)),
    ("DotNetBuddy     – a tiny streaming chatbot (finale)",         () => D8_ChatLoop.Run(llm)),
};

// Direct mode: dotnet run -- 3
if (args.Length > 0 && int.TryParse(args[0], out var direct) && direct >= 1 && direct <= demos.Length)
{
    await RunSafe(demos[direct - 1]);
    return 0;
}

while (true)
{
    Ui.Banner(config);
    for (int i = 0; i < demos.Length; i++)
        Console.WriteLine($"  [{i + 1}] {demos[i].Title}");
    Console.WriteLine("  [q] Quit");
    Console.Write("\nPick a demo: ");
    var choice = Console.ReadLine()?.Trim();
    if (choice is null || choice.Equals("q", StringComparison.OrdinalIgnoreCase)) break;
    if (int.TryParse(choice, out var n) && n >= 1 && n <= demos.Length)
    {
        await RunSafe(demos[n - 1]);
        Ui.Dim("\nPress Enter to return to the menu...");
        Console.ReadLine();
    }
}
return 0;

static async Task RunSafe((string Title, Func<Task> Run) demo)
{
    Console.Clear();
    Ui.Title(demo.Title);
    try { await demo.Run(); }
    catch (HttpRequestException ex) { Ui.Error($"Network problem: {ex.Message}"); }
    catch (TaskCanceledException) { Ui.Error("Request timed out."); }
    catch (Exception ex) { Ui.Error($"{ex.GetType().Name}: {ex.Message}"); }
}
