namespace SmartGymBooking.ViewModels
{
    public class PaymentCheckoutViewModel
    {
        public long PaymentId { get; set; }

        public long PackageId { get; set; }

        public string PackageName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string PaymentCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int DurationDays { get; set; }

        public bool IncludesPT { get; set; }

        public string QrImageUrl { get; set; }
            = "/images/payment/bank-qr.png";
    }
}
