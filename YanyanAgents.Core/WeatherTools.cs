using System.ComponentModel;

namespace YanyanAgents.Core;

public static class WeatherTools
{
    [Description("指定した都市の天気を取得する")]
    public static string GetWeather(
        [Description("都市名")] string city)
    {
        return $"{city}の天気は晴れです";  // ← 中身はそのままでOK
    }
}

//// 登録方法
//var tool = AIFunctionFactory.Create(WeatherTools.GetWeather);

//    var agent = chatClient.AsAIAgent(
//        name: "YanyanAgent",
//        instructions: "...",
//        tools: [tool]
//    );