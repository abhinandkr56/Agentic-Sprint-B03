var builder = DistributedApplication.CreateBuilder(args);

var groqKey = builder.AddParameter("groq-key", secret: true);
var geminiKey = builder.AddParameter("gemini-key", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17")
    .WithDataVolume();
var vectors = postgres.AddDatabase("vectors");

var ragApi = builder.AddProject<Projects.RagApi>("ragapi")
    .WithEnvironment("GROQ_API_KEY", groqKey)
    .WithEnvironment("GEMINI_API_KEY", geminiKey)
    .WithReference(vectors)
    .WaitFor(vectors)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
