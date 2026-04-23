using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using YanyanAgents;
using YanyanAgents.ChatPattern;

// 設定を読み込んでクライアントを作成
var azureConfig = AzureOpenAIConfig.FromDefaultFiles();
var client = azureConfig.CreateClient();
var chatClient = client.GetChatClient(azureConfig.Deployment);

var patterns = new (string Label, string Description, Func<IMultiAgentChat>)[]
{
    ("Sequential（直列）",
     "調査エージェントが情報を整理し、ライターエージェントがまとめる。\n" +
     "  向いてる用途：レポート作成・段階的な処理",
     () => new SequentialChat(chatClient)),

    ("Handoff（振り分け）",
     "受付が質問を見てIT・人事・総務の専門担当へ動的に振り分ける。\n" +
     "  向いてる用途：問い合わせ対応・専門家への委任",
     () => new HandoffChat(chatClient)),

    ("GroupChat（議論）",
     "賛成・反対・中立の3エージェントが同じ場で議論し多角的に分析する。\n" +
     "  向いてる用途：意思決定支援・アイデア検討",
     () => new GroupChat(chatClient)),
};

Console.OutputEncoding = Encoding.UTF8;

while (true)
{
    // ── メニュー表示 ──────────────────────────
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║   マルチエージェント パターン比較アプリ  ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.WriteLine();

    for (int i = 0; i < patterns.Length; i++)
    {
        var (label, desc, _) = patterns[i];
        Console.WriteLine($"  [{i + 1}] {label}");
        Console.WriteLine($"      {desc}");
        Console.WriteLine();
    }
    Console.WriteLine("  [0] 終了");
    Console.WriteLine();
    Console.Write("パターンを選択 > ");

    var input = Console.ReadLine()?.Trim();

    if (input == "0") break;

    if (!int.TryParse(input, out int choice)
        || choice < 1 || choice > patterns.Length)
    {
        Console.WriteLine("⚠ 正しい番号を入力してください。");
        continue;
    }

    // ── チャット開始 ──────────────────────────
    var (selectedLabel, _, factory) = patterns[choice - 1];
    IMultiAgentChat chat = factory();   // ★ ここで初めてインスタンス生成

    Console.WriteLine();
    Console.WriteLine($"【{chat.PatternName}】を開始します。");
    Console.WriteLine("'back' で戻ります。");
    Console.WriteLine(new string('─', 44));

    while (true)
    {
        Console.WriteLine();
        Console.Write("あなた: ");
        var userInput = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(userInput)) continue;
        if (userInput.Equals("back", StringComparison.OrdinalIgnoreCase)) break;

        Console.WriteLine();
        Console.Write("AI: ");

        try
        {
            await foreach (var chunk in chat.RunTurnAsync(userInput))
            {
                Console.Write(chunk);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n⚠ エラーが発生しました: {ex.Message}");
        }

        Console.WriteLine();
    }
}

Console.WriteLine("終了します。お疲れ様でした！");