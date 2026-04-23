using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace YanyanAgents.ChatPattern;

// 受付担当がユーザーの質問を聞いて、内容に応じてIT、人事、総務の専門担当に引き継ぐパターン
// 「PCが起動しない」
// → 受付が質問を聞いてIT担当へ引き継ぐ

// 「有給の申請方法を教えて」
// → 受付が質問を聞いて人事担当へ引き継ぐ


public class HandoffChat : IMultiAgentChat
{
    public string PatternName => "Handoff（社内ヘルプデスク）";

    private readonly Workflow _workflow;
    private readonly List<Microsoft.Extensions.AI.ChatMessage> _history = [];

    public HandoffChat(ChatClient chatClient)
    {
        var triage = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは社内ヘルプデスクの受付担当です。
            ユーザーの質問を以下のルールで必ず専門担当へ引き継いでください。
            - PCトラブル・システム・ネットワーク・アカウント関連 → IT担当へ
            - 給与・休暇・採用・労務・福利厚生関連             → 人事担当へ
            - 備品・経費・施設・契約・郵便関連                 → 総務担当へ
            自分では回答せず、必ず適切な担当へHandoffしてください。
            """,
            "triage",
            "受付。質問を分類して適切な担当エージェントへ引き継ぐ");

        var itAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは社内ITサポート担当です。
            PCトラブル、システム障害、ネットワーク、アカウント管理などの質問に答えてください。
            回答後、他に質問があれば受付（triage）に戻してください。
            """,
            "it_support",
            "IT担当。PC・システム・ネットワーク・アカウントの質問を処理する");

        var hrAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは社内人事担当です。
            給与、休暇申請、採用、労務、福利厚生などの質問に答えてください。
            回答後、他に質問があれば受付（triage）に戻してください。
            """,
            "hr",
            "人事担当。給与・休暇・採用・福利厚生の質問を処理する");

        var generalAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは社内総務担当です。
            備品発注、経費精算、施設予約、契約、郵便などの質問に答えてください。
            回答後、他に質問があれば受付（triage）に戻してください。
            """,
            "general_affairs",
            "総務担当。備品・経費・施設・契約の質問を処理する");

        // ★ WithHandoff(A, B), WithHandoff(B, A) → WithHandoffs([A,B], C) で双方向を1行で書ける
#pragma warning disable MAAIW001 // 種類は、評価の目的でのみ提供されています。将来の更新で変更または削除されることがあります。続行するには、この診断を非表示にします。
        _workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(triage)
            .WithHandoffs(triage, [itAgent, hrAgent, generalAgent])
            .WithHandoffs([itAgent, hrAgent, generalAgent], triage)
            .Build();
#pragma warning restore MAAIW001 // 種類は、評価の目的でのみ提供されています。将来の更新で変更または削除されることがあります。続行するには、この診断を非表示にします。
    }

    public async IAsyncEnumerable<string> RunTurnAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _history.Add(new(ChatRole.User, userInput));

        await using var run = await InProcessExecution.RunStreamingAsync(_workflow, _history);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastExecutorId = null;

        await foreach (var evt in run.WatchStreamAsync().WithCancellation(ct))
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update:
                    // ★エージェントが切り替わったタイミングだけヘッダーを出す
                    if (update.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = update.ExecutorId;
                        yield return $"\n\n--- [{update.ExecutorId}] ---\n";
                    }
                    yield return update.Update.Text ?? "";
                    break;

                case WorkflowOutputEvent output:
                    var results = output.As<List<Microsoft.Extensions.AI.ChatMessage>>();
                    if (results is not null)
                        _history.AddRange(results.Skip(_history.Count));
                    yield break;
            }
        }
    }
}
