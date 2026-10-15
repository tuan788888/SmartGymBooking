using System.ComponentModel.DataAnnotations;
using SmartGymBooking.Models;

namespace SmartGymBooking.ViewModels
{
    public class AIChatViewModel
    {
        public List<Aiconversation> Conversations { get; set; } = new();
        public string AIProvider { get; set; } = "DEMO";
        public long? ConversationId { get; set; }

        public string ConversationTitle { get; set; }
            = "AI Virtual PT";


        [Required(
            ErrorMessage =
                "Vui lòng nhập câu hỏi."
        )]

        [StringLength(2000)]
        public string Message { get; set; }
            = string.Empty;


        public List<Aimessage> Messages
        {
            get;
            set;
        } = new();
    }
}
