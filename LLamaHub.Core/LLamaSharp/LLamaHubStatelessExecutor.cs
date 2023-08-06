using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using System.Runtime.CompilerServices;

namespace LLamaHub.Core.LLamaSharp
{
    using llama_token = Int32;
    /// <summary>
    /// This executor infer the input as one-time job. Previous inputs won't impact on the 
    /// response to current input.
    /// </summary>
    public class LLamaHubStatelessExecutor : ILLamaHubExecutor
    {
        private LLamaHubModelContext _context;
        private LLamaHubModelContext.State _originalState;
        /// <summary>
        /// The mode used by the executor when running the inference.
        /// </summary>
        public LLamaHubModelContext Context => _context;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model">The LLama model.</param>
        public LLamaHubStatelessExecutor(LLamaHubModelContext context)
        {
            _context = context;

            var tokens = _context.Tokenize(" ", true).ToArray();
            Utils.Eval(_context.NativeHandle, tokens, 0, tokens.Length, 0, _context.Model.Params.Threads);
            _originalState = _context.GetState();
        }

        /// <inheritdoc />
        public IEnumerable<string> Infer(string text, IInferenceParams? inferenceParams = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int n_past = 1;
            if (inferenceParams is null)
            {
                inferenceParams = new InferenceParams();
            }
            List<llama_token> lastTokens = new(inferenceParams.RepeatLastTokensCount);
            for (int i = 0; i < lastTokens.Count; i++)
            {
                lastTokens[i] = 0;
            }
            List<llama_token> tokens = _context.Tokenize(text, true).ToList();
            int n_prompt_tokens = tokens.Count;

            Utils.Eval(_context.NativeHandle, tokens.ToArray(), 0, n_prompt_tokens, n_past, _context.Model.Params.Threads);

            lastTokens.AddRange(tokens);
            n_past += n_prompt_tokens;

            var mu = float.NaN;
            int max_tokens = inferenceParams.MaxTokens < 0 ? int.MaxValue : inferenceParams.MaxTokens;
            for (int i = 0; i < max_tokens; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _context.LoadState(_originalState);
                    break;
                }
                var repeat_last_n = inferenceParams.RepeatLastTokensCount < 0 ? _context.ContextSize : inferenceParams.RepeatLastTokensCount;

                var tokenDataArray = _context.ApplyPenalty(lastTokens, inferenceParams.LogitBias, repeat_last_n,
                    inferenceParams.RepeatPenalty, inferenceParams.FrequencyPenalty, inferenceParams.PresencePenalty, inferenceParams.PenalizeNL);

                var id = _context.Sample(tokenDataArray, ref mu, inferenceParams.Temperature, inferenceParams.Mirostat, inferenceParams.MirostatTau,
                    inferenceParams.MirostatEta, inferenceParams.TopK, inferenceParams.TopP, inferenceParams.TfsZ, inferenceParams.TypicalP);

                lastTokens.Add(id);

                string response = Utils.TokenToString(id, _context.NativeHandle, _context.Encoding);
                yield return response;

                tokens.Clear();
                tokens.Add(id);

                if (inferenceParams.AntiPrompts is not null && inferenceParams.AntiPrompts.Count() > 0)
                {
                    string last_output = "";
                    foreach (var token in lastTokens)
                    {
                        last_output += Utils.PtrToString(NativeApi.llama_token_to_str(_context.NativeHandle, id), _context.Encoding);
                    }

                    bool should_break = false;
                    foreach (var antiprompt in inferenceParams.AntiPrompts)
                    {
                        if (last_output.EndsWith(antiprompt))
                        {
                            should_break = true;
                            break;
                        }
                    }
                    if (should_break)
                    {
                        break;
                    }
                }

                // when run out of context
                if (n_past + tokens.Count > _context.ContextSize)
                {
                    int n_left = n_past - inferenceParams.TokensKeep;

                    n_past = Math.Max(1, inferenceParams.TokensKeep);

                    // insert n_left/2 tokens at the start of embed from last_n_tokens
                    tokens.InsertRange(0, lastTokens.Take(lastTokens.Count - tokens.Count).Skip(_context.ContextSize - n_left / 2 - tokens.Count));
                }

                n_past = _context.Eval(tokens.ToArray(), n_past);
            }

            _context.LoadState(_originalState);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> InferAsync(string text, IInferenceParams? inferenceParams = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var result in Infer(text, inferenceParams, cancellationToken))
            {
                yield return result;
            }
        }
    }
}
