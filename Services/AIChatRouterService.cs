using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public interface IAIChatService
{
    Task<string> GetResponseAsync(
        Customer customer,
        string message,
        List<Aimessage> history,
        WorkoutPlan? workoutPlan
    );
}

public class AIChatRouterService
    : IAIChatService
{
    private readonly IConfiguration _configuration;

    private readonly DemoAIChatService
        _demoService;

    private readonly OpenAIChatService
        _openAIService;

    private readonly ILogger<AIChatRouterService>? _logger;


    public AIChatRouterService(
        IConfiguration configuration,
        DemoAIChatService demoService,
        OpenAIChatService openAIService,
        ILogger<AIChatRouterService>? logger = null)
    {
        _configuration = configuration;

        _demoService = demoService;

        _openAIService = openAIService;
        _logger = logger;
    }


    public async Task<string> GetResponseAsync(
        Customer customer,
        string message,
        List<Aimessage> history,
        WorkoutPlan? workoutPlan)
    {
        var provider =
            _configuration["AI:Provider"]
                ?.Trim()
                .ToUpperInvariant()
            ?? "DEMO";



        if (provider == "OPENAI")
        {
            try
            {
                return await _openAIService
                    .GetResponseAsync(
                        customer,
                        message,
                        history,
                        workoutPlan
                    );
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("OpenAI chat failed: {ErrorType}, HTTP {StatusCode}",
                    ex.GetType().Name, (ex as HttpRequestException)?.StatusCode);
                var fallback =
                    _configuration.GetValue<bool>(
                        "AI:FallbackToDemo"
                    );


                if (!fallback)
                {
                    throw;
                }


                var demoResponse =
                    await _demoService
                        .GetResponseAsync(
                            customer,
                            message,
                            history,
                            workoutPlan
                        );


                return
                    "AI Online hiện không khả dụng. " +
                    "Hệ thống đã chuyển sang Demo AI.\n\n" +
                    (ex is AIProviderException providerError ? providerError.UserMessage + "\n\n" : "") +
                    demoResponse;
            }
        }



        return await _demoService
            .GetResponseAsync(
                customer,
                message,
                history,
                workoutPlan
            );
    }
}
