using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.DTO.OrderDTOs;
using ECommerce.BL.Settings;
using ECommerce.BL.Specification.ProductSpecification;
using ECommerce.BL.UnitOfWork;
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

      
        #region Confirm Checkout
        /// <summary>
        /// Confirms a checkout by creating an order, sending a WhatsApp notification, and sending an email notification.
        /// </summary>
        /// <param name="dto">The data transfer object containing the user ID and order items for the checkout.</param>
        /// <returns>A ResultDTO indicating the success or failure of the checkout operation, including any error messages.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the WhatsApp notification process.</exception>
        public async Task<ResultDTO> ConfirmCheckout(OrderDTO dto)
        {
            if(dto == null || dto.Name == null || dto.Email == null|| dto.PhoneNumber == null )
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

            var checkout = new CheckoutDTO
            {
                Name = $"{dto.Name}",
                Address = dto?.Address,
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

                TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);

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
🎗️ فريق متجر العوفي";

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
                Email = _orderSettings.Email, 
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
                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{dto.Email}</td>
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