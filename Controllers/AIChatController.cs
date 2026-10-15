using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers;

[Authorize(Roles = "CUSTOMER")]
public class AIChatController(ApplicationDbContext context, IAIChatService aiChatService,
    IConfiguration configuration, ILogger<AIChatController> logger) : Controller
{
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
        return new AIChatViewModel
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
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NewConversation() => RedirectToAction(nameof(Index), new { newChat = true });

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
        if (string.IsNullOrWhiteSpace(model.Message) && ModelState.IsValid)
            ModelState.AddModelError(nameof(model.Message), "Vui lòng nhập câu hỏi.");
        if (!ModelState.IsValid)
        {
            var viewModel = await BuildViewModelAsync(customer.CustomerId, conversation);
            viewModel.Message = model.Message;
            return View("Index", viewModel);
        }
        var message = model.Message.Trim();
        var history = conversation == null ? new List<Aimessage>() : await context.Aimessages.AsNoTracking()
            .Where(m => m.AiconversationId == conversation.AiconversationId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.AimessageId).Take(10).ToListAsync();
        history.Reverse();
        conversation ??= new Aiconversation
        {
            CustomerId = customer.CustomerId,
            Title = message.Length <= 40 ? message : message[..40] + "...",
            CreatedAt = DateTime.Now
        };
        if (conversation.AiconversationId == 0) context.Aiconversations.Add(conversation);
        // Save the new conversation and first question together. Keep the user's
        // question even if the provider is unavailable, matching chat history semantics.
        conversation.Aimessages.Add(new Aimessage { Role = "USER", Content = message, CreatedAt = DateTime.Now });
        conversation.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        var workoutPlan = await context.WorkoutPlans.AsNoTracking().Include(p => p.WorkoutExercises)
            .ThenInclude(w => w.Exercise).Where(p => p.CustomerId == customer.CustomerId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.WorkoutPlanId).FirstOrDefaultAsync();
        try
        {
            var response = await aiChatService.GetResponseAsync(customer, message, history, workoutPlan);
            conversation.Aimessages.Add(new Aimessage { Role = "ASSISTANT", Content = response, CreatedAt = DateTime.Now });
            conversation.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("AI chat failed for conversation {ConversationId} ({ErrorType})",
                conversation.AiconversationId, ex.GetType().Name);
            TempData["ErrorMessage"] = "AI hiện không thể phản hồi. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(Index), new { conversationId = conversation.AiconversationId });
    }
}
