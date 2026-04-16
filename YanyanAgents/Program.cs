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


// エージェント作成（今まで通り）
var agent = chatClient.AsAIAgent(
//    instructions: File.ReadAllText("Plugins/MyPrompt.dat"),
    instructions: "あなたは親切なアシスタントです。",
    name: "YanyanAgent"
);

// セッション作成（←これがチャット履歴の本体）
AgentSession session = await agent.CreateSessionAsync();

// チャットループ
Console.WriteLine("YanyanAgent 起動！（exitで終了）");
while (true)
{
    Console.Write("You: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent: {response}");
}

//// 保存
//var serialized = agent.SerializeSession(session);
//File.WriteAllText("session.json", serialized);

//// 復元
//var saved = File.ReadAllText("session.json");
//AgentSession resumedSession = await agent.DeserializeSessionAsync(saved);

//// 続きから再開
//string response = await agent.RunAsync("さっきの話の続きやけど...", resumedSession);