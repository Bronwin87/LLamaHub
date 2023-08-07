using LLama.Abstractions;

namespace LLamaHub.Core.Config
{
    public interface ISessionConfig : IInferenceParams
    {
        string AntiPrompt { get; set; }
        LLamaExecutorType ExecutorType { get; set; }
        string Model { get; set; }
        string OutputFilter { get; set; }
        string Prompt { get; set; }
    }
}