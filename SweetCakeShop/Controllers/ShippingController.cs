using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services.Shipping;

namespace SweetCakeShop.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private readonly IGhnShippingService _ghnService;
        private readonly ApplicationDbContext _context;

        public ShippingController(IGhnShippingService ghnService, ApplicationDbContext context)
        {
            _ghnService = ghnService;
            _context = context;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var list = await _ghnService.GetProvincesAsync();
            return Ok(list);
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int provinceId)
        {
            var list = await _ghnService.GetDistrictsAsync(provinceId);
            return Ok(list);
        }

        [HttpGet("wards")]
        public async Task<IActionResult> GetWards([FromQuery] int districtId)
        {
            var list = await _ghnService.GetWardsAsync(districtId);
            return Ok(list);
        }

        [HttpGet("calculate-fee")]
        public async Task<IActionResult> CalculateFee([FromQuery] int toDistrictId, [FromQuery] string toWardCode, [FromQuery] string? provinceName = null, [FromQuery] string? districtName = null)
        {
            var fee = await _ghnService.CalculateFeeAsync(toDistrictId, toWardCode, 100000, provinceName, districtName);
            return Ok(new { fee });
        }

        // 1. API tạo đơn trực tiếp với cấu trúc DTO gửi lên từ FE
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] GhnCreateOrderRequestDto request)
        {
            var response = await _ghnService.CreateOrderAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // 2. API tiện ích: Tự động lấy thông tin từ Order trong DB theo orderId để tạo đơn GHN
        [HttpPost("create-order-by-id/{orderId}")]
        public async Task<IActionResult> CreateOrderById(int orderId, [FromQuery] int? toDistrictId = null, [FromQuery] string? toWardCode = null)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng trong hệ thống" });

            int districtId = toDistrictId ?? 0;
            string wardCode = toWardCode ?? string.Empty;

            // Nếu người dùng không truyền districtId hay wardCode, thử tra cứu tự động theo Tên Tỉnh/Quận/Phường
            if (districtId == 0 || string.IsNullOrEmpty(wardCode))
            {
                var provinces = await _ghnService.GetProvincesAsync();
                var prov = provinces.FirstOrDefault(p => order.Province.Contains(p.Name, StringComparison.OrdinalIgnoreCase) || p.Name.Contains(order.Province, StringComparison.OrdinalIgnoreCase));
                if (prov != null)
                {
                    var districts = await _ghnService.GetDistrictsAsync(prov.Id);
                    var dist = districts.FirstOrDefault(d => order.District.Contains(d.Name, StringComparison.OrdinalIgnoreCase) || d.Name.Contains(order.District, StringComparison.OrdinalIgnoreCase));
                    if (dist != null)
                    {
                        if (districtId == 0) districtId = dist.Id;
                        if (string.IsNullOrEmpty(wardCode))
                        {
                            var wards = await _ghnService.GetWardsAsync(dist.Id);
                            var w = wards.FirstOrDefault(wItem => order.Ward.Contains(wItem.Name, StringComparison.OrdinalIgnoreCase) || wItem.Name.Contains(order.Ward, StringComparison.OrdinalIgnoreCase));
                            if (w != null) wardCode = w.Code;
                        }
                    }
                }
            }

            if (districtId == 0 || string.IsNullOrEmpty(wardCode))
            {
                return BadRequest(new { success = false, message = "Không xác định được Mã Quận (DistrictId) hoặc Mã Phường (WardCode) chuẩn GHN từ địa chỉ đơn hàng. Vui lòng truyền tham số toDistrictId và toWardCode." });
            }

            var validPhone = !string.IsNullOrEmpty(order.CustomerPhone) && order.CustomerPhone.Trim().Length >= 9
                ? order.CustomerPhone.Trim()
                : "0909123456";

            var ghnRequest = new GhnCreateOrderRequestDto
            {
                payment_type_id = 1, // Shop trả phí ship
                note = "Xin nhẹ tay, bánh kem dễ vỡ",
                required_note = "CHOXEMHANGKHONGTHU",
                client_order_code = order.OrderId.ToString(),
                to_name = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : "Khách hàng SweetCake",
                to_phone = validPhone,
                to_address = !string.IsNullOrEmpty(order.StreetAddress) ? $"{order.StreetAddress}, {order.Ward}, {order.District}, {order.Province}" : order.ShippingAddress,
                to_district_id = districtId,
                to_ward_code = wardCode,
                cod_amount = order.Status == "Pending" || order.Status == "Confirmed" ? (int)order.TotalPrice : 0, // Nếu đã thanh toán online (Paid) thì COD = 0
                content = $"Đơn hàng Sweet Cake Shop #{order.OrderId}",
                weight = 500,
                length = 20,
                width = 20,
                height = 15,
                insurance_value = (int)Math.Min(order.TotalPrice, 5000000),
                service_type_id = 2,
                items = order.OrderDetails.Select(od => new GhnOrderItemDto
                {
                    name = od.Product?.ProductName ?? $"Sản phẩm #{od.ProductId}",
                    code = od.ProductId.ToString(),
                    quantity = od.Quantity,
                    price = (int)od.Price,
                    weight = 500
                }).ToList()
            };

            var response = await _ghnService.CreateOrderAsync(ghnRequest);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
