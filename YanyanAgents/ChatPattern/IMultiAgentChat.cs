using System;
using System.Collections.Generic;
using System.Text;

namespace YanyanAgents.ChatPattern;

public interface IMultiAgentChat
{
    /// <summary>パターン名（表示用）</summary>
    string PatternName { get; }

    /// <summary>ユーザー入力を受け取って会話を1ターン実行する</summary>
    IAsyncEnumerable<string> RunTurnAsync(string userInput, CancellationToken ct = default);
}
