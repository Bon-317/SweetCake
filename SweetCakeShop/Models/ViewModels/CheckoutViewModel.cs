namespace SweetCakeShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        // Optional: you can add shipping method, notes, payment method etc.
    }
}
