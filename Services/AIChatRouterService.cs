using SmartGymBooking.Models;

namespace SmartGymBooking.Services
{
    public class AIChatRouterService
        : IAIChatService
    {
        private readonly IConfiguration _configuration;

        private readonly DemoAIChatService
            _demoService;

        private readonly OpenAIChatService
            _openAIService;


        public AIChatRouterService(
            IConfiguration configuration,
            DemoAIChatService demoService,
            OpenAIChatService openAIService)
        {
            _configuration = configuration;

            _demoService = demoService;

            _openAIService = openAIService;
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


            // ============================
            // OPENAI MODE
            // ============================

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
                catch
                {
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
                        demoResponse;
                }
            }


            // ============================
            // DEMO MODE - DEFAULT
            // ============================

            return await _demoService
                .GetResponseAsync(
                    customer,
                    message,
                    history,
                    workoutPlan
                );
        }
    }
}
