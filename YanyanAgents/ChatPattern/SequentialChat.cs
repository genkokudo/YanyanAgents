using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace YanyanAgents.ChatPattern;

// 「こんにちは。ソフトウェアのUIをデザインするにあたって、大事なことは何ですか？」のように質問する。
// researcherが調べ物をする。
// writerがresearcherの調査結果をまとめる。

public class SequentialChat : IMultiAgentChat
{
    public string PatternName => "Sequential（直列）";

    private readonly Workflow _workflow;
    private readonly List<Microsoft.Extensions.AI.ChatMessage> _history = [];

    public SequentialChat(ChatClient chatClient)
    {
        // ★ここがポイント：役割の違うエージェントを2つ定義する
        var researcher = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは調査担当エージェントです。
            ユーザーの質問に対して、重要なポイントを箇条書きで調査・整理してください。
            """,
            "researcher");

        var writer = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたはライター担当エージェントです。
            前のエージェントが整理した情報をもとに、わかりやすい日本語で説明文にまとめてください。
            """,
            "writer");

        // ★Sequential：researcher → writer の順番で実行される
        _workflow = AgentWorkflowBuilder.BuildSequential(researcher, writer);
    }

    public async IAsyncEnumerable<string> RunTurnAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _history.Add(new(ChatRole.User, userInput));

        // ★ StreamAsync → RunStreamingAsync に修正
        await using var run = await InProcessExecution.RunStreamingAsync(_workflow, _history);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync().WithCancellation(ct))
        {
            // まず全イベントの型名を出力して確認する
            //yield return $"[DEBUG] {evt.GetType().Name} / ExecutorId: {evt.Data}\n";

            switch (evt)
            {
                case ExecutorInvokedEvent invoked:
                    yield return $"\n--- [{invoked.ExecutorId}] ---\n";
                    break;

                case AgentResponseUpdateEvent update:
                    yield return update.Update.Text ?? "";
                    break;

                case WorkflowOutputEvent output:
                    var results = output.As<List<Microsoft.Extensions.AI.ChatMessage>>();
                    if (results is not null)
                        _history.AddRange(results.Where(m => m.Role == ChatRole.Assistant));
                    yield break;
            }
        }
    }
}