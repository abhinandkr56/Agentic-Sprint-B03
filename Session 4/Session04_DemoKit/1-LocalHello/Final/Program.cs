using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

IChatClient chat = new OpenAIClient(new ApiKeyCredential("Password"),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri("http://localhost:11434/v1")
    }).GetChatClient("llama3.2:1b").AsIChatClient();

    Console.WriteLine("Ask");
    
    var question = Console.ReadLine();

    var clock = new Stopwatch();
    clock.Start();
    
    var response = await chat.GetResponseAsync<Person>(question, new ChatOptions()
    {
        Temperature = 0.7f,
    });
    
Console.WriteLine(clock.ElapsedMilliseconds);
Console.WriteLine(response.Text);




    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Birthday { get; set; }
    }