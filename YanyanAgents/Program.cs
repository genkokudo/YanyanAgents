using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.Net;
using System.Security.Cryptography;
using YanyanAgents;
using YanyanAgents.ChatPattern;

// 設定を読み込んでクライアントを作成
var azureConfig = AzureOpenAIConfig.FromDefaultFiles();
var client = azureConfig.CreateClient();
var chatClient = client.GetChatClient(azureConfig.Deployment);




// ★ここを差し替えるだけで別パターンに切り替えられる
//SequentialChat chat = new(chatClient);
//HandoffChat chat = new(chatClient);
GroupChat chat = new(chatClient);
// あと、MagenticChatもありますが、C#は未対応なので今回は割愛する。これは、「何をどう進めるかも含めて自分で考える」複雑タスクなので他のパターンとは毛色が異なる。


Console.WriteLine($"=== {chat.PatternName} のデモ ===");
Console.WriteLine("終了するには 'exit' と入力してください\n");

while (true)
{
    Console.Write("あなた: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input == "exit") break;

    Console.Write("AI: ");
    await foreach (var chunk in chat.RunTurnAsync(input))
    {
        Console.Write(chunk);
    }
    Console.WriteLine();
}

