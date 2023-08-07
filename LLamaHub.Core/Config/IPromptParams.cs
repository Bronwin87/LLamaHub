namespace LLamaHub.Core.Config
{
    public interface IPromptParams
    {
        List<string> AntiPrompt { get; set; }
        string Name { get; set; }
        List<string> OutputFilter { get; set; }
        string Path { get; set; }
        string Prompt { get; set; }
    }
}