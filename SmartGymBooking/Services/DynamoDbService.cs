using System.Net.Http.Json;

namespace SmartGymBooking.Services
{
    public class DynamoDbService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DynamoDbService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> TestConnectionAsync()
        {
            var apiUrl = _configuration["AWS:ApiUrl"];

            if (string.IsNullOrEmpty(apiUrl))
            {
                return "API URL chưa được cấu hình.";
            }

            var response = await _httpClient.GetAsync(
                $"{apiUrl}/api/test");

            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}