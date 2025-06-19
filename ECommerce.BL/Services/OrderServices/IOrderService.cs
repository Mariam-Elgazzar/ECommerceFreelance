using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.OrderDTOs;

namespace ECommerce.BL.Services
{
    public interface IOrderService
    {
        Task<ResultDTO> ConfirmCheckout(OrderDTO dto);
    }
}