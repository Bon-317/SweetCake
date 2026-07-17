using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SweetCakeShop.Services.Shipping
{
    public class GhnShippingService : IGhnShippingService
    {
        private readonly HttpClient _httpClient;
        private const string Token = "7d0a6149-81a0-11f1-a973-aee5264794df";
        private const string ShopId = "201573"; // Shop ID liên kết với Token trên Sandbox
        private const string BaseUrl = "https://dev-online-gateway.ghn.vn/shiip/public-api";

        public GhnShippingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("Token", Token);
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
                        var data = root.GetProperty("data");
                        var list = new List<GhnLocationDto>();
                        foreach (var item in data.EnumerateArray())
                        {
                            list.Add(new GhnLocationDto
                            {
                                Id = item.GetProperty("ProvinceID").GetInt32(),
                                Name = item.GetProperty("ProvinceName").GetString() ?? "",
                                Code = item.GetProperty("Code").GetString() ?? item.GetProperty("ProvinceID").GetInt32().ToString()
                            });
                        }
                        return list;
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
                        var data = root.GetProperty("data");
                        var list = new List<GhnLocationDto>();
                        foreach (var item in data.EnumerateArray())
                        {
                            list.Add(new GhnLocationDto
                            {
                                Id = item.GetProperty("DistrictID").GetInt32(),
                                Name = item.GetProperty("DistrictName").GetString() ?? "",
                                Code = item.GetProperty("Code").GetString() ?? item.GetProperty("DistrictID").GetInt32().ToString()
                            });
                        }
                        return list;
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
                        var data = root.GetProperty("data");
                        var list = new List<GhnLocationDto>();
                        foreach (var item in data.EnumerateArray())
                        {
                            list.Add(new GhnLocationDto
                            {
                                Id = 0,
                                Code = item.GetProperty("WardCode").GetString() ?? "",
                                Name = item.GetProperty("WardName").GetString() ?? ""
                            });
                        }
                        return list;
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
                    from_district_id = 3695, // TP. Thủ Đức (hoặc Quận/Huyện của shop)
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
    }
}
