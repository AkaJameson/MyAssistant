using MyAssistant.Models;

namespace MyAssistant.IServices
{
    public interface IModelService
    {
        List<ModelConfig> GetAvailableModels();
        Task SwitchModelAsync(string modelName);
        Task UpdateModelConfigsAsync(string newConfigsJson);
    }
}
