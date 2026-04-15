// skprompt.txtの内容をinstructionsに移す
var agent = chatClient.AsAIAgent(
    instructions: File.ReadAllText("Plugins/ChatGame/MyPrompt.dat"),
    name: "YanyanAgent"
);