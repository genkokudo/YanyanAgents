using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace YanyanAgents.ChatPattern;

// 質問例：「パソコンのデスクトップ壁紙として飾るなら、「推しキャラ」と「自分のオリジナルキャラ」のどちらにしますか？」

public class GroupChat : IMultiAgentChat
{
    public string PatternName => "GroupChat（論説ディベート）";

    private readonly Workflow _workflow;

    public GroupChat(ChatClient chatClient)
    {
        // ★ 賛成・反対・中立の3エージェント
        var proAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは「賛成派」の論客です。
            与えられたテーマに対して積極的に賛成の立場で論拠を述べてください。
            他のエージェントの発言を踏まえて反論や補強もしてください。
            発言は簡潔に2〜3文でまとめてください。
            """,
            "賛成派",
            "テーマに賛成する立場で議論する");

        var conAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは「反対派」の論客です。
            与えられたテーマに対して反対の立場で論拠を述べてください。
            他のエージェントの発言を踏まえて反論や補強もしてください。
            発言は簡潔に2〜3文でまとめてください。
            """,
            "反対派",
            "テーマに反対する立場で議論する");

        var neutralAgent = new ChatClientAgent(chatClient.AsIChatClient(),
            """
            あなたは「中立派」の論客です。
            与えられたテーマについて賛成・反対両方の視点を踏まえて中立的に分析してください。
            最後の発言では全体の議論をまとめた結論を述べてください。
            発言は簡潔に2〜3文でまとめてください。
            """,
            "中立派",
            "中立の立場で分析・まとめをする");

        // ★ RoundRobin：賛成→反対→中立→賛成→... の順で発言
        //    MaximumIterationCount = 6 で各エージェントが2回ずつ発言
        _workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 6
                })
            .AddParticipants(proAgent, conAgent, neutralAgent)
            .Build();
    }

    public async IAsyncEnumerable<string> RunTurnAsync(
        string userInput,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.User, $"ディベートのテーマ：「{userInput}」について議論してください。")
        };

        await using var run = await InProcessExecution.RunStreamingAsync(_workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastExecutorId = null;

        await foreach (var evt in run.WatchStreamAsync().WithCancellation(ct))
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update:
                    if (update.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = update.ExecutorId;
                        yield return $"\n\n--- [{update.ExecutorId}] ---\n";
                    }
                    yield return update.Update.Text ?? "";
                    break;

                case WorkflowOutputEvent:
                    yield break;
            }
        }
    }
}
