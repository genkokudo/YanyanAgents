# 基本的なチャット
```
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
```

# チャットの保存や復元を行う方法

```
// 保存
var serialized = agent.SerializeSession(session);
File.WriteAllText("session.json", serialized);

// 復元
var saved = File.ReadAllText("session.json");
AgentSession resumedSession = await agent.DeserializeSessionAsync(saved);

// 続きから再開
string response = await agent.RunAsync("さっきの話の続きやけど...", resumedSession);
```

# MCPサーバーに繋ぐ方法

```
// MCPサーバーに繋いでみる
// ①MCPクライアント作成（例：filesystemサーバー）
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "filesystem",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "."],
});
await using var mcpClient = await McpClient.CreateAsync(clientTransport);

// ②サーバーが持ってるツール一覧を取得
var mcpTools = await mcpClient.ListToolsAsync();

foreach (var tool in mcpTools)
{
    Console.WriteLine($"Tool: {tool.Name}");
}

var mcpAgent = chatClient.AsAIAgent(
    instructions: "あなたはファイルシステムを操作できるアシスタントです。",
    name: "YanyanMcpAgent",
    tools: [.. mcpTools.Cast<AITool>()]  // ←ここでMCPツールを渡す
);

// ④実行
var mcpResponse = await mcpAgent.RunAsync("カレントディレクトリのファイル一覧を教えて");
Console.WriteLine(mcpResponse.Text);
```
