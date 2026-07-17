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

    public interface IGhnShippingService
    {
        Task<List<GhnLocationDto>> GetProvincesAsync();
        Task<List<GhnLocationDto>> GetDistrictsAsync(int provinceId);
        Task<List<GhnLocationDto>> GetWardsAsync(int districtId);
        Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode, decimal insuranceValue = 100000, string? provinceName = null, string? districtName = null);
    }
}
