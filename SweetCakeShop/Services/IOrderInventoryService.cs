using System.Threading.Tasks;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IOrderInventoryService
    {
        Task<(bool Success, string Message)> DeductInventoryForOrderAsync(int orderId);
    }
}
