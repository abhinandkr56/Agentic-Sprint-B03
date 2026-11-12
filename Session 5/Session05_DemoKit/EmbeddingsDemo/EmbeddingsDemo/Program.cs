// How to enable dark mode

// Chat model -> grok -> openai/gpt-oss-120b
// all-minilm
// Gemini - > gemini-embedding-001

using System.ClientModel;
using System.Numerics.Tensors;
using CommunityToolkit.VectorData.InMemory;
using CommunityToolkit.VectorData.PgVector;
using EmbeddingsDemo;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

IEmbeddingGenerator<string, Embedding<float>> embeddor =
    new OpenAIClient(new ApiKeyCredential(Environment.GetEnvironmentVariable("GEMINI_API_KEY")),
        new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta")
        }).GetEmbeddingClient("gemini-embedding-001").AsIEmbeddingGenerator();

List<Faq> faqs = new List<Faq>()
{
    new Faq()
    {
        Id = 1,
        Text = "If a UPI payment fails but money is deducted, the bank reverses it automatically within 3-5 working days."
    },
    new Faq()
    {
        Id = 2,
        Text = "GST invoices are emailed on the 1st of every month and can be downloaded from Settings > Billing."
    },
    new Faq()
    {
        Id = 3,
        Text = "We accept UPI, credit and debit cards, and net banking. Cheques are not accepted."
    },
    new Faq()
    {
        Id = 4,
        Text = "To reset your password, click 'Forgot password' on the login page. The link expires in 15 minutes."
    },
    new Faq()
    {
        Id = 5,
        Text = "Invite teammates from Settings > Team. The Starter plan allows up to 5 users; Pro has no limit."
    }
};
var docText = await File.ReadAllTextAsync("");

embeddor.GenerateAsync(docText);

var store = new PostgresVectorStore("Host=localhost;Username=postgres;Password=postgres",
    new PostgresVectorStoreOptions()
    {
        EmbeddingGenerator = embeddor
    });

var collection = store.GetCollection<int, Faq>("faqs");
await collection.EnsureCollectionExistsAsync();
collection.UpsertAsync(faqs);


Console.WriteLine("Ask?");
var query = Console.ReadLine();

await foreach (var items in collection.SearchAsync(query, top: 3, 
                   new VectorSearchOptions<Faq>()
               {
                   ScoreThreshold = 0.5f
               }))
{
    Console.WriteLine(items.Record.Text);
}


// Microsoft.extension.vectordata
// CommunityToolkit.VectorData.Inmemmory
// CommunityToolkit.VectorData.pgvector
