namespace MyAssistant.Models
{
    public class ModelConfig
    {
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Model) &&
                   !string.IsNullOrWhiteSpace(Endpoint) &&
                   Uri.IsWellFormedUriString(Endpoint, UriKind.Absolute) &&
                   !string.IsNullOrWhiteSpace(ApiKey);
        }
    }
}
