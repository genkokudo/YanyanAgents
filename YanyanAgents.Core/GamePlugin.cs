using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace YanyanAgents.Core;

// 呼び方例：
//var chatClient = azureClient.GetChatClient(azureConfig.Deployment)
//                            .AsIChatClient(); // ←これでIChatClientに変換
//var plugin = new GamePlugin(chatClient);

public class GamePlugin(IChatClient chatClient)
{

    // ──────────────────────────────────────────────
    // プレイヤー入力の要約
    // ──────────────────────────────────────────────

    /// <summary>
    /// プレイヤーの入力文章をAIに要約させる。
    /// 長文・悪意ある入力をAIが自然に圧縮してくれる効果もある。
    /// </summary>
    [Description("プレイヤーの入力を簡潔に要約する")]
    public async Task<string> SummarizePlayerInputAsync(
    [Description("プレイヤーが入力した文章")] string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput))
            return "（何もしなかった）";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "あなたは文章を簡潔にまとめる要約係です。"),
            new(ChatRole.User,
                $"以下の文章を100文字以内で要約してください。\n" +
                $"要約文だけを出力し、余計な説明は不要です。\n\n" +
                $"{playerInput}")
        };

        var result = await chatClient.GetResponseAsync(messages);
        return result.Text ?? playerInput;
    }

    //[Description("プレイヤーの入力を簡潔に要約する")]
    //public async Task<string> SummarizePlayerInputAsync(
    //    [Description("プレイヤーが入力した文章")] string playerInput)
    //{
    //    if (string.IsNullOrWhiteSpace(playerInput))
    //        return "（何もしなかった）";

    //    var history = new ChatHistory();
    //    history.AddSystemMessage(
    //        "あなたは文章を簡潔にまとめる要約係です。");
    //    history.AddUserMessage(
    //        $"以下の文章を100文字以内で要約してください。\n" +
    //        $"要約文だけを出力し、余計な説明は不要です。\n\n" +
    //        $"{playerInput}");

    //    var result = await chat.GetChatMessageContentAsync(history);
    //    return result.Content ?? playerInput;
    //}

    // ──────────────────────────────────────────────
    // プレイヤー行動の評価
    // ──────────────────────────────────────────────

    /// <summary>
    /// プレイヤーの行動を評価して成功/失敗・評価コメント・次の展開を返す。
    /// AIの返答をパースする処理があるためC#で実装。
    /// </summary>
    [Description("プレイヤーの行動を評価する")]
    public async Task<(bool IsSuccess, string Evaluation, string NextSituation)> EvaluateActionAsync(
        [Description("現在の問題・状況")] string problem,
        [Description("プレイヤーの行動・台詞（要約済み）")] string playerAction,
        [Description("ゲームの舞台設定")] string setting)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                $"あなたはTRPGのゲームマスターです。舞台: {setting}"),
            new(ChatRole.User,
                $"状況: {problem}\n" +
                $"プレイヤーの行動: {playerAction}\n\n" +
                "この行動を評価してください。\n" +
                "形式（この形式を厳守してください）:\n" +
                "結果: 成功 or 失敗\n" +
                "評価: （2～3文で評価コメント）\n" +
                "次の展開: （1～2文で次の状況）")
        };

        var result = await chatClient.GetResponseAsync(messages);
        var text = result.Text ?? string.Empty;

        return ParseEvaluationResult(text);
    }

    // ──────────────────────────────────────────────
    // ラウンドのあらすじ要約
    // ──────────────────────────────────────────────

    /// <summary>
    /// 1ラウンドの出来事を50文字以内の1文に要約する。
    /// StoryLogに追記していくことで情報欠落なくあらすじを蓄積できる。
    /// </summary>
    [Description("直前のラウンドを1文で要約する")]
    public async Task<string> SummarizeRoundAsync(
        [Description("今回の状況")] string problem,
        [Description("プレイヤーの行動（要約済み）")] string playerAction,
        [Description("結果（成功/失敗）")] string result,
        [Description("次の展開")] string nextSituation)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "あなたはTRPGセッションの記録係です。簡潔な日本語で書いてください。"),
            new(ChatRole.User,
                $"状況: {problem}\n" +
                $"行動: {playerAction}\n" +
                $"結果: {result}\n" +
                $"次の展開: {nextSituation}\n\n" +
                "上記を50文字以内の1文で要約してください。\n" +
                "要約文だけを出力し、余計な説明は不要です。")
        };

        var aiResult = await chatClient.GetResponseAsync(messages);
        return aiResult.Text ?? $"{result}の展開があった。";
    }

    // ──────────────────────────────────────────────
    // ジャンルと部隊の生成
    // ──────────────────────────────────────────────
    /// <summary>
    /// ジャンルと舞台を自動生成する
    /// </summary>
    [Description("ゲームのジャンルと舞台をAIが自動生成する")]
    public async Task<(string Genre, string Setting)> GenerateSettingAsync()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "あなたはTRPGのゲームマスターです。"),
            new(ChatRole.User,
                "ゲームの舞台を1つ考えてください。\n" +
                "ジャンル（例: ファンタジー冒険、現代オフィス、SF宇宙船）と\n" +
                "具体的な舞台設定を1～2文で答えてください。\n" +
                "形式: ジャンル: XXX\n舞台: XXX")
        };

        var result = await chatClient.GetResponseAsync(messages);
        var text = result.Text ?? string.Empty;

        // パース
        var lines = text.Split('\n');
        var genre = lines.FirstOrDefault(l => l.StartsWith("ジャンル:"))
                        ?.Replace("ジャンル:", "").Trim() ?? "ファンタジー";
        var setting = lines.FirstOrDefault(l => l.StartsWith("舞台:"))
                          ?.Replace("舞台:", "").Trim() ?? "謎の王国";

        return (genre, setting);
    }

    // ──────────────────────────────────────────────
    // プライベート：パース処理
    // ──────────────────────────────────────────────

    /// <summary>
    /// EvaluateActionAsyncのAI応答テキストをパースする。
    /// 形式が崩れた場合のフォールバックも含む。
    /// </summary>
    private static (bool IsSuccess, string Evaluation, string NextSituation)
        ParseEvaluationResult(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var resultLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("結果:"));
        var evaluationLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("評価:"));
        var nextLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("次の展開:"));

        var isSuccess = resultLine?.Contains("成功") ?? false;
        var evaluation = evaluationLine?.Replace("評価:", "").Trim()
                         ?? "行動の結果が出た。";
        var next = nextLine?.Replace("次の展開:", "").Trim()
                         ?? "状況が変化した。";

        return (isSuccess, evaluation, next);
    }
}