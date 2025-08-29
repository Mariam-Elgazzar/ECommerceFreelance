using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.OrderDTOs;
using ECommerce.BL.Specification.OrderSpecification;

namespace ECommerce.BL.Services
{
    public interface IOrderService
    {
        /// <summary>
        /// Retrieves a paginated list of orders based on provided parameters.
        /// </summary>
        /// <param name="param">Parameters for filtering and pagination, including search, user ID, and status.</param>
        /// <returns>A PaginationResponse containing the list of OrderDTOs, page size, page index, and total count.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during order retrieval.</exception>
        Task<PaginationResponse<OrderDTO>> GetAllOrdersAsync(OrderParams param);
        /// <summary>
        /// Retrieves an order by its ID.
        /// </summary>
        /// <param name="orderId">The ID of the order to retrieve.</param>
        /// <returns>An OrderDTO containing order details and items if found; otherwise, null.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during order retrieval.</exception>
        Task<OrderDTO> GetOrderByIdAsync(int orderId);
        /// <summary>
        /// Creates a new order for a user based on the provided order details.
        /// </summary>
        /// <param name="dto">The data transfer object containing user ID and order items.</param>
        /// <returns>An OrderDTO containing the created order's details.</returns>
        /// <exception cref="Exception">Thrown when the user or product is not found, no order items are provided, or an error occurs during order creation.</exception>
        Task<ResultDTO> CreateOrderAsync(CreateOrderDTO dto);
        /// <summary>
        /// Updates an existing order's status and/or order items based on the provided DTO.
        /// </summary>
        /// <param name="orderId">The ID of the order to update.</param>
        /// <param name="dto">The data transfer object containing the updated status and order items.</param>
        /// <returns>An OrderDTO containing the updated order details.</returns>
        /// <exception cref="Exception">Thrown when the order, order item, or product is not found, or an error occurs during the update operation.</exception>
        Task<ResultDTO> UpdateOrderAsync(UpdateOrderDTO dto);
        /// <summary>
        /// deletes an order and its associated order items by the specified order ID.
        /// </summary>
        /// <param name="orderId">The ID of the order to be deleted.</param>
        /// <returns>A Task representing the asynchronous deletion operation.</returns>
        /// <exception cref="Exception">Thrown when the order is not found or an error occurs during the deletion process.</exception>
        Task DeleteOrderAsync(int orderId);
        /// <summary>
        /// Confirms a checkout by creating an order, sending a WhatsApp notification, and sending an email notification.
        /// </summary>
        /// <param name="dto">The data transfer object containing the user ID and order items for the checkout.</param>
        /// <returns>A ResultDTO indicating the success or failure of the checkout operation, including any error messages.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the WhatsApp notification process.</exception>
        Task<ResultDTO> ConfirmCheckout(CreateOrderDTO dto);
    }
}