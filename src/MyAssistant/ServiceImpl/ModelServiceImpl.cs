using MyAssistant.Core;
using MyAssistant.IServices;
using MyAssistant.Models;
using System.Text.Json;

namespace MyAssistant.ServiceImpl
{
    public class ModelServiceImpl : IModelService
    {
        private readonly IConfiguration _configuration;
        private readonly KernelContext _kernelContext;
        private readonly ILogger<ModelServiceImpl> _logger;

        public ModelServiceImpl(
            IConfiguration configuration,
            KernelContext kernelContext,
            ILogger<ModelServiceImpl> logger)
        {
            _configuration = configuration;
            _kernelContext = kernelContext;
            _logger = logger;
        }

        public List<ModelConfig> GetAvailableModels()
        {
            return _kernelContext.GetAvailableModels();
        }

        public async Task SwitchModelAsync(string modelName)
        {
            _kernelContext.BuildKernelByModel(modelName);
            _logger.LogInformation($"Switched to model: {modelName}");
        }

        public async Task UpdateModelConfigsAsync(string newConfigsJson)
        {
            try
            {
                var newConfigs = JsonSerializer.Deserialize<List<ModelConfig>>(newConfigsJson);
                if (newConfigs == null || !newConfigs.Any())
                    throw new ArgumentException("ModelConfigs cannot be empty");

                // 验证配置
                foreach (var config in newConfigs)
                {
                    if (!config.IsValid())
                        throw new ArgumentException($"Invalid config: {config.Model}");
                }

                // 更新appsettings.json
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(appSettingsPath));
                appSettings["ModelConfigs"] = newConfigs;
                await File.WriteAllTextAsync(appSettingsPath,
                    JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true }));

                // 重新初始化内核
                _kernelContext.ResetKernel();
                _logger.LogInformation("Model configs updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update model configs");
                throw;
            }
        }
    }
}
