using LLama;
using LLama.Abstractions;
using LLama.Native;

namespace LLamaHub.Core.LLamaSharp
{
    using llama_token = Int32;
    /// <summary>
    /// The LLama executor for instruct mode.
    /// </summary>
    public class LLamaHubInstructExecutor : LLamaHubStatefulExecutorBase
    {
        bool _is_prompt_run = true;
        string _instructionPrefix;
        llama_token[] _inp_pfx;
        llama_token[] _inp_sfx;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="instructionPrefix"></param>
        /// <param name="instructionSuffix"></param>
        public LLamaHubInstructExecutor(LLamaHubModelContext context, string instructionPrefix = "\n\n### Instruction:\n\n", string instructionSuffix = "\n\n### Response:\n\n")
            : base(context)
        {

            _inp_pfx = context.Tokenize(instructionPrefix, true).ToArray();
            _inp_sfx = context.Tokenize(instructionSuffix, false).ToArray();
            _instructionPrefix = instructionPrefix;
        }
      
        /// <inheritdoc />
        protected override bool GetLoopCondition(InferStateArgs args)
        {
            return args.RemainedTokens != 0 || _is_prompt_run;
        }
        /// <inheritdoc />
        protected override void PreprocessInputs(string text, InferStateArgs args)
        {
            if (args.Antiprompts is null)
            {
                args.Antiprompts = new List<string>();
            }
            args.Antiprompts.Add(_instructionPrefix);
            if (_is_prompt_run)
            {
                // When running the first input (prompt) in inteactive mode, we should specially process it.
                text = " " + text;
                _embed_inps = _context.Tokenize(text, true).ToList();
            }
            else
            {
                if (!text.EndsWith("\n"))
                {
                    text += "\n";
                }
                _consumedTokensCount = _embed_inps.Count;
                _embed_inps.AddRange(_inp_pfx);

                var line_inp = _context.Tokenize(text, false);
                _embed_inps.AddRange(line_inp);

                _embed_inps.AddRange(_inp_sfx);

                args.RemainedTokens -= line_inp.Count();
            }
        }
        /// <inheritdoc />
        protected override bool PostProcess(IInferenceParams inferenceParams, InferStateArgs args, out IEnumerable<string> extraOutputs)
        {
            extraOutputs = null;
            if (_embed_inps.Count <= _consumedTokensCount)
            {
                if (args.Antiprompts is not null && args.Antiprompts.Count > 0)
                {
                    string last_output = "";
                    foreach (var id in _last_n_tokens)
                    {
                        last_output += Utils.PtrToString(NativeApi.llama_token_to_str(_context.NativeHandle, id), _context.Encoding);
                    }

                    foreach (var antiprompt in args.Antiprompts)
                    {
                        if (last_output.EndsWith(antiprompt))
                        {
                            args.WaitForInput = true;
                            return true;
                        }
                    }
                }

                if (_pastTokensCount > 0 && args.WaitForInput)
                {
                    extraOutputs = new string[] { "\n> " };
                    return true;
                }
            }

            if (_embeds.Count > 0 && _embeds.Last() == NativeApi.llama_token_eos())
            {
                args.WaitForInput = true;
            }

            if (args.RemainedTokens <= 0 && inferenceParams.MaxTokens != -1)
            {
                args.RemainedTokens = inferenceParams.MaxTokens;
                args.WaitForInput = true;
            }
            return false;
        }
        /// <inheritdoc />
        protected override void InferInternal(IInferenceParams inferenceParams, InferStateArgs args)
        {
            if (_embeds.Count > 0)
            {
                _is_prompt_run = false;
                if (_pastTokensCount + _embeds.Count > _context.ContextSize)
                {
                    HandleRunOutOfContext(inferenceParams.TokensKeep);
                }

                TryReuseMathingPrefix();
                _pastTokensCount = _context.Eval(_embeds.ToArray(), _pastTokensCount);

                if (_embeds.Count > 0 && !string.IsNullOrEmpty(_pathSession))
                {
                    _session_tokens.AddRange(_embeds);
                    _n_session_consumed = _session_tokens.Count;
                }
            }

            _embeds.Clear();

            if (_embed_inps.Count <= _consumedTokensCount && !args.WaitForInput)
            {
                var repeat_last_n = inferenceParams.RepeatLastTokensCount < 0 ? _context.ContextSize : inferenceParams.RepeatLastTokensCount;

                var tokenDataArray = _context.ApplyPenalty(_last_n_tokens, inferenceParams.LogitBias, repeat_last_n,
                    inferenceParams.RepeatPenalty, inferenceParams.FrequencyPenalty, inferenceParams.PresencePenalty, inferenceParams.PenalizeNL);

                var mu = MirostateMu;
                var id = _context.Sample(
                    tokenDataArray, ref mu, inferenceParams.Temperature, inferenceParams.Mirostat, inferenceParams.MirostatTau,
                    inferenceParams.MirostatEta, inferenceParams.TopK, inferenceParams.TopP, inferenceParams.TfsZ, inferenceParams.TypicalP
                );
                MirostateMu = mu;

                _last_n_tokens.Enqueue(id);

                _embeds.Add(id);

                args.RemainedTokens--;
                args.ReturnValue = true;
            }
            else
            {
                while (_embed_inps.Count > _consumedTokensCount)
                {
                    _embeds.Add(_embed_inps[_consumedTokensCount]);
                    _last_n_tokens.Enqueue(_embed_inps[_consumedTokensCount]);
                    _consumedTokensCount++;
                    if (_embeds.Count >= _context.Model.Params.BatchSize)
                    {
                        break;
                    }
                }
            }
        }
    }
}
