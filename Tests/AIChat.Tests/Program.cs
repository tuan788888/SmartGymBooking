using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

var settings = new Dictionary<string,string?> { ["AI:Provider"] = "DEMO", ["AI:FallbackToDemo"] = "true", ["AI:OpenAI:Model"] = "gpt-6-luna", ["AI:OpenAI:MaxOutputTokens"] = "500" };
var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
var handler = new FakeHandler();
var online = new OpenAIChatService(new HttpClient(handler), config);
var demo = new DemoAIChatService();
var router = new AIChatRouterService(config, demo, online);
var customer = new Customer { FullName = "Test", Height = 175, Weight = 70, FitnessGoal = "MUSCLE_GAIN" };
var history = Enumerable.Range(1, 12).Select(n => new Aimessage { Role = n % 2 == 0 ? "ASSISTANT" : "USER", Content = "history-" + n }).ToList();
var oldKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
try
{
    Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-only-not-real");
    var answer = await router.GetResponseAsync(customer, "BMI của tôi", history, null);
    Check(answer.Replace(',', '.').Contains("22.9") && handler.Calls == 0, "Demo BMI / no API");
    answer = await demo.GetResponseAsync(customer, "giải thích giáo án", history, new WorkoutPlan { Title = "Test Plan" });
    Check(answer.Contains("Test Plan") && !answer.Contains("Xin chào"), "Keyword boundaries");
    config["AI:Provider"] = "OPENAI";
    handler.Status = HttpStatusCode.TooManyRequests;
    answer = await router.GetResponseAsync(customer, "BMI", history, null);
    Check(answer.Contains("chuyển sang Demo AI") && answer.Replace(',', '.').Contains("22.9"), "429 fallback");
    config["AI:FallbackToDemo"] = "false";
    try { await router.GetResponseAsync(customer, "BMI", history, null); throw new Exception("Failure swallowed"); }
    catch (HttpRequestException) { }
    handler.Status = HttpStatusCode.OK;
    handler.Body = """{"status":"completed","output":[{"type":"reasoning"},{"type":"message","content":[{"type":"output_text","text":"Part 1"},{"type":"output_text","text":"Part 2"}]}]}""";
    answer = await router.GetResponseAsync(customer, "current", history, null);
    Check(answer == "Part 1\nPart 2", "All output text parts");
    using (var payload = JsonDocument.Parse(handler.Payload!))
    {
        var root = payload.RootElement;
        Check(root.GetProperty("model").GetString() == "gpt-6-luna", "Requested model");
        Check(root.GetProperty("store").GetBoolean() == false, "Store disabled");
        var input = root.GetProperty("input");
        Check(input.GetArrayLength() == 11 && input[0].GetProperty("content").GetString() == "history-3", "History capped");
        Check(input[10].GetProperty("content").GetString() == "current" && input[10].GetProperty("role").GetString() == "user", "Current message once");
    }
    handler.Body = """{"output":[{"content":[{"type":"refusal","refusal":"Cannot assist"}]}]}""";
    Check(await router.GetResponseAsync(customer, "current", history, null) == "Cannot assist", "Refusal text");
    handler.Body = """{"output":[]}""";
    try { await router.GetResponseAsync(customer, "current", history, null); throw new Exception("Empty output accepted"); } catch (InvalidOperationException) { }
    config["AI:FallbackToDemo"] = "true";
    Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
    var calls = handler.Calls;
    answer = await router.GetResponseAsync(customer, "BMI", history, null);
    Check(answer.Contains("Demo AI") && handler.Calls == calls, "Missing key fallback without request");
    Console.WriteLine("PASS: DEMO zero requests, BMI, keyword matching, 429 fallback, disabled fallback, Responses payload/history, multipart text, refusal, empty output and missing key.");
}
finally { Environment.SetEnvironmentVariable("OPENAI_API_KEY", oldKey); }
static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
class FakeHandler : HttpMessageHandler
{
    public int Calls;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string Body = "{}";
    public string? Payload;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Calls++;
        Payload = await request.Content!.ReadAsStringAsync(ct);
        return new HttpResponseMessage(Status) { Content = new StringContent(Body) };
    }
}

