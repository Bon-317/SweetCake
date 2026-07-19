using System.Threading.Tasks;
using System.Collections.Generic;

namespace SweetCakeShop.Services.Shipping
{
    public class GhnLocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class GhnOrderItemDto
    {
        public string name { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
        public int quantity { get; set; } = 1;
        public int price { get; set; }
        public int weight { get; set; } = 500;
    }

    public class GhnCreateOrderRequestDto
    {
        public int payment_type_id { get; set; } = 1; // 1: Shop trả phí, 2: Người nhận trả
        public string note { get; set; } = "Xin nhẹ tay, bánh kem dễ vỡ";
        public string required_note { get; set; } = "CHOXEMHANGKHONGTHU";
        public string client_order_code { get; set; } = string.Empty;
        public string to_name { get; set; } = string.Empty;
        public string to_phone { get; set; } = string.Empty;
        public string to_address { get; set; } = string.Empty;
        public string to_ward_code { get; set; } = string.Empty;
        public int to_district_id { get; set; }
        public int cod_amount { get; set; }
        public string content { get; set; } = string.Empty;
        public int weight { get; set; } = 500;
        public int length { get; set; } = 20;
        public int width { get; set; } = 20;
        public int height { get; set; } = 15;
        public int insurance_value { get; set; }
        public int service_type_id { get; set; } = 2;
        public List<GhnOrderItemDto> items { get; set; } = new List<GhnOrderItemDto>();
    }

    public class GhnCreateOrderResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public decimal TotalFee { get; set; }
        public string ExpectedDeliveryTime { get; set; } = string.Empty;
    }

    public interface IGhnShippingService
    {
        Task<List<GhnLocationDto>> GetProvincesAsync();
        Task<List<GhnLocationDto>> GetDistrictsAsync(int provinceId);
        Task<List<GhnLocationDto>> GetWardsAsync(int districtId);
        Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode, decimal insuranceValue = 100000, string? provinceName = null, string? districtName = null);
        Task<GhnCreateOrderResponseDto> CreateOrderAsync(GhnCreateOrderRequestDto request);
        Task<string> CreateAutomatedOrderAsync(int orderId);
    }
}
