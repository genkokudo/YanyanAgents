using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Chat;
Console.WriteLine("Hello, Work!");

// Azure OpenAI接続
var azureClient = new AzureOpenAIClient(
    new Uri("https://<your-resource>.openai.azure.com"),
    new DefaultAzureCredential()
);

var chatClient = azureClient.GetChatClient("<your-deployment-name>");

// エージェント作成
var agent = chatClient.AsAIAgent(
    instructions: "あなたは親切なアシスタントです。",
    name: "YanyanAgent"
);

// テスト実行
Console.WriteLine("YanyanAgent 起動！");
var response = await agent.RunAsync("こんにちは！自己紹介して。");
Console.WriteLine($"Agent: {response}");