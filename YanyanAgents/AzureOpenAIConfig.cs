using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace YanyanAgents;

public class AzureOpenAIConfig
{
    public string Deployment { get; }
    public string Endpoint { get; }
    public string? ApiKey { get; }

    public static AzureOpenAIConfig FromDefaultFiles()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        return new AzureOpenAIConfig(config);
    }

    // コンストラクタ：設定ファイルから読み込んでバリデーション
    public AzureOpenAIConfig(IConfiguration config)
    {
        var section = config.GetSection("AzureOpenAI");

        Deployment = section["Deployment"]
            ?? throw new InvalidOperationException(
                "Configuration value 'AzureOpenAI:Deployment' is missing or empty.");

        Endpoint = section["Endpoint"]
            ?? throw new InvalidOperationException(
                "Configuration value 'AzureOpenAI:Endpoint' is missing or empty.");

        // ApiKeyはオプション（nullでもDefaultAzureCredentialにフォールバック）
        ApiKey = section["ApiKey"];
    }

    // AzureOpenAIClientをサクッと作れるファクトリメソッド
    public AzureOpenAIClient CreateClient()
    {
        return string.IsNullOrWhiteSpace(ApiKey)
            ? new AzureOpenAIClient(new Uri(Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(Endpoint), new Azure.AzureKeyCredential(ApiKey));
    }
}
