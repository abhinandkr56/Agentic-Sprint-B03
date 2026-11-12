var builder = DistributedApplication.CreateBuilder(args);

var groqKey = builder.AddParameter("groq-key", secret: true);

var ragApi = builder.AddProject<Projects.RagApi>("ragapi")
    .WithEnvironment("GROQ_API_KEY", groqKey)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
