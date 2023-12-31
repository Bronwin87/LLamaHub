﻿using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Exceptions;
using LLama.Native;
using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace LLamaHub.Core.LLamaSharp
{
    using llama_token = Int32;
    /// <summary>
    /// The abstraction of a context over a LLama model
    /// </summary>
    public class LLamaHubModelContext : IDisposable
    {
        private readonly Encoding _encoding;
        private readonly LLamaHubModel _model;
        private readonly SafeLLamaContextHandle _context;

        /// <summary>
        /// The context size.
        /// </summary>
        public int ContextSize { get; }

        /// <summary>
        /// The model params set for this model.
        /// </summary>
        public LLamaHubModel Model => _model;

        /// <summary>
        /// The native handle, which is used to be passed to the native APIs. Please avoid using it 
        /// unless you know what is the usage of the Native API.
        /// </summary>
        public SafeLLamaContextHandle NativeHandle => _context;

        /// <summary>
        /// The encoding set for this model to deal with text input.
        /// </summary>
        public Encoding Encoding => _encoding;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model">Model instance.</param>
        /// <param name="encoding">Encoding to deal with text input.</param>
        /// <param name="logger">The logger.</param>
        public LLamaHubModelContext(LLamaHubModel model, string encoding = "UTF-8", ILLamaLogger logger = null)
        {
            _model = model;
            _encoding = Encoding.GetEncoding(encoding);

            var contextParams = LLamaModelUtils.CreateContextParams(_model.Params);
            _context = SafeLLamaContextHandle.Create(model.NativeHandle, contextParams);
            ContextSize = NativeApi.llama_n_ctx(_context);
        }

        /// <summary>
        /// Tokenize a string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="addBos">Whether to add a bos to the text.</param>
        /// <returns></returns>
        public IEnumerable<llama_token> Tokenize(string text, bool addBos = true)
        {
            // TODO: reconsider whether to convert to array here.
            return Utils.Tokenize(_context, text, addBos, _encoding);
        }

        /// <summary>
        /// Detokenize the tokens to text.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public string DeTokenize(IEnumerable<llama_token> tokens)
        {
            StringBuilder sb = new();
            foreach (var token in tokens)
            {
                sb.Append(Utils.PtrToString(NativeApi.llama_token_to_str(_context, token), _encoding));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Save the state to specified path.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveState(string filename)
        {
            // Delete that file before overwriting it
            if (File.Exists(filename))
                File.Delete(filename);

            // Estimate size of state to write to disk, this is always equal to or greater than the actual size
            var estimatedStateSize = (long)NativeApi.llama_get_state_size(_context);

            // Map the file and write the bytes directly to it. This saves copying the bytes into a C# array
            long writtenBytes;
            using (var file = MemoryMappedFile.CreateFromFile(filename, FileMode.Create, null, estimatedStateSize))
            using (var view = file.CreateViewAccessor(0, estimatedStateSize))
            {
                unsafe
                {
                    byte* ptr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    writtenBytes = (long)NativeApi.llama_copy_state_data(_context, ptr);
                    view.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }

            // Truncate the file to the actual size of data that was written
            using (var fileStream = new FileStream(filename, FileMode.Open))
                fileStream.SetLength(writtenBytes);
        }

        /// <summary>
        /// Get the state data as a byte array.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use `GetState` instead, this supports larger states (over 2GB)")]
        public byte[] GetStateData()
        {
            var stateSize = NativeApi.llama_get_state_size(_context);
            byte[] stateMemory = new byte[stateSize];
            NativeApi.llama_copy_state_data(_context, stateMemory);
            return stateMemory;
        }

        /// <summary>
        /// Get the state data as an opaque handle
        /// </summary>
        /// <returns></returns>
        public State GetState()
        {
            var stateSize = NativeApi.llama_get_state_size(_context);

            unsafe
            {
                var bigMemory = Marshal.AllocHGlobal((nint)stateSize);
                var smallMemory = IntPtr.Zero;
                try
                {
                    // Copy the state data into "big memory", discover the actual size required
                    var actualSize = NativeApi.llama_copy_state_data(_context, (byte*)bigMemory);

                    // Allocate a smaller buffer
                    smallMemory = Marshal.AllocHGlobal((nint)actualSize);

                    // Copy into the smaller buffer and free the large one to save excess memory usage
                    Buffer.MemoryCopy(bigMemory.ToPointer(), smallMemory.ToPointer(), actualSize, actualSize);
                    Marshal.FreeHGlobal(bigMemory);
                    bigMemory = IntPtr.Zero;

                    return new State(smallMemory);
                }
                catch
                {
                    if (bigMemory != IntPtr.Zero)
                        Marshal.FreeHGlobal(bigMemory);
                    if (smallMemory != IntPtr.Zero)
                        Marshal.FreeHGlobal(smallMemory);
                    throw;
                }
            }
        }

        /// <summary>
        /// Load the state from specified path.
        /// </summary>
        /// <param name="filename"></param>
        /// <exception cref="RuntimeError"></exception>
        public void LoadState(string filename)
        {
            // Map state file into memory and pass that pointer directly to `llama_set_state_data` to load from
            using (var file = MemoryMappedFile.CreateFromFile(filename, FileMode.Open, null))
            using (var view = file.CreateViewAccessor())
            {
                unsafe
                {
                    byte* ptr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    NativeApi.llama_set_state_data(_context, ptr);
                    view.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        /// <summary>
        /// Load the state from memory.
        /// </summary>
        /// <param name="stateData"></param>
        /// <exception cref="RuntimeError"></exception>
        public void LoadState(byte[] stateData)
        {
            int stateSize = (int)NativeApi.llama_get_state_size(_context);
            if (stateData.Length > stateSize)
            {
                throw new RuntimeError("Failed to validate state size.");
            }
            NativeApi.llama_set_state_data(_context, stateData);
        }

        /// <summary>
        /// Load the state from memory.
        /// </summary>
        /// <param name="state"></param>
        /// <exception cref="RuntimeError"></exception>
        public void LoadState(State state)
        {
            unsafe
            {
                NativeApi.llama_set_state_data(_context, (byte*)state.DangerousGetHandle().ToPointer());
            }
        }

        /// <summary>
        /// Perform the sampling. Please don't use it unless you fully know what it does.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="mirostat_mu"></param>
        /// <param name="temperature"></param>
        /// <param name="mirostat"></param>
        /// <param name="mirostatTau"></param>
        /// <param name="mirostatEta"></param>
        /// <param name="topK"></param>
        /// <param name="topP"></param>
        /// <param name="tfsZ"></param>
        /// <param name="typicalP"></param>
        /// <returns></returns>
        public llama_token Sample(LLamaTokenDataArray candidates, ref float mirostat_mu, float temperature = 0.8f, MirostatType mirostat = MirostatType.Disable,
                                  float mirostatTau = 5.0f, float mirostatEta = 0.1f, int topK = 40, float topP = 0.95f, float tfsZ = 1.0f, float typicalP = 1.0f)
        {
            llama_token id;
            if (temperature <= 0)
            {
                // Greedy sampling
                id = SamplingApi.llama_sample_token_greedy(_context, candidates);
            }
            else
            {
                if (float.IsNaN(mirostat_mu))
                    mirostat_mu = 2 * mirostatTau;

                if (mirostat == MirostatType.Mirostat)
                {
                    const int mirostat_m = 100;
                    SamplingApi.llama_sample_temperature(_context, candidates, temperature);
                    id = SamplingApi.llama_sample_token_mirostat(_context, candidates, mirostatTau, mirostatEta, mirostat_m, ref mirostat_mu);
                }
                else if (mirostat == MirostatType.Mirostat2)
                {
                    SamplingApi.llama_sample_temperature(_context, candidates, temperature);
                    id = SamplingApi.llama_sample_token_mirostat_v2(_context, candidates, mirostatTau, mirostatEta, ref mirostat_mu);
                }
                else
                {
                    // Temperature sampling
                    SamplingApi.llama_sample_top_k(_context, candidates, topK, 1);
                    SamplingApi.llama_sample_tail_free(_context, candidates, tfsZ, 1);
                    SamplingApi.llama_sample_typical(_context, candidates, typicalP, 1);
                    SamplingApi.llama_sample_top_p(_context, candidates, topP, 1);
                    SamplingApi.llama_sample_temperature(_context, candidates, temperature);
                    id = SamplingApi.llama_sample_token(_context, candidates);
                }
            }
            return id;
        }

        /// <summary>
        /// Apply the penalty for the tokens. Please don't use it unless you fully know what it does.
        /// </summary>
        /// <param name="lastTokens"></param>
        /// <param name="logitBias"></param>
        /// <param name="repeatLastTokensCount"></param>
        /// <param name="repeatPenalty"></param>
        /// <param name="alphaFrequency"></param>
        /// <param name="alphaPresence"></param>
        /// <param name="penalizeNL"></param>
        /// <returns></returns>
        public LLamaTokenDataArray ApplyPenalty(IEnumerable<llama_token> lastTokens, Dictionary<llama_token, float> logitBias = null,
            int repeatLastTokensCount = 64, float repeatPenalty = 1.1f, float alphaFrequency = .0f, float alphaPresence = .0f,
            bool penalizeNL = true)
        {
            var n_vocab = NativeApi.llama_n_vocab(_context);
            var logits = Utils.GetLogits(_context, n_vocab);

            // Apply params.logit_bias map
            if (logitBias is not null)
            {
                foreach (var (key, value) in logitBias)
                {
                    logits[key] += value;
                }
            }

            var candidates = new LLamaTokenData[n_vocab];
            for (llama_token token_id = 0; token_id < n_vocab; token_id++)
                candidates[token_id] = new LLamaTokenData(token_id, logits[token_id], 0.0f);
            LLamaTokenDataArray candidates_p = new LLamaTokenDataArray(candidates);

            // Apply penalties
            float nl_logit = logits[NativeApi.llama_token_nl()];
            int lastTokensCount = lastTokens.Count();
            var last_n_repeat = Math.Min(Math.Min(lastTokensCount, repeatLastTokensCount), ContextSize);
            SamplingApi.llama_sample_repetition_penalty(_context, candidates_p,
                lastTokens.Skip(lastTokensCount - last_n_repeat).ToArray(),
                (ulong)last_n_repeat, repeatPenalty);
            SamplingApi.llama_sample_frequency_and_presence_penalties(_context, candidates_p,
                lastTokens.Skip(lastTokensCount - last_n_repeat).ToArray(),
                (ulong)last_n_repeat, alphaFrequency, alphaPresence);
            if (!penalizeNL)
            {
                logits[NativeApi.llama_token_nl()] = nl_logit;
            }

            return candidates_p;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="pastTokensCount"></param>
        /// <returns>The updated `pastTokensCount`.</returns>
        /// <exception cref="RuntimeError"></exception>
        public llama_token Eval(llama_token[] tokens, llama_token pastTokensCount)
        {
            int total = tokens.Length;
            for (int i = 0; i < total; i += _model.Params.BatchSize)
            {
                int n_eval = total - i;
                if (n_eval > _model.Params.BatchSize)
                {
                    n_eval = _model.Params.BatchSize;
                }

                if (Utils.Eval(_context, tokens, i, n_eval, pastTokensCount, _model.Params.Threads) != 0)
                {
                    throw new RuntimeError("Failed to eval.");
                }

                pastTokensCount += n_eval;
            }
            return pastTokensCount;
        }

        // TODO: add comment
        internal IEnumerable<string> GenerateResult(IEnumerable<llama_token> ids)
        {
            foreach (var id in ids)
            {
                yield return Utils.TokenToString(id, _context, _encoding);
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// The state of this model, which can be reloaded later
        /// </summary>
        public class State
            : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal State(IntPtr memory)
                : base(true)
            {
                SetHandle(memory);
            }

            /// <inheritdoc />
            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}