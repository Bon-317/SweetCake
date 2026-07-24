using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services.Shipping
{
    public class GhnShippingService : IGhnShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly string Token;
        private readonly string ShopId;
        private const string BaseUrl = "https://dev-online-gateway.ghn.vn/shiip/public-api";

        public GhnShippingService(HttpClient httpClient, IServiceProvider serviceProvider, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _httpClient = httpClient;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            Token = _configuration["GHN:Token"] ?? "";
            ShopId = _configuration["GHN:ShopId"] ?? "";
            
            if (!string.IsNullOrEmpty(Token))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", Token);
            }
        }

        public async Task<List<GhnLocationDto>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/master-data/province");
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                    {
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<GhnLocationDto>();
                            foreach (var item in data.EnumerateArray())
                            {
                                int id = item.TryGetProperty("ProvinceID", out var idProp) && idProp.ValueKind != JsonValueKind.Null ? idProp.GetInt32() : 0;
                                string name = item.TryGetProperty("ProvinceName", out var nameProp) && nameProp.ValueKind != JsonValueKind.Null ? nameProp.GetString() ?? "" : "";
                                string codeStr = item.TryGetProperty("Code", out var codeProp) && codeProp.ValueKind != JsonValueKind.Null ? codeProp.GetString() ?? id.ToString() : id.ToString();

                                list.Add(new GhnLocationDto
                                {
                                    Id = id,
                                    Name = name,
                                    Code = codeStr
                                });
                            }
                            return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GHN API Error] GetProvinces: {ex.Message}");
            }
            return new List<GhnLocationDto>();
        }

        public async Task<List<GhnLocationDto>> GetDistrictsAsync(int provinceId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/master-data/district", new { province_id = provinceId });
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                    {
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<GhnLocationDto>();
                            foreach (var item in data.EnumerateArray())
                            {
                                int id = item.TryGetProperty("DistrictID", out var idProp) && idProp.ValueKind != JsonValueKind.Null ? idProp.GetInt32() : 0;
                                string name = item.TryGetProperty("DistrictName", out var nameProp) && nameProp.ValueKind != JsonValueKind.Null ? nameProp.GetString() ?? "" : "";
                                string codeStr = item.TryGetProperty("Code", out var codeProp) && codeProp.ValueKind != JsonValueKind.Null ? codeProp.GetString() ?? id.ToString() : id.ToString();

                                list.Add(new GhnLocationDto
                                {
                                    Id = id,
                                    Name = name,
                                    Code = codeStr
                                });
                            }
                            return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GHN API Error] GetDistricts: {ex.Message}");
            }
            return new List<GhnLocationDto>();
        }

        public async Task<List<GhnLocationDto>> GetWardsAsync(int districtId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/master-data/ward", new { district_id = districtId });
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                    {
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<GhnLocationDto>();
                            foreach (var item in data.EnumerateArray())
                            {
                                string wardCode = item.TryGetProperty("WardCode", out var codeProp) && codeProp.ValueKind != JsonValueKind.Null ? codeProp.GetString() ?? "" : "";
                                string wardName = item.TryGetProperty("WardName", out var nameProp) && nameProp.ValueKind != JsonValueKind.Null ? nameProp.GetString() ?? "" : "";

                                list.Add(new GhnLocationDto
                                {
                                    Id = 0,
                                    Code = wardCode,
                                    Name = wardName
                                });
                            }
                            return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GHN API Error] GetWards: {ex.Message}");
            }
            return new List<GhnLocationDto>();
        }

        public async Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode, decimal insuranceValue = 100000, string? provinceName = null, string? districtName = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/shipping-order/fee");
                request.Headers.Add("ShopId", ShopId);
                request.Content = JsonContent.Create(new
                {
                    from_district_id = 1459, // Hóc Môn
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    height = 15,
                    length = 20,
                    weight = 500,
                    width = 20,
                    insurance_value = (int)Math.Min(insuranceValue, 5000000),
                    service_type_id = 2
                });

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                {
                    if (root.TryGetProperty("data", out var data) && data.TryGetProperty("total", out var total))
                    {
                        return total.GetDecimal();
                    }
                }
                else if (root.TryGetProperty("message", out var msg))
                {
                    Console.WriteLine($"[GHN API Note] {msg.GetString()} -> Dùng tính phí ship dự phòng theo khu vực.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GHN API Error] CalculateFee: {ex.Message}");
            }

            // Fallback khi GHN sandbox hoặc dùng provinces.open-api.vn
            var pName = (provinceName ?? "").ToLower();
            var dName = (districtName ?? "").ToLower();

            if (pName.Contains("hồ chí minh") || pName.Contains("sài gòn") || toDistrictId == 3695 || (toDistrictId >= 1440 && toDistrictId <= 1550))
            {
                if (dName.Contains("thủ đức") || dName.Contains("quận 1") || dName.Contains("quận 3") || dName.Contains("bình thạnh") || toDistrictId == 3695 || toDistrictId == 1463)
                    return 25000m; // Nội thành TP.HCM
                return 30000m; // Ngoại thành TP.HCM
            }
            if (pName.Contains("bình dương") || pName.Contains("đồng nai") || pName.Contains("long an"))
                return 35000m; // Lân cận TP.HCM

            return 45000m; // Các tỉnh thành khác
        }

        public async Task<GhnCreateOrderResponseDto> CreateOrderAsync(GhnCreateOrderRequestDto request)
        {
            var result = new GhnCreateOrderResponseDto();
            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/shipping-order/create");
                httpRequest.Headers.Add("ShopId", ShopId);
                httpRequest.Content = JsonContent.Create(request);

                var response = await _httpClient.SendAsync(httpRequest);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() == 200)
                {
                    result.Success = true;
                    if (root.TryGetProperty("message_display", out var msgDisp))
                        result.Message = msgDisp.GetString() ?? "Tạo đơn hàng thành công";
                    else if (root.TryGetProperty("message", out var msg))
                        result.Message = msg.GetString() ?? "Success";

                    if (root.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("order_code", out var code))
                            result.OrderCode = code.GetString() ?? "";
                        if (data.TryGetProperty("total_fee", out var fee) && fee.ValueKind != JsonValueKind.Null)
                            result.TotalFee = fee.GetDecimal();
                        if (data.TryGetProperty("expected_delivery_time", out var time) && time.ValueKind != JsonValueKind.Null)
                            result.ExpectedDeliveryTime = time.GetString() ?? "";
                    }
                }
                else
                {
                    result.Success = false;
                    if (root.TryGetProperty("message", out var msg))
                        result.Message = msg.GetString() ?? "Lỗi tạo đơn hàng GHN";
                    else if (root.TryGetProperty("code_message", out var codeMsg))
                        result.Message = codeMsg.GetString() ?? "Error";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Exception: {ex.Message}";
            }
            return result;
        }

        public async Task<string> CreateAutomatedOrderAsync(int orderId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return "(GHN: Không tìm thấy đơn hàng trong CSDL)";

            // Chống tạo trùng lặp: Nếu đơn hàng đã có GhnOrderCode trước đó -> Không tạo lại
            if (!string.IsNullOrEmpty(order.GhnOrderCode))
            {
                return $"[ GHN: Đơn hàng đã được tạo trên GHN trước đó (Mã GHN: {order.GhnOrderCode}) ]";
            }

            int districtId = 0;
            string wardCode = string.Empty;

            var provinces = await GetProvincesAsync();
            var prov = provinces.FirstOrDefault(p => order.Province.Contains(p.Name, StringComparison.OrdinalIgnoreCase) || p.Name.Contains(order.Province, StringComparison.OrdinalIgnoreCase));
            if (prov != null)
            {
                var districts = await GetDistrictsAsync(prov.Id);
                var dist = districts.FirstOrDefault(d => order.District.Contains(d.Name, StringComparison.OrdinalIgnoreCase) || d.Name.Contains(order.District, StringComparison.OrdinalIgnoreCase));
                if (dist != null)
                {
                    districtId = dist.Id;
                    var wards = await GetWardsAsync(dist.Id);
                    var w = wards.FirstOrDefault(wItem => order.Ward.Contains(wItem.Name, StringComparison.OrdinalIgnoreCase) || wItem.Name.Contains(order.Ward, StringComparison.OrdinalIgnoreCase));
                    if (w != null) wardCode = w.Code;
                }
            }

            if (districtId == 0 || string.IsNullOrEmpty(wardCode))
            {
                return "(GHN: Chưa tra được mã Quận/Phường từ địa chỉ đơn hàng. Vui lòng kiểm tra lại địa chỉ Tỉnh/Quận/Phường chuẩn.)";
            }

            var validPhone = !string.IsNullOrEmpty(order.CustomerPhone) && order.CustomerPhone.Trim().Length >= 9 && order.CustomerPhone.Trim().All(char.IsDigit)
                ? order.CustomerPhone.Trim()
                : "0909123456"; // Tự động điền SĐT hợp lệ nếu số trên đơn cũ ngắn/sai định dạng

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
                cod_amount = (order.Status == "Pending" || order.Status == "Confirmed" || order.Status == "AwaitingConfirmation") ? (int)order.TotalPrice : 0,
                content = $"Đơn hàng Sweet Cake Shop #{order.OrderId}",
                weight = 500,
                length = 20,
                width = 20,
                height = 15,
                insurance_value = (int)Math.Min(order.TotalPrice, 5000000),
                service_type_id = 2
            };

            foreach (var item in order.OrderDetails)
            {
                ghnRequest.items.Add(new GhnOrderItemDto
                {
                    name = item.Product?.ProductName ?? $"Bánh kem #{item.ProductId}",
                    code = item.ProductId.ToString(),
                    quantity = item.Quantity > 0 ? item.Quantity : 1,
                    price = (int)item.Price,
                    weight = 500
                });
            }

            if (ghnRequest.items.Count == 0)
            {
                ghnRequest.items.Add(new GhnOrderItemDto
                {
                    name = "Bánh kem SweetCake",
                    code = "SWEETCAKE",
                    quantity = 1,
                    price = (int)order.TotalPrice,
                    weight = 500
                });
            }

            var result = await CreateOrderAsync(ghnRequest);
            if (result.Success)
            {
                // Lưu lại mã vận đơn GHN vào CSDL để lần sau không bị tạo trùng
                order.GhnOrderCode = result.OrderCode;
                await context.SaveChangesAsync();
                return $"[ GHN: Tạo vận đơn thành công! Mã GHN: {result.OrderCode} - Phí: {result.TotalFee:N0}đ ]";
            }
            else
            {
                return $"[ GHN Lỗi: {result.Message} ]";
            }
        }
    }
}
