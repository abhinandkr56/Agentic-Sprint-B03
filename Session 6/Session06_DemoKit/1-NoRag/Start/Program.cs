// Build 1 · NoRag (START: open this before the call)
// Session 3's chat client, asked about OUR company. It has never seen our help centre.
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

// Session 5's ten help-centre FAQs, as plain strings (faqs[9] is the refunds one)
string[] faqs =
[
    "If a UPI payment fails but money is deducted, the bank reverses it automatically within 3-5 working days.",
    "GST invoices are emailed on the 1st of every month and can be downloaded from Settings > Billing.",
    "We accept UPI, credit and debit cards, and net banking. Cheques are not accepted.",
    "To reset your password, click 'Forgot password' on the login page. The link expires in 15 minutes.",
    "Invite teammates from Settings > Team. The Starter plan allows up to 5 users; Pro has no limit.",
    "Export your invoices to Excel from Reports > Export, then choose .xlsx.",
    "Turn on dark mode under Profile > Appearance.",
    "The mobile app works offline and syncs your changes when you are back online.",
    "You can cancel your subscription anytime. You keep access until the end of the billing cycle.",
    "Refunds for annual plans are pro-rated and reach your account within 7 working days.",
];

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    // ▼ EDIT 1: paste the right FAQ into the prompt, by hand
    // ▼ EDIT 2: paste ALL ten FAQs, every time
    var prompt = question;

    var response = await chat.GetResponseAsync(prompt);
    Console.WriteLine(response.Text);
    Console.WriteLine($"  [{response.Usage?.InputTokenCount} tokens in]");
}
