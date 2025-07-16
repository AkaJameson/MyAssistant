using Microsoft.SemanticKernel;
using MyAssistant.Models;

namespace MyAssistant.Core
{
    public class KernelContext
    {
        private Kernel Kernel;
        private IConfiguration configuration;

        public Kernel Current
        {
            get
            {
                if (Kernel == null)
                {
                    BuildDefaultKernel();
                }
                return Kernel;
            }
            set
            {
                Kernel = value;
            }
        }
        /// <summary>
        /// 语义内核
        /// </summary>
        /// <param name="configuration"></param>
        public KernelContext(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void BuildDefaultKernel()
        {
            var modelConfigs = configuration.GetSection("ModelConfigs").Get<List<ModelConfig>>();
            if (modelConfigs == null || !modelConfigs.Any())
            {
                throw new InvalidOperationException("未找到模型配置。");
            }

            var defaultModel = modelConfigs.FirstOrDefault()
                ?? throw new InvalidOperationException("默认模型配置无效。");
            if (!defaultModel.IsValid())
            {
                throw new InvalidOperationException($"默认模型配置无效：{defaultModel.Model}");
            }

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(defaultModel.Model, new Uri(defaultModel.Endpoint), defaultModel.ApiKey);
            Kernel = kernelBuilder.Build();
        }
    }
}
