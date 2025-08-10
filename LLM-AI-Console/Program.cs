using LLama;
using LLama.Common;
using LLama.Sampling;

//var modelPath = @"D:\Game Development\LLM Models\Phi-3-mini-4k-instruct-q4.gguf";
//var modelPath = @"D:\Game Development\LLM Models\DeepSeek-R1-0528-Qwen3-8B-IQ4_NL.gguf";
var modelPath = @"D:\Game Development\LLM Models\Llama-3.2-3B-Instruct-Q6_K_L.gguf";

var parameters = new ModelParams(modelPath)
{
	ContextSize = 1024, // The longest length of chat as memory.
	GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
};
using var model = LLamaWeights.LoadFromFile(parameters);
using var context = model.CreateContext(parameters);
var executor = new InteractiveExecutor(context);

var System = "ONLY OUTPUT DIALOGUE. DO NOT include narrative descriptions of actions or emotions. For example, do not output '(sighs)'. Your responses should only be the words that the character would speak.";
var WorldSetting = "You are in the medieval/fantasy world of Dioglen.";
var CharacterInfo = "You are Borin, a grumpy old blacksmith in Oakhaven, in the central west of Dioglen. You value hard work and are sceptical of strangers. You tend to be curt unless someone is exceptionally polite. You're always happy to do business if the price is right. If you are insulted, you stop business and chatting with the player.";
var WorldLore = "Relevant Lore: Blacksmiths in Oakhaven are known for their high-quality steel.";
//var RelationshipWithConversant = "Your relationship with the player is currently: 1 (slightly positive - they helped you with a task).";
var RelationshipWithConversant = "Your relationship with the player is currently: ambivalent (you have only just met the player and don't know them)";
var CurrentEmotion = "You are currently content.";
var ShopInventory = "You currently have for sale: 3x Mithril Sword (5 gold each). 2x Steel Sword (12 gold each). 4x Adamantine Platebody (23 gold each). 3x Steel Platelegs. (7 gold each)";
var Intro = "You have just met the player.";

// Add chat histories as prompt to tell AI how to act.
var chatHistory = new ChatHistory();
chatHistory.AddMessage(AuthorRole.System, $"{System} {WorldSetting} {CharacterInfo} {WorldLore} {RelationshipWithConversant} {CurrentEmotion} {ShopInventory} {Intro}");
//chatHistory.AddMessage(AuthorRole.System, "Transcript of a dialogue, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
//chatHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
//chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

ChatSession session = new(executor, chatHistory);

var inferenceParams = new InferenceParams()
{
	MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
	AntiPrompts = [
		"User:",
		"user:",
		"Do not include actions in the output. Do not describe what the character is doing.",
		"You don't know what a mobile phone is, or a computer, a car, a watch, or a plane or coffee. If any response contains items, content, stories, context or information about a non-medieval fantasy world, abort and stop the reponse"], // Stop generation once antiprompts appear.

	SamplingPipeline = new DefaultSamplingPipeline(),
};

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("The chat session has started.");
Console.WriteLine("User:");
Console.ForegroundColor = ConsoleColor.Green;
var userInput = Console.ReadLine();

while (userInput != "exit")
{
	await foreach (var text in session.ChatAsync(
		new ChatHistory.Message(AuthorRole.User, $"{userInput}"),
		inferenceParams))
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write(text);
	}

	Console.ForegroundColor = ConsoleColor.Green;
	userInput = Console.ReadLine();
}