using System.Text;
using PromptLab;
using PromptLab.Demos;

// Session 02 · Prompt Engineering & Getting Useful Output
// dotnet run          → menu
// dotnet run -- 5     → jump straight to demo 5
// dotnet run -- all   → run every demo in order (rehearsal)

Console.OutputEncoding = Encoding.UTF8;

var demos = new (string Name, Func<Task> Run)[]
{
    ("Vague vs specific prompt",          D1_VagueVsSpecific.Run),
    ("Same question, 3 system prompts",   D2_SystemPrompt.Run),
    ("Zero-shot vs few-shot triage",      D3_FewShot.Run),
    ("Run it three times (scored)",       D4_RunItThreeTimes.Run),
    ("Just ask for JSON (naive)",         D5_NaiveJson.Run),
    ("JSON mode + validate + retry",      D6_JsonMode.Run),
    ("Structured outputs (strict)",       D7_StructuredOutputs.Run),
    ("Prompt injection",                  D8_PromptInjection.Run),
};

if (!Llm.HasKey)
{
    Ui.Bad("GROQ_API_KEY is not set in this terminal.");
    Ui.Dim("   PowerShell:  $env:GROQ_API_KEY = \"gsk_...\"");
    Ui.Dim("   bash/zsh:    export GROQ_API_KEY=\"gsk_...\"");
    return 1;
}

Ui.Title("PromptLab · Session 02");
Ui.Dim($"   endpoint {Llm.BaseUrl}");
Ui.Dim($"   model    {Llm.Model}   ·   strict model {Llm.StrictModel}");

var pick = args.FirstOrDefault();
while (true)
{
    if (pick is null)
    {
        Console.WriteLine();
        for (int i = 0; i < demos.Length; i++) Console.WriteLine($"   {i + 1}. {demos[i].Name}");
        Console.WriteLine("   q. quit");
        Console.Write("\n   Pick a demo: ");
        pick = Console.ReadLine()?.Trim();
        if (pick is null) return 0; // stdin closed
    }
    if (pick is "q" or "Q") return 0;

    var toRun = pick == "all"
        ? Enumerable.Range(0, demos.Length).ToList()
        : int.TryParse(pick, out var n) && n >= 1 && n <= demos.Length ? [n - 1] : new List<int>();
    if (toRun.Count == 0) Ui.Warn($"Unknown choice '{pick}'.");

    foreach (var i in toRun)
    {
        try { await demos[i].Run(); }
        catch (LlmException ex) { Explain(ex); }
        catch (HttpRequestException ex) { Ui.Bad("Network problem: " + ex.Message); Ui.Dim("   Try the hotspot, or play the backup recording."); }
        catch (TaskCanceledException) { Ui.Bad("Timed out after 90s. Provider slow or network down."); }
    }

    if (args.Length > 0) return 0; // non-interactive: run once and exit
    pick = null;
}

static void Explain(LlmException ex)
{
    Ui.Bad($"HTTP {ex.Status}");
    Ui.Box(Ui.Clip(Json.Pretty(ex.Body), 600), ConsoleColor.DarkYellow);
    var hint = ex.Status switch
    {
        401 => "Bad or missing key. Re-set GROQ_API_KEY in THIS terminal.",
        404 => "Model id not found. Check LLM_MODEL / STRICT_MODEL against console.groq.com/docs/models.",
        429 => "Rate limited. Wait 60s, or set LLM_MODEL=llama-3.1-8b-instant.",
        400 when ex.Body.Contains("json", StringComparison.OrdinalIgnoreCase)
            => "The provider rejected the JSON output or the response_format. Great teaching moment: that's validation doing its job.",
        400 => "Bad request. Often: this model doesn't support a parameter (e.g. json_schema). Try STRICT_MODEL=openai/gpt-oss-120b.",
        >= 500 => "Provider problem. Retry in a moment, or switch model.",
        _ => "Unexpected. Read the body above.",
    };
    Ui.Warn(hint);
}
