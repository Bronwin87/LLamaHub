using LLama.Abstractions;
using LLama.Native;

namespace LLamaHub.Core.LLamaSharp
{
    public class LLamaModelUtils
    {
        public static LLamaContextParams CreateContextParams(IModelParams modelParams)
        {
            if (!File.Exists(modelParams.ModelPath))
                throw new FileNotFoundException($"The model file does not exist: {modelParams.ModelPath}");

            var lparams = NativeApi.llama_context_default_params();
            lparams.n_ctx = modelParams.ContextSize;
            lparams.n_batch = modelParams.BatchSize;
            lparams.main_gpu = modelParams.MainGpu;
            lparams.n_gpu_layers = modelParams.GpuLayerCount;
            lparams.seed = modelParams.Seed;
            lparams.f16_kv = modelParams.UseFp16Memory;
            lparams.use_mmap = modelParams.UseMemoryLock;
            lparams.use_mlock = modelParams.UseMemoryLock;
            lparams.logits_all = modelParams.Perplexity;
            lparams.embedding = modelParams.EmbeddingMode;
            lparams.low_vram = modelParams.LowVram;
            lparams.n_gqa = modelParams.GroupedQueryAttention;
            lparams.rms_norm_eps = modelParams.RmsNormEpsilon;
            lparams.rope_freq_base = modelParams.RopeFrequencyBase;
            lparams.rope_freq_scale = modelParams.RopeFrequencyScale;
            lparams.mul_mat_q = modelParams.MulMatQ;
            lparams.tensor_split = modelParams.TensorSplits;

            return lparams;
        }
    }
}