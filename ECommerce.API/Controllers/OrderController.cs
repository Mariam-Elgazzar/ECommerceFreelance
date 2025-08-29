using ECommerce.BL.DTO.OrderDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Specification.OrderSpecification;
using ECommerce.BL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IUnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Get Orders
        /// <summary>
        /// Retrieves a list of orders, optionally filtered by search term, user ID, or status, restricted to Admin users.
        /// </summary>
        /// <param name="param">Optional parameters for filtering and pagination, including Search, UserId, and Status.</param>
        /// <returns>
        /// Returns a <see cref="PaginationResponse{OrderDTO}"/> containing a list of orders if found, or a message indicating no orders were found.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves a list of orders, supporting optional filtering by search term, user ID, or order status, and pagination. The request is restricted to users with the Admin role.
        ///
        /// Assumed validation rules for query parameters (based on OrderParams):
        /// - Search: Optional, must be a valid string if provided, e.g., "order123".
        /// - UserId: Optional, must be a valid string if provided, e.g., "user123".
        /// - Status: Optional, must be a valid order status if provided, e.g., "Pending".
        /// - PageIndex: Optional, must be a positive integer (≥ 1) if provided, defaults to 1.
        /// - PageSize: Optional, must be a positive integer (≥ 1) if provided, defaults to a system-defined value (e.g., 10).
        ///
        /// Example Request:
        /// ```
        /// GET ~/Orders/GetAllOrders?Search=order123&UserId=user123&Status=Pending&PageIndex=1&PageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a paginated list of orders when orders are found.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "pageSize": 10,
        ///   "pageIndex": 1,
        ///   "totalCount": 2,
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "userId": "user123",
        ///       "status": "Pending",
        ///       "totalPrice": 59.98,
        ///       "items": [
        ///         {
        ///           "productId": 1,
        ///           "quantity": 2,
        ///           "name": "Product Name",
        ///           "price": 29.99
        ///         }
        ///       ]
        ///     },
        ///     {
        ///       "id": 2,
        ///       "userId": "user123",
        ///       "status": "Completed",
        ///       "totalPrice": 19.99,
        ///       "items": [
        ///         {
        ///           "productId": 2,
        ///           "quantity": 1,
        ///           "name": "Another Product",
        ///           "price": 19.99
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="204">
        /// Returned when no orders are found.
        /// No Content Response (204):
        /// ```json
        /// {
        ///   "Message": "No orders found."
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized or lacks the Admin role.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "Message": "Unauthorized access"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "Message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Route("~/Orders/GetAllOrders")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderParams param = null)
        {
            try
            {
                _logger.LogInformation("Retrieving orders with parameters: Search={Search}, Status={Status}", param?.Search, param?.OrderStatus);
                var response = await _unitOfWork.OrderServices.GetAllOrdersAsync(param);

                if (response == null || !response.Data.Any())
                {
                    _logger.LogWarning("No orders found");
                    return StatusCode(204, new { Message = "No orders found." });
                }

                _logger.LogInformation("Retrieved {OrderCount} orders", response.Data.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion


        #region Get Order by UserId

        /// <summary>
        /// Retrieves an order by its ID, restricted to User role.
        /// </summary>
        /// <param name="orderId">The ID of the order to retrieve.</param>
        /// <returns>
        /// Returns an <see cref="OrderDTO"/> containing the order details if found, or an error message if the order is not found or the request fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves a single order by its ID. The request is restricted to users with the User role. Note: The route in the code (`~/Orders/GetOrdersByUserId/{userId}`) suggests retrieval by user ID, but the method logic uses `orderId`. This summary assumes the route should be `~/Orders/GetOrderById/{orderId}` to align with the logic.
        ///
        /// Validation rules for parameters:
        /// - OrderId: Required, must be a positive integer (≥ 1).
        ///
        /// Example Request:
        /// ```
        /// GET ~/Orders/GetOrderById/1
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the order details when the order is found.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "id": 1,
        ///   "userId": "user123",
        ///   "status": "Pending",
        ///   "totalPrice": 59.98,
        ///   "items": [
        ///     {
        ///       "productId": 1,
        ///       "quantity": 2,
        ///       "name": "Product Name",
        ///       "price": 29.99
        ///     }
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the order with the specified ID is not found.
        /// Not Found Response (404):
        /// ```json
        /// {
        ///   "Message": "Order not found."
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized or lacks the User role.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "Message": "Unauthorized access"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "Message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Route("~/Orders/GetOrdersByOrderId/")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                _logger.LogInformation("Retrieving order: {OrderId}", orderId);
                var order = await _unitOfWork.OrderServices.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { Message = "Order not found." });
                }

                _logger.LogInformation("Successfully retrieved order: {OrderId}", orderId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion


        //#region Create Order
        ///// <summary>
        ///// Creates a new order for a specified user, restricted to User and Admin roles.
        ///// </summary>
        ///// <param name="dto">The data transfer object containing order details.</param>
        ///// <returns>
        ///// Returns the created <see cref="OrderDTO"/> with a location header pointing to the new order if successful, or an error message if the operation fails.
        ///// </returns>
        ///// <remarks>
        ///// This endpoint creates a new order based on the provided details. The request is restricted to users with User or Admin roles.
        ///// The request body must contain valid order information, including the user ID and order items.
        /////
        ///// Assumed validation rules for request body (based on CreateOrderDTO):
        ///// - UserId: Required, must be a non-empty string, e.g., "user123".
        ///// - Items: Required, must be a non-empty list of order items, each with a valid ProductId (≥ 1) and Quantity (≥ 1).
        /////
        ///// Example Request:
        ///// ```json
        ///// {
        /////   "userId": "user123",
        /////   "items": [
        /////     {
        /////       "productId": 1,
        /////       "quantity": 2
        /////     }
        /////   ]
        ///// }
        ///// ```
        ///// </remarks>
        ///// <response code="201">
        ///// Returns the created order details with a location header pointing to the order retrieval endpoint.
        ///// Successful Response (201 Created):
        ///// ```json
        ///// {
        /////   "orderID": 1,
        /////   "userId": "user123",
        /////   "status": "Pending",
        /////   "totalPrice": 59.98,
        /////   "items": [
        /////     {
        /////       "productId": 1,
        /////       "quantity": 2,
        /////       "name": "Product Name",
        /////       "price": 29.99
        /////     }
        /////   ]
        ///// }
        ///// ```
        ///// </response>
        ///// <response code="401">
        ///// Returned when the user is not authorized or lacks the User or Admin role.
        ///// Unauthorized Response (401):
        ///// ```json
        ///// {
        /////   "Message": "Unauthorized access"
        ///// }
        ///// ```
        ///// </response>
        ///// <response code="500">
        ///// Returned when an unexpected server error occurs during processing.
        ///// Server Error Response (500):
        ///// ```json
        ///// {
        /////   "Message": "An error occurred while processing your request"
        ///// }
        ///// ```
        ///// </response>
        //[HttpPost]
        //[Route("~/Orders/CreateOrder")]
        ////[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        //public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO dto)
        //{
        //    try
        //    {
        //        var order = await _unitOfWork.OrderServices.CreateOrderAsync(dto);
        //        return Created("", order);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Message = ex.Message });
        //    }
        //}

        //#endregion


        #region Update Order
        /// <summary>
        /// Updates an existing order by its ID, restricted to User and Admin roles.
        /// </summary>
        /// <param name="orderId">The ID of the order to update.</param>
        /// <param name="dto">The data transfer object containing updated order details.</param>
        /// <returns>
        /// Returns the updated <see cref="OrderDTO"/> if successful, or an error message if the order is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint updates an order with the specified ID using the provided details. The request is restricted to users with User or Admin roles.
        /// The request body must contain valid order information, such as status or item updates, and may need to include the user ID for validation.
        ///
        /// Assumed validation rules for request body (based on UpdateOrderDTO):
        /// - UserId: Optional, must be a non-empty string if provided, e.g., "user123".
        /// - Status: Optional, must be a valid order status if provided, e.g., "Pending", "Completed".
        /// - Items: Optional, must be a list of order items if provided, each with a valid ProductId (≥ 1) and Quantity (≥ 1).
        ///
        /// Example Request:
        /// ```json
        /// {
        ///   "userId": "user123",
        ///   "status": "Completed",
        ///   "items": [
        ///     {
        ///       "productId": 1,
        ///       "quantity": 3
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the updated order details when the operation is successful.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "orderID": 1,
        ///   "userId": "user123",
        ///   "status": "Completed",
        ///   "totalPrice": 89.97,
        ///   "items": [
        ///     {
        ///       "productId": 1,
        ///       "quantity": 3,
        ///       "name": "Product Name",
        ///       "price": 29.99
        ///     }
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the order with the specified ID is not found.
        /// Not Found Response (404):
        /// ```json
        /// {
        ///   "Message": "Order not found."
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized or lacks the User or Admin role.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "Message": "Unauthorized access"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "Message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpPut]
        [Route("~/Orders/UpdateOrder/{orderId}")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderDTO dto)
        {
            try
            {
                if(!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for UpdateOrder: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ModelState);
                }
                _logger.LogInformation("Updating order: {OrderId}", dto.Id);
                var order = await _unitOfWork.OrderServices.UpdateOrderAsync(dto);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", dto.Id);
                    return NotFound(new { Message = "Order not found." });
                }

                _logger.LogInformation("Successfully updated order: {OrderId}", dto.Id);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", dto.Id);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion


        #region Delete Order
        /// <summary>
        /// Deletes an order by its ID, restricted to User and Admin roles.
        /// </summary>
        /// <param name="orderId">The ID of the order to delete.</param>
        /// <returns>
        /// Returns a 204 No Content response if the order is deleted successfully, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint deletes an order with the specified ID. The request is restricted to users with User or Admin roles.
        ///
        /// Validation rules for parameters:
        /// - OrderId: Required, must be a positive integer (≥ 1).
        ///
        /// Example Request:
        /// ```
        /// DELETE ~/Orders/DeleteOrder/1
        /// ```
        /// </remarks>
        /// <response code="204">
        /// Indicates the order was deleted successfully. No content is returned.
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authorized or lacks the User or Admin role.
        /// Unauthorized Response (401):
        /// ```json
        /// {
        ///   "Message": "Unauthorized access"
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "Message": "An error occurred while processing your request"
        /// }
        /// ```
        /// </response>
        [HttpDelete]
        [Route("~/Orders/DeleteOrder/{orderId}")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    _logger.LogWarning("Invalid order ID: {OrderId}", orderId);
                    return BadRequest(new { Message = "Invalid order ID." });
                }
                _logger.LogInformation("Deleting order: {OrderId}", orderId);
                await _unitOfWork.OrderServices.DeleteOrderAsync(orderId);
                _logger.LogInformation("Successfully deleted order: {OrderId}", orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {OrderId}", orderId);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion


        #region Checkout 

        /// <summary>
        /// Initiates the checkout process for a user's cart.
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A response indicating the result of the checkout process.</returns>
        /// <response code="200">Checkout initiated successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="500">Server error.</response>
        [HttpPost]
        [Route("~/Orders/Checkout")]
        //[Authorize(Roles = $"{Roles.User}, {Roles.Admin}")]
        public async Task<IActionResult> Checkout(CreateOrderDTO dto)
        {
            try
            {
                if(!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for Checkout: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ModelState);
                }
                var response = await _unitOfWork.OrderServices.ConfirmCheckout(dto);
                if (response == null)
                {
                    return BadRequest(new { Message = "Checkout failed. Please try again." });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion
    }
}