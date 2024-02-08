using AICentralTests.TestHelpers;
using AICentralWeb;
using Xunit.Abstractions;

namespace AICentralTests.Assistants;

public class open_ai_assistants
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public open_ai_assistants(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    public async Task can_be_mapped_to_allow_load_balancing()
    {
        
    }

}