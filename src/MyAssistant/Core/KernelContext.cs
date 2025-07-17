using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using MyAssistant.Models;
using OpenAI;
using System.ClientModel;
using Qdrant.Client;
using static Qdrant.Client.Grpc.Qdrant;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Memory;
using System;
namespace MyAssistant.Core
{
    public class KernelContext
    {
        private Kernel? _kernel;
        private readonly IConfiguration _configuration;
        private readonly QdrantSupport _qdrantSupport;
        private readonly ILogger<KernelContext> _logger;
        private ModelConfig? _currentModel;
        private QdrantVectorStore qdrantVectorStore;

        public Kernel Current
        {
            get
            {
                if (_kernel == null)
                {
                    BuildDefaultKernel();
                }
                return _kernel!;
            }
        }

        /// <summary>
        /// 向量存储库
        /// </summary>
        public QdrantVectorStore Store
        {
            get
            {
                if (qdrantVectorStore == null)
                {
                    qdrantVectorStore = SetVectorStore().GetAwaiter().GetResult();
                }
                return qdrantVectorStore;
            }
        }



        /// <summary>
        /// 当前使用的模型配置
        /// </summary>
        public ModelConfig? CurrentModel => _currentModel;

        /// <summary>
        /// 语义内核
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="qdrantSupport"></param>
        /// <param name="logger"></param>
        public KernelContext(IConfiguration configuration, QdrantSupport qdrantSupport, ILogger<KernelContext> logger)
        {
            _configuration = configuration;
            _qdrantSupport = qdrantSupport;
            _logger = logger;
        }

        /// <summary>
        /// 构建默认的 Kernel（使用第一个配置）
        /// </summary>
        public void BuildDefaultKernel()
        {
            var modelConfigs = GetModelConfigs();
            var defaultModel = modelConfigs.FirstOrDefault()
                ?? throw new InvalidOperationException("默认模型配置无效。");

            BuildKernelWithModel(defaultModel);
        }

        /// <summary>
        /// 根据模型名称构建 Kernel
        /// </summary>
        /// <param name="modelName">模型名称</param>
        public void BuildKernelByModel(string modelName)
        {
            var modelConfigs = GetModelConfigs();
            var targetModel = modelConfigs.FirstOrDefault(x => x.Model.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"未找到模型配置: {modelName}");

            BuildKernelWithModel(targetModel);
        }

        /// <summary>
        /// 使用指定模型配置构建 Kernel
        /// </summary>
        /// <param name="modelConfig">模型配置</param>
        private void BuildKernelWithModel(ModelConfig modelConfig)
        {
            if (!modelConfig.IsValid())
            {
                throw new InvalidOperationException($"模型配置无效：{modelConfig.Model}");
            }

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(
                modelConfig.Model,
                new Uri(modelConfig.Endpoint),
                modelConfig.ApiKey
            );

            _kernel = kernelBuilder.Build();
            _currentModel = modelConfig;

            _logger.LogInformation($"Kernel 已切换到模型: {modelConfig.Model}");
        }

        /// <summary>
        /// 添加向量检索库
        /// </summary>
        /// <returns></returns>
        private async Task<QdrantVectorStore> SetVectorStore()
        {
            try
            {
                var modelConfig = _configuration.GetSection("EmbeddingModel").Get<ModelConfig>();
                // 确保 Qdrant 运行
                _logger.LogInformation("检查并启动 Qdrant 服务...");
                var qdrantStarted = await _qdrantSupport.EnsureQdrantRunningAsync();

                if (!qdrantStarted)
                {
                    throw new InvalidOperationException("无法启动 Qdrant 服务");
                }

                var qdrantEndpoint = _qdrantSupport.GetQdrantEndpoint();
                _logger.LogInformation($"使用 Qdrant 端点: {qdrantEndpoint}");

                var embeddingGenerator = new OpenAIClient(new ApiKeyCredential(modelConfig.ApiKey), new OpenAIClientOptions() { Endpoint = new Uri(modelConfig.Endpoint) })
                                    .GetEmbeddingClient(modelConfig.Model)
                                    .AsIEmbeddingGenerator();

                var vectorStore = new QdrantVectorStore(new Qdrant.Client.QdrantClient(qdrantEndpoint), true, new QdrantVectorStoreOptions
                {
                    EmbeddingGenerator = embeddingGenerator
                });
                _logger.LogInformation("Kernel Memory 初始化完成");
                return vectorStore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建 Kernel Memory 失败");
                throw;
            }
        }

        /// <summary>
        /// 根据模型名称切换 Kernel
        /// </summary>
        /// <param name="modelName">模型名称</param>
        public async Task SwitchModelAsync(string modelName)
        {
            _logger.LogInformation($"切换模型到: {modelName}");

            // 重新构建 Kernel
            BuildKernelByModel(modelName);

        }

        /// <summary>
        /// 获取所有可用的模型配置
        /// </summary>
        /// <returns></returns>
        public List<ModelConfig> GetAvailableModels()
        {
            return GetModelConfigs();
        }

        /// <summary>
        /// 获取模型配置的私有方法
        /// </summary>
        /// <returns></returns>
        private List<ModelConfig> GetModelConfigs()
        {
            var modelConfigs = _configuration.GetSection("ModelConfigs").Get<List<ModelConfig>>();
            if (modelConfigs == null || !modelConfigs.Any())
            {
                throw new InvalidOperationException("未找到模型配置。");
            }
            return modelConfigs;
        }

        /// <summary>
        /// 重新初始化 Kernel（使用当前模型）
        /// </summary>
        public void ResetKernel()
        {
            _kernel = null;
            if (_currentModel != null)
            {
                BuildKernelWithModel(_currentModel);
            }
            else
            {
                BuildDefaultKernel();
            }
        }

        /// <summary>
        /// 获取 Qdrant 状态
        /// </summary>
        /// <returns></returns>
        public async Task<QdrantStatus> GetQdrantStatusAsync()
        {
            return await _qdrantSupport.GetStatusAsync();
        }

        public async Task CreateAsync<T>(string collectionName, IEnumerable<T> records) where T : class
        {
            var collection = Store.GetCollection<string, T>(collectionName);
            await collection.EnsureCollectionExistsAsync();
            await collection.UpsertAsync(records);
        }

        public async Task<IEnumerable<T>> ReadAsync<T>(string collectionName, IEnumerable<string> keys) where T : class
        {
            var collection = Store.GetCollection<string, T>(collectionName);
            var records = new List<T>();
            await foreach (var record in collection.GetAsync(keys))
            {
                records.Add(record);
            }
            return records;
        }

        public async Task UpdateAsync<T>(string collectionName, IEnumerable<T> records) where T:class
        {
            var collection = Store.GetCollection<string, T>(collectionName);
            await collection.UpsertAsync(records);
        }

        public async Task DeleteAsync<T>(string collectionName, IEnumerable<string> keys) where T : class
        {
            var collection = Store.GetCollection<string, T>(collectionName);
            await collection.DeleteAsync(keys);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _qdrantSupport?.Dispose();
        }
    }
}