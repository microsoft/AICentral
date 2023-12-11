using AICentral.Core;

namespace AICentral.Configuration;

    public class AICentralConfig
    {
        public bool EnableDiagnosticsHeaders { get; set; } = false;
        public bool EnableOpenTelemetry { get; set; } = false;
        public AICentralPipelineConfig[]? Pipelines { get; init; }
        public AICentralTypeAndNameConfig[]? Endpoints { get; set; }
        public AICentralTypeAndNameConfig[]? EndpointSelectors { get; set; }
        public AICentralTypeAndNameConfig[]? AuthProviders { get; set; }
        public AICentralTypeAndNameConfig[]? GenericSteps { get; set; }
        public HttpMessageHandler? HttpMessageHandler { get; set; }

        public void FillInPropertiesFromConfiguration(IConfigurationSection configurationSection)
        {
            Endpoints = FillCollection(nameof(Endpoints), configurationSection).ToArray();
            EndpointSelectors = FillCollection(nameof(EndpointSelectors), configurationSection).ToArray();
            AuthProviders = FillCollection(nameof(AuthProviders), configurationSection).ToArray();
            GenericSteps = FillCollection(nameof(GenericSteps), configurationSection).ToArray();
        }

        private List<AICentralTypeAndNameConfig> FillCollection(
            string property,
            IConfigurationSection configurationSection)
        {
            var newList = new List<AICentralTypeAndNameConfig>();
            foreach (var item in configurationSection.GetSection(property).GetChildren())
            {
                newList.Add(new AICentralTypeAndNameConfig()
                {
                    Name = Guard.NotNull(item.GetValue<string>("Name"), item, "Name"),
                    Type = Guard.NotNull(item.GetValue<string>("Type"), item, "Type"),
                    ConfigurationSection = item,
                });
            }

            return newList;
        }
    }