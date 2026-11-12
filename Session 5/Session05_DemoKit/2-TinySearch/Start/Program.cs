// Build 2 · TinySearch (START: open this before the call)
// A search engine in a for-loop. No database yet: embed everything, compare, sort.
using System.ClientModel;
using System.Numerics.Tensors;
using Microsoft.Extensions.AI;
using OpenAI;

// Build 1's embedder
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

// Our help centre: 10 short FAQ answers for the same Indian SaaS company as Session 3's tickets
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

// ▼ EDIT 1: embed all 10 FAQs in ONE call

while (true)
{
    Console.Write("\nSearch> ");
    var query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) break;

    // ▼ EDIT 2: embed the query, score every FAQ, print the top 3
    // ▼ EDIT 3: no good match? say so
}
