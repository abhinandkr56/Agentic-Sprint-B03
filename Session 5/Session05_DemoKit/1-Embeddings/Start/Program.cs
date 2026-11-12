// Build 1 · Embeddings (START: open this before the call)
// Session 3 gave us IChatClient: text in, text out.
// Tonight's new interface is IEmbeddingGenerator: text in, a list of numbers out.
using System.ClientModel;
using System.Numerics.Tensors;
using Microsoft.Extensions.AI;
using OpenAI;

// ▼ EDIT 1: Groq has no embedding models. Point it at Ollama (three strings change)
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetEmbeddingClient("llama-3.3-70b-versatile")
    .AsIEmbeddingGenerator();

var embedding = await embedder.GenerateAsync("UPI payment failed but money was deducted");
var vector = embedding.Vector;
Console.WriteLine($"{vector.Length} numbers: {string.Join("  ", vector[..5].ToArray().Select(x => x.ToString("F3")))} …");

// ▼ EDIT 2: two sentences in, one number out: how close are they in meaning?
