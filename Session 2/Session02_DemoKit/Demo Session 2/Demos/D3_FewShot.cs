namespace PromptLab.Demos;

/// <summary>
/// Demo 3 · Zero-shot vs few-shot on ticket triage.
/// Zero-shot: the model invents its own labels ("Payment Issue", "billing", "Refund Request"...).
/// Few-shot: an allowed list plus three worked examples, and the labels lock in.
/// </summary>
public static class D3_FewShot
{
    const string ZeroShotSystem = "Classify the customer support ticket into a category. Reply with the category only.";

    static readonly string FewShotSystem = $"""
        You triage customer support tickets.
        Reply with exactly one category from this list and nothing else:
        {string.Join(", ", Samples.TicketCategories)}
        """;

    /// <summary>Few-shot examples are sent as fake earlier turns: user asks, assistant answers.</summary>
    static readonly Msg[] Examples =
    [
        Msg.User("My card was billed but the invoice still says unpaid."), Msg.Assistant("Billing"),
        Msg.User("Search returns a 500 error when I type an apostrophe."),  Msg.Assistant("Bug"),
        Msg.User("Where do I change my notification settings?"),           Msg.Assistant("HowTo"),
    ];

    public static async Task Run()
    {
        Ui.Title("Demo 3 · Zero-shot vs few-shot");
        Ui.Label($"{Samples.Tickets.Length} tickets, same model, temperature 0.7");
        Ui.Pause();

        var zero = new List<string>();
        var few = new List<string>();
        foreach (var t in Samples.Tickets)
        {
            var z = await Llm.AskAsync(ZeroShotSystem, t, temperature: 0.7, maxTokens: 20);
            var msgs = new List<Msg> { Msg.System(FewShotSystem) };
            msgs.AddRange(Examples);
            msgs.Add(Msg.User(t));
            var f = await Llm.ChatAsync(msgs, temperature: 0.7, maxTokens: 20);
            zero.Add(z.Content.Trim().Trim('.', '"'));
            few.Add(f.Content.Trim().Trim('.', '"'));
        }

        Console.WriteLine();
        Console.WriteLine($"   {"Ticket",-52} {"Zero-shot",-24} Few-shot");
        Console.WriteLine("   " + new string('─', 96));
        for (int i = 0; i < Samples.Tickets.Length; i++)
        {
            var ok = Samples.TicketCategories.Contains(few[i]);
            Console.Write($"   {Ui.Trunc(Samples.Tickets[i], 50),-52} ");
            Ui.Inline(ConsoleColor.Yellow, $"{Ui.Trunc(zero[i], 23),-24} ");
            Ui.Write(ok ? ConsoleColor.Green : ConsoleColor.Red, few[i]);
        }

        Console.WriteLine();
        int zeroValid = zero.Count(Samples.TicketCategories.Contains);
        int fewValid = few.Count(Samples.TicketCategories.Contains);
        Ui.Label($"Zero-shot: {zero.Distinct().Count()} different labels, {zeroValid}/{zero.Count} from our list");
        Ui.Label($"Few-shot:  {few.Distinct().Count()} different labels, {fewValid}/{few.Count} from our list");
        Ui.Good("Three examples did more than three paragraphs of instructions would.");
    }
}
