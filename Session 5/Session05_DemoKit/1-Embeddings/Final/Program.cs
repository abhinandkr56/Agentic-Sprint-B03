// Build 1 · Embeddings (FINAL: where you end up after the edits)
using System.ClientModel;
using System.Numerics.Tensors;
using Microsoft.Extensions.AI;
using OpenAI;

// EDIT 1: same three lines as a chat client, three strings changed. GetEmbeddingClient instead of GetChatClient.
// Ollama ignores the key, but the OpenAI SDK insists on one, so any non-empty string works.
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GEMINI_API_KEY")),
        new OpenAIClientOptions { Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai") })
    .GetEmbeddingClient("gemini-embedding-001")
    .AsIEmbeddingGenerator();

var embedding = await embedder.GenerateAsync("UPI payment failed but money was deducted");
var vector = embedding.Vector;
Console.WriteLine($"{vector.Length} numbers: {string.Join("  ", vector[..5].ToArray().Select(x => x.ToString("F3")))} …");

// EDIT 2: two sentences in, one number out. Cosine similarity: 1 = same direction (same meaning), 0 = unrelated.
while (true)
{
    Console.Write("\nA> ");
    var a = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(a)) break;
    Console.Write("B> ");
    var b = Console.ReadLine()!;

    var va = await embedder.GenerateVectorAsync(a);
    var vb = await embedder.GenerateVectorAsync(b);
    Console.WriteLine($"similarity: {TensorPrimitives.CosineSimilarity(va.Span, vb.Span):F2}");
}
