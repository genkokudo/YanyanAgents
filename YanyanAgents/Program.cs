using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.Json;
using YanyanAgents;

// 設定を読み込んでクライアントを作成
var azureConfig = AzureOpenAIConfig.FromDefaultFiles();
var client = azureConfig.CreateClient();
var chatClient = client.GetChatClient(azureConfig.Deployment);

// エージェント作成
var agent = chatClient.AsAIAgent(
    instructions: File.ReadAllText("Plugins/MyPrompt.dat"),
    name: "YanyanAgent"
);

// テスト実行
Console.WriteLine("YanyanAgent 起動！");
var response = await agent.RunAsync("こんにちは！");
Console.WriteLine($"Agent: {response}");
