using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers;

[Authorize(Roles = "CUSTOMER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AIChatController(ApplicationDbContext context, IAIChatService aiChatService,
    IConfiguration configuration, ILogger<AIChatController> logger) : Controller
{
    private const string CredentialNotice = "AI PT không cung cấp thông tin đăng nhập hoặc khóa truy cập. Chỉ gửi câu hỏi về tập luyện.";
    private const string HiddenMessage = "Nội dung nhạy cảm đã được ẩn.";

    private static readonly Regex CredentialPattern = new(
        @"\b(?:mat[\s_-]*khau|pass[\s_-]*(?:word|code)|passwd|pwd|mk|api[\s_-]*key|secret[\s_-]*key|access[\s_-]*token|bearer|credentials?|admin(?:istrator)?|quan[\s_-]*tri[\s_-]*vien)\b|sk-[a-z0-9_-]{8,}|[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static bool ContainsCredentials(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var normalized = new string(text.Normalize(NormalizationForm.FormKD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) is not
                (UnicodeCategory.NonSpacingMark or UnicodeCategory.Format))
            .ToArray()).Replace('đ', 'd').Replace('Đ', 'D');
        return CredentialPattern.IsMatch(normalized);
    }

    private Task<Customer?> GetCurrentCustomerAsync()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Task.FromResult<Customer?>(null);
        return context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? conversationId, bool newChat = false)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null) return Unauthorized();
        var conversations = context.Aiconversations.AsNoTracking().Where(c => c.CustomerId == customer.CustomerId);
        Aiconversation? conversation = null;
        if (conversationId.HasValue)
        {
            conversation = await conversations.SingleOrDefaultAsync(c => c.AiconversationId == conversationId.Value);
            if (conversation == null) return NotFound();
        }
        else if (!newChat)
            conversation = await conversations.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ThenByDescending(c => c.AiconversationId).FirstOrDefaultAsync();
        return View(await BuildViewModelAsync(customer.CustomerId, conversation));
    }

    private async Task<AIChatViewModel> BuildViewModelAsync(long customerId, Aiconversation? conversation)
    {
        var model = new AIChatViewModel
        {
            ConversationId = conversation?.AiconversationId,
            ConversationTitle = conversation?.Title ?? "AI Virtual PT",
            AIProvider = configuration["AI:Provider"]?.Trim().ToUpperInvariant() ?? "DEMO",
            Conversations = await context.Aiconversations.AsNoTracking().Where(c => c.CustomerId == customerId)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt).ThenByDescending(c => c.AiconversationId).ToListAsync(),
            Messages = conversation == null ? new() : await context.Aimessages.AsNoTracking()
                .Where(m => m.AiconversationId == conversation.AiconversationId)
                .OrderBy(m => m.CreatedAt).ThenBy(m => m.AimessageId).ToListAsync()
        };
        if (ContainsCredentials(model.ConversationTitle))
            model.ConversationTitle = HiddenMessage;
        foreach (var item in model.Conversations.Where(c => ContainsCredentials(c.Title)))
            item.Title = HiddenMessage;
        foreach (var item in model.Messages.Where(m => m.Content != CredentialNotice && ContainsCredentials(m.Content)))
            item.Content = HiddenMessage;

        var replies = model.Messages.Where(m => m.Role == "ASSISTANT").ToList();
        if (replies.Count == 0) return model;

        var exercises = await context.Exercises.AsNoTracking()
            .Where(e => e.IsActive || e.WorkoutExercises.Any(w => w.WorkoutPlan.CustomerId == customerId))
            .OrderBy(e => e.ExerciseId).Select(e => new { e.Name, e.Instructions, e.Description, e.VideoUrl }).ToListAsync();
        var guides = exercises.Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(e => e.Name, e => new ExerciseGuideViewModel(e.Name,
                string.IsNullOrWhiteSpace(e.Instructions) ? e.Description : e.Instructions, e.VideoUrl),
                StringComparer.OrdinalIgnoreCase);
        if (guides.Count == 0) return model;

        var names = string.Join("|", guides.Keys.OrderByDescending(name => name.Length).Select(Regex.Escape));
        var pattern = new Regex($@"(?<![\p{{L}}\p{{N}}])(?:{names})(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var reply in replies)
            model.ExerciseGuides[reply.AimessageId] = pattern.Matches(reply.Content)
                .Select(match => match.Value).Where(guides.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => guides[name]).ToList();
        return model;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NewConversation() => RedirectToAction(nameof(Index), new { newChat = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConversation(long conversationId)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null) return Unauthorized();

        await using var transaction = await context.Database.BeginTransactionAsync();
        var conversations = context.Aiconversations.Where(c =>
            c.AiconversationId == conversationId && c.CustomerId == customer.CustomerId);
        if (!await conversations.AnyAsync()) return NotFound();

        try
        {
            await context.Aimessages.Where(m => conversations.Any(c => c.AiconversationId == m.AiconversationId))
                .ExecuteDeleteAsync();
            if (await conversations.ExecuteDeleteAsync() == 0) return NotFound();
            await transaction.CommitAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 547 or 1205)
        {
            TempData["ErrorMessage"] = "Cuộc trò chuyện đang thay đổi. Vui lòng thử xóa lại.";
            return RedirectToAction(nameof(Index), new { conversationId });
        }

        TempData["SuccessMessage"] = "Đã xóa cuộc trò chuyện và toàn bộ tin nhắn.";
        return RedirectToAction(nameof(Index), new { newChat = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(AIChatViewModel model)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null) return Unauthorized();
        Aiconversation? conversation = null;
        if (model.ConversationId.HasValue)
        {
            conversation = await context.Aiconversations.SingleOrDefaultAsync(c =>
                c.AiconversationId == model.ConversationId.Value && c.CustomerId == customer.CustomerId);
            if (conversation == null) return NotFound();
        }
        var message = model.Message?.Trim() ?? string.Empty;
        if (message.Length == 0 && ModelState.IsValid)
            ModelState.AddModelError(nameof(model.Message), "Vui lòng nhập câu hỏi.");
        var sensitiveInput = ContainsCredentials(message);
        if (sensitiveInput)
        {
            ModelState.Remove(nameof(model.Message));
            ModelState.AddModelError(nameof(model.Message), CredentialNotice);
        }
        if (!ModelState.IsValid)
        {
            var viewModel = await BuildViewModelAsync(customer.CustomerId, conversation);
            viewModel.Message = sensitiveInput ? string.Empty : message;
            return View("Index", viewModel);
        }
        var history = conversation == null ? new List<Aimessage>() : await context.Aimessages.AsNoTracking()
            .Where(m => m.AiconversationId == conversation.AiconversationId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.AimessageId).Take(10).ToListAsync();
        history.RemoveAll(m => ContainsCredentials(m.Content));
        history.Reverse();
        conversation ??= new Aiconversation
        {
            CustomerId = customer.CustomerId,
            Title = message.Length <= 40 ? message : message[..(char.IsHighSurrogate(message[39]) ? 39 : 40)] + "...",
            CreatedAt = DateTime.Now
        };
        if (conversation.AiconversationId == 0) context.Aiconversations.Add(conversation);
        conversation.Aimessages.Add(new Aimessage { Role = "USER", Content = message, CreatedAt = DateTime.Now });
        conversation.UpdatedAt = DateTime.Now;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return RedirectToAction(nameof(Index), new { newChat = true });
        }


        var workoutPlan = await context.WorkoutPlans.AsNoTracking().Include(p => p.WorkoutExercises)
            .ThenInclude(w => w.Exercise).Where(p => p.CustomerId == customer.CustomerId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.WorkoutPlanId).FirstOrDefaultAsync();
        try
        {
            var response = await aiChatService.GetResponseAsync(customer, message, history, workoutPlan);
            if (!await context.Aiconversations.AnyAsync(c => c.AiconversationId == conversation.AiconversationId))
                return RedirectToAction(nameof(Index), new { newChat = true });

            conversation.Aimessages.Add(new Aimessage
            {
                Role = "ASSISTANT",
                Content = ContainsCredentials(response) ? CredentialNotice : response,
                CreatedAt = DateTime.Now
            });
            conversation.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("AI chat failed for conversation {ConversationId} ({ErrorType})",
                conversation.AiconversationId, ex.GetType().Name);
            TempData["ErrorMessage"] = ex is AIProviderException providerError
                ? providerError.UserMessage : "AI hiện không thể phản hồi. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(Index), new { conversationId = conversation.AiconversationId });
    }
}
