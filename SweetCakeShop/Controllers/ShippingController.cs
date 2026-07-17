using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Services.Shipping;

namespace SweetCakeShop.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private readonly IGhnShippingService _ghnService;

        public ShippingController(IGhnShippingService ghnService)
        {
            _ghnService = ghnService;
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
    }
}
