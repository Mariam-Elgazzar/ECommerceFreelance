using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.OrderDTOs;
using ECommerce.BL.Settings;
using ECommerce.BL.Specification.OrderSpecification;
using ECommerce.BL.Specification.ProductSpecification;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Extend;
using ECommerce.DAL.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ECommerce.BL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderService> _logger;
        private readonly OrderSettings _orderSettings;
        private readonly TwilioSettings _twilioSettings;
        public OrderService(
            IUnitOfWork unitOfWork, 
            ILogger<OrderService> logger, 
            IOptions<OrderSettings> orderSettings, 
            IOptions<TwilioSettings> twilioSettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _orderSettings = orderSettings.Value;
            _twilioSettings = twilioSettings.Value;
        }


        #region Get All Orders
        /// <summary>
        /// Retrieves a paginated list of orders based on provided parameters.
        /// </summary>
        /// <param name="param">Parameters for filtering and pagination, including search, user ID, and status.</param>
        /// <returns>A PaginationResponse containing the list of OrderDTOs, page size, page index, and total count.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during order retrieval.</exception>
        public async Task<PaginationResponse<OrderDTO>> GetAllOrdersAsync(OrderParams param)
        {
            try
            {
                var statusMap = new Dictionary<OrderStatus, string>
                {
                    { OrderStatus.NewOrder, "طلب جديد" },
                    { OrderStatus.Processing, "تحت الاجراء" },
                    { OrderStatus.Shipped, "تم انهاء الطلب" },
                    { OrderStatus.Cancelled, "الغاء الطلب" }
                };
                
                Dictionary<string, OrderStatus> ReverseStatusMap = statusMap.ToDictionary(x => x.Value, x => x.Key);
                
                param.OrderStatus = string.IsNullOrEmpty(param.OrderStatus) ? 
                    null : ReverseStatusMap.
                    TryGetValue(param.OrderStatus, out var status) ? 
                    status.ToString() : null;
                
                var spec = new OrderSpecification(param);
                var orders = await _unitOfWork.Repository<Order>().GetAllBySpecAsync(spec);
                var totalCount = await _unitOfWork.Repository<Order>().CountAsync(spec);

                var data  = new List<OrderDTO>();
                if (orders == null || !orders.Any())
                {
                    _logger.LogInformation("No orders found for the given parameters.");
                    return new PaginationResponse<OrderDTO>
                    {
                        PageSize = param.PageSize,
                        PageIndex = param.PageIndex,
                        TotalCount = 0,
                        Data = data
                    };
                }
                var orderItems = new List<OrderItemDTO>();
               

                data = orders.Select(order => new OrderDTO
                {
                    Id = order.Id,
                    UserName = order.Name,
                    PhoneNumber = order.PhoneNumber,
                    Address = order.Address,
                    Date = order.Date,
                    Status = statusMap[order.OrderStatus], // Map enum to Arabic string
                    Email = order.Email,
                    OrderItems = new List<OrderItemDTO>
                    {
                        new OrderItemDTO
                        {
                            ProductId = order.ProductId,
                            ProductName = order.Product.Name,
                            Brand = order.Product.Brand,
                            Model = order.Product.Modal,
                            ProductStatus = order.ProductStatus,
                            RentalPeriod = order.RentalPeriod,
                        }
                    }
                }).ToList();

                var response = new PaginationResponse<OrderDTO>
                {
                    PageSize = param.PageSize,
                    PageIndex = param.PageIndex,
                    TotalCount = totalCount,
                    Data = data
                };

                _logger.LogInformation("Retrieved {OrderCount} orders", data.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                throw;
            }
        }
        #endregion


        #region Get Order By Id
        /// <summary>
        /// Retrieves an order by its ID.
        /// </summary>
        /// <param name="orderId">The ID of the order to retrieve.</param>
        /// <returns>An OrderDTO containing order details and items if found; otherwise, null.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during order retrieval.</exception>
        public async Task<OrderDTO> GetOrderByIdAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Retrieving order with ID: {OrderId}", orderId);
                var spec = new OrderSpecification(orderId);
                var order = await _unitOfWork.Repository<Order>().GetBySpecAsync(spec);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return null;
                }

                var orderDto = new OrderDTO
                {
                    Id = order.Id,
                    UserName = order.Name,
                    PhoneNumber = order.PhoneNumber,
                    Address = order.Address,
                    Date = order.Date,
                    Status = order.OrderStatus.ToString(),
                    Email = order.Email,
                    OrderItems = new List<OrderItemDTO>()
                    {
                        new OrderItemDTO
                        {
                            ProductId = order.ProductId,
                            ProductName = order.Product.Name,
                            Brand = order.Product.Brand,
                            Model = order.Product.Modal,
                            ProductStatus = order.ProductStatus,
                            RentalPeriod = order.RentalPeriod,
                        }
                    }
                };

                _logger.LogInformation("IsSuccessfully retrieved order: {OrderId}", orderId);
                return orderDto;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
                throw;
            }
        }

        #endregion


        #region Create Order
        /// <summary>
        /// Creates a new order for a user based on the provided order details.
        /// </summary>
        /// <param name="dto">The data transfer object containing user ID and order items.</param>
        /// <returns>An OrderDTO containing the created order's details.</returns>
        /// <exception cref="Exception">Thrown when the user or product is not found, no order items are provided, or an error occurs during order creation.</exception>
        public async Task<ResultDTO> CreateOrderAsync(CreateOrderDTO dto)
        {
            try
            {
                _logger.LogInformation("Creating order for user: {Name}", dto.Name);

                if( dto == null)
                {
                    _logger.LogWarning("CreateOrderDTO is null.");
                    return new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Invalid order data."
                    };
                }
                if (dto.ProductId <= 0)
                {
                    _logger.LogWarning("Invalid ProductId: {ProductId}", dto.ProductId);
                    return new ResultDTO
                    {
                        IsSuccess = false,
                        Message = "Invalid product."
                    };
                }

                var order = new Order
                {
                    ProductId = dto.ProductId,
                    Date = DateTime.Now.AddHours(1),
                    ProductStatus = dto.Status ?? "شراء",
                    Name = dto.Name ?? "غير معروف",
                    Email = dto.Email ?? "غير معروف",
                    PhoneNumber = dto.PhoneNumber ?? "غير معروف",
                    Address = dto.Address ?? "غير معروف",
                    OrderStatus = OrderStatus.NewOrder,
                    RentalPeriod = dto.RentalPeriod,
                };

                await _unitOfWork.Repository<Order>().AddAsync(order);
                await _unitOfWork.Complete();



                return new ResultDTO
                {
                    IsSuccess = true,
                    Message = "Order created successfully."
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion


        #region Update Order
        /// <summary>
        /// Updates an existing order's status and/or order items based on the provided DTO.
        /// </summary>
        /// <param name="orderId">The ID of the order to update.</param>
        /// <param name="dto">The data transfer object containing the updated status and order items.</param>
        /// <returns>An OrderDTO containing the updated order details.</returns>
        /// <exception cref="Exception">Thrown when the order, order item, or product is not found, or an error occurs during the update operation.</exception>
        public async Task<ResultDTO> UpdateOrderAsync(UpdateOrderDTO dto)
        {
            try
            {
                _logger.LogInformation("Updating order: {OrderId}", dto.Id);

                var order = await _unitOfWork.Repository<Order>()
                    .FindAsync(o => o.Id == dto.Id);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", dto.Id);
                    throw new Exception("Order not found.");
                }

                // Update status
                if (!string.IsNullOrEmpty(dto.Status))
                {
                    switch (dto.Status)
                    {
                        case "طلب جديد":
                            order.OrderStatus = OrderStatus.NewOrder;
                            break;
                        case "تحت الاجراء":
                            order.OrderStatus = OrderStatus.Processing;
                            break;
                        case "تم انهاء الطلب":
                            order.OrderStatus = OrderStatus.Shipped;
                            break;
                        case "الغاء الطلب":
                            order.OrderStatus = OrderStatus.Cancelled;
                            break;
                        default:
                            _logger.LogWarning("Invalid order status: {Status}", dto.Status);
                            throw new Exception($"Invalid order status: {dto.Status}");
                    }
                }

                await _unitOfWork.Repository<Order>().UpdateAsync(order);
                await _unitOfWork.Complete();

                _logger.LogInformation("IsSuccessfully updated order: {OrderId}", dto.Id);
                
                return new ResultDTO
                {
                    IsSuccess = true,
                    Message = "Order updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", dto.Id);
                throw;
            }
        }

        #endregion


        #region Delete Order
        /// <summary>
        /// deletes an order and its associated order items by the specified order ID.
        /// </summary>
        /// <param name="orderId">The ID of the order to be deleted.</param>
        /// <returns>A Task representing the asynchronous deletion operation.</returns>
        /// <exception cref="Exception">Thrown when the order is not found or an error occurs during the deletion process.</exception>
        public async Task DeleteOrderAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Deleting order: {OrderId}", orderId);

                var scpc = new OrderSpecification(orderId);
                var order = await _unitOfWork.Repository<Order>().GetBySpecAsync(scpc);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    throw new Exception("Order not found.");
                }

                await _unitOfWork.Repository<Order>().DeleteAsync(order.Id);
                await _unitOfWork.Complete();

                _logger.LogInformation("IsSuccessfully deleted order: {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {OrderId}", orderId);
                throw;
            }
        }

        #endregion


        #region Confirm Checkout
        /// <summary>
        /// Confirms a checkout by creating an order, sending a WhatsApp notification, and sending an email notification.
        /// </summary>
        /// <param name="dto">The data transfer object containing the user ID and order items for the checkout.</param>
        /// <returns>A ResultDTO indicating the success or failure of the checkout operation, including any error messages.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the WhatsApp notification process.</exception>
        public async Task<ResultDTO> ConfirmCheckout(CreateOrderDTO dto)
        {
            if(dto == null)
            {
                _logger.LogWarning("Checkout DTO is null.");
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Invalid checkout data."
                };
            }

            if (dto.ProductId <= 0)
            {
                _logger.LogWarning("Invalid ProductId: {ProductId}", dto.ProductId);
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Invalid product."
                };
            }

            var product = await _unitOfWork.Repository<Product>().GetBySpecAsync(new ProductSpecification(dto.ProductId));
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", dto.ProductId);
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Product not found."
                };
            }

            //if(!product.Status.Contains(dto.Status)) 
            //{
            //    _logger.LogWarning("Product status mismatch: {ProductStatus}", product.Status);
            //    return new ResultDTO
            //    {
            //        IsSuccess = false,
            //        Message = "Product status does not match."
            //    };
            //}

            var result = await CreateOrderAsync(dto);

            if (!result.IsSuccess)
            {
                return result;
            }

            var checkout = new CheckoutDTO
            {
                Name = dto.Name,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                RentalPeriod = dto.RentalPeriod,
                ProductName = product.Name,
                Brand = product.Brand,
                Modal = product.Modal,
                ProductCategory = product.Category.Name,
                Status = product.Status,
            };

            try
            {
                #region Old Massage
                
//                string messageBody = $@"📦 *إشعار طلب جديد - متجر العوفي* 📦

//*عزيزي مدير المتجر،*

//تم تسجيل طلب شراء جديد في *متجر العوفي*. يرجى مراجعة التفاصيل أدناه:

//*تفاصيل العميل:*
//الاسم: {checkout.Name}
//البريد الإلكتروني: {checkout.Email}
//رقم الهاتف: {checkout.PhoneNumber}
//العنوان: {checkout.Address}

//*المنتجات المطلوبة:*
//{$"المنتج: {checkout.ProductName}"}
//{$"الوصف: {checkout.ProductDescription}"}
//{$"الفئة: {checkout.ProductCategory}"}
//{$"العلامه التجاريه: {checkout.Brand}"}
//{$"الموديل: {checkout.Modal}"}

//لأي استفسارات، تواصلوا مع الدعم: {_orderSettings.Email}  
//🎗️ *فريق متجر العوفي*";

                #endregion

                TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);


                string messageBody = $@"📦 إشعار طلب جديد - متجر العوفي 📦

عزيزي مدير المتجر،

تم تسجيل طلب جديد في متجر العوفي. يرجى مراجعة التفاصيل أدناه:

تفاصيل العميل
:الاسم: {checkout.Name} ,
البريد الإلكتروني: {checkout.Email} ,
رقم الهاتف: {checkout.PhoneNumber} ,
العنوان: {checkout.Address}

تفاصيل الطلب:
نوع الطلب: {(checkout.RentalPeriod != null ? $"إيجار (مدة الإيجار: {checkout.RentalPeriod})" : "شراء")} ,
المنتج: {checkout.ProductName} ,
الفئة: {checkout.ProductCategory} ,
العلامة التجارية: {checkout.Brand} ,
الموديل: {checkout.Modal}

لأي استفسارات، تواصلوا مع الدعم: {_orderSettings.Email}
فريق متجر العوفي";

                var message = MessageResource.Create(
                    body: messageBody,
                    from: new PhoneNumber(_twilioSettings.FromNumber),
                    to: new PhoneNumber(_twilioSettings.ToNumber)
                );

            }
            catch (Exception ex)
            {
                throw;
            }
            

            var contant = GetEmailTemplate(checkout);

            var emailData = new EmailDTO
            {
                Name = $"{dto.Name}",
                Contant = contant,
                //Email = _orderSettings.Email,
                Email = _unitOfWork?.Repository<ApplicationUser>()
                ?.FindAsync(u => u.FirstName == "Admin").Result?.Email,
                Subject = "إشعار طلب جديد ",
                Title = "إشعار طلب جديد"
            };

            var emailSent = await _unitOfWork.EmailServices.SendEmailAsync(emailData);

            if (!emailSent.IsSuccess)
            {
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Failed to send email notification."
                };
            }

            product.Quantity = product.Quantity == 0 ? 0 : product.Quantity--;
            return emailSent;
        }

        #endregion


        #region GetEmailTemplate
        /// <summary>
        /// Generates an HTML email template for a checkout confirmation, including customer details, order details, and a responsive product table.
        /// </summary>
        /// <param name="dto">The data transfer object containing checkout details such as customer information, order items, and order metadata.</param>
        /// <returns>A string containing the formatted HTML email template.</returns>
        private string GetEmailTemplate(CheckoutDTO dto)
        {
            return $@"
<!DOCTYPE html>
<html lang=""ar"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>إشعار طلب جديد</title>
    <style>
        /* Base styles */
        body {{
            font-family: Arial, sans-serif;
            background-color: #f8f8f8;
            padding: 0;
            margin: 0;
            direction: rtl;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
        }}
        a {{
            text-decoration: none;
        }}
        th, td {{
            word-wrap: break-word;
        }}
        /* Desktop styles for details table */
        .details-table th {{
            background-color: #f9f9f9;
            border: 1px solid #ddd;
            padding: 10px;
        }}
        .details-table td {{
            border: 1px solid #ddd;
            padding: 10px;
        }}
        /* Responsive styles */
        @media only screen and (max-width: 600px) {{
            .container {{
                width: 100% !important;
                min-width: 100% !important;
                max-width: 100% !important;
            }}
            .content {{
                padding: 15px !important;
            }}
            .header, .footer {{
                padding: 15px !important;
            }}
            .button {{
                padding: 10px 20px !important;
                font-size: 14px !important;
            }}
            p, td, th {{
                font-size: 14px !important;
            }}
            h2 {{
                font-size: 20px !important;
            }}
            h3 {{
                font-size: 16px !important;
            }}
            /* Stack details table */
            table.details-table td,
            table.details-table th {{
                display: block;
                width: 100% !important;
                box-sizing: border-box;
                text-align: right !important;
            }}
            table.details-table td:first-child,
            table.details-table th:first-child {{
                border-bottom: none;
            }}
        }}
    </style>
</head>
<body>
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8f8f8; padding: 20px 0;"">
        <tr>
            <td align=""center"">
                <table class=""container"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.05); max-width: 600px; min-width: 320px;"">
                    <tr>
                        <td class=""header"" style=""background-color: #FFCD11; color: #333; padding: 20px; text-align: center;"">
                            <h2 style=""margin: 0; font-size: 24px;"">إشعار طلب جديد</h2>
                        </td>
                    </tr>
                    <tr>
                        <td class=""content"" style=""padding: 30px;"">
                            <p style=""font-size: 16px; color: #333;"">عزيزي مدير المتجر،</p>
                            <p style=""font-size: 16px; color: #333;"">
                                تم تسجيل طلب جديد في <strong>متجر العوفي</strong>. يرجى مراجعة التفاصيل أدناه لاتخاذ الإجراءات اللازمة:
                            </p>
                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">تفاصيل العميل</h3>
                            <table class=""details-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>الاسم:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.Name}</td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>البريد الإلكتروني:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><a href='mailto:{dto.Email}'>{dto.Email}</a></td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>رقم الهاتف:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><a href='https://wa.me/{dto.PhoneNumber}'>{dto.PhoneNumber}</a></td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>العنوان:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.Address}</td>
                                </tr>
                            </table>
                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">تفاصيل الطلب</h3>
                            <table class=""details-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>نوع الطلب:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{(dto.RentalPeriod != null ? $"إيجار (مدة الإيجار: {dto.RentalPeriod})" : "شراء")}</td>
                                </tr>
                            </table>
                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">تفاصيل المنتج</h3>
                            <table class=""details-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>اسم المنتج:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.ProductName}</td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>الفئة:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.ProductCategory}</td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>الموديل:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.Modal}</td>
                                </tr>
                                <tr>
                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>العلامة التجارية:</strong></td>
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.Brand}</td>
                                </tr>
                            </table>
                            <p style=""font-size: 16px; color: #333; margin-top: 20px;"">
                                يرجى التحقق من الطلب واتخاذ الإجراءات اللازمة عبر لوحة التحكم.
                            </p>
                            <p style=""font-size: 16px; color: #333;"">
                                إذا كانت هناك أي استفسارات، يرجى التواصل مع فريق الدعم الفني:
                            </p>
                            <p style=""text-align: center; margin: 30px 0;"">
                                <a href=""mailto:help@aloufi-store.com"" class=""button"" style=""background-color: #FFCD11; color: #333; padding: 12px 24px; text-decoration: none; font-size: 16px; border-radius: 5px; display: inline-block;"">تواصلوا مع الدعم</a>
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td class=""footer"" style=""background-color: #fff5cc; padding: 20px; text-align: center; font-size: 14px; color: #666;"">
                            <p style=""margin: 0;"">فريق متجر العوفي</p>
                            <p style=""margin: 10px 0;"">
                                <a href=""mailto:help@aloufi-store.com"" style=""color: #FFCD11; text-decoration: none;"">help@aloufi-store.com</a>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        #endregion

    }
}