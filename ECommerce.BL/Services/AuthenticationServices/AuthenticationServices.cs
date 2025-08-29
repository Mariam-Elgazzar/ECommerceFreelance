using ECommerce.BL.DTO.AuthenticationDTOs;
using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.Helper;
using ECommerce.BL.Settings;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace ECommerce.BL.Services.AuthenticationService
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AdminLogin _adminLogin;
        private readonly JWT _jwt;
        private readonly ILogger<AuthenticationServices> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmailConfiguration _configuration;
        public AuthenticationServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AdminLogin> adminLogin,
            IOptions<JWT> jwt,
            ILogger<AuthenticationServices> logger,
            IUnitOfWork unitOfWork,
            IOptions<EmailConfiguration> configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _adminLogin = adminLogin.Value;
            _jwt = jwt.Value;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _configuration = configuration.Value;
        }


        #region Register
        /// <summary>
        /// Registers a new user with the provided credentials and returns an authentication response.
        /// </summary>
        /// <param name="data">The registration data containing user details such as email and password.</param>
        /// <returns>An <see cref="AuthenticationDTO"/> containing authentication status, user details, JWT token, and error message if applicable.</returns>
        public async Task<AuthenticationDTO> Register(RegisterDTO data)
        {
            var validationContext = new ValidationContext(data);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(data, validationContext, validationResults, true);

            if (!isValid)
            {
                _logger.LogWarning("Validation failed for user registration: {Email}", data.Email);
                var errors = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = $"Validation failed: {errors}"
                };
            }

            if (await _userManager.FindByEmailAsync(data.Email) != null)
            {
                _logger.LogWarning("User with email {Email} already exists.", data.Email);
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = "User already exists."
                };
            }

            var user = new ApplicationUser
            {
                IsDeleted = false,
                FirstName = data.FirstName,
                LastName = data.LastName,
                Address = data.Address,
                UserName = data.Email,
                Email = data.Email,
                PhoneNumber = data.PhoneNumber
            };

            _logger.LogInformation("Creating user with email: {Email}", data.Email);
            var result = await _userManager.CreateAsync(user, data.Password);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user {Email}: {Errors}",
                    data.Email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = $"Registration failed: {string.Join("; ", result.Errors.Select(e => e.Description))}"
                };
            }

            _logger.LogInformation("Assigning role to user: {Email}", data.Email);

            if (data.FirstName == Roles.Admin)
                await _userManager.AddToRoleAsync(user, Roles.Admin);
            else
                await _userManager.AddToRoleAsync(user, Roles.User);

            _logger.LogInformation("Generating JWT for user: {Email}", data.Email);
            var token = await CreateJWTToken(user);

            _logger.LogInformation("User successfully registered: {Email}", data.Email);
                        
            return new AuthenticationDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Message = "Registration successful",
                Roles = "User"
            };
        }

        #endregion


        #region Login
        /// <summary>
        /// Authenticates a user with the provided login credentials and returns an authentication response.
        /// </summary>
        /// <param name="data">The login data containing email and password.</param>
        /// <returns>An <see cref="AuthenticationDTO"/> containing authentication status, user details, JWT token, and error message if applicable.</returns>
        public async Task<AuthenticationDTO> Login(LoginDTO data)
        {
            if (string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
            {
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = "Email and password are required."
                };
            }

            var user = await _userManager.FindByEmailAsync(data.Email);

            if (user == null && data.Email == _adminLogin.Email && data.Password == _adminLogin.Password)
            {
                return await Register(new RegisterDTO
                {
                    FirstName = "Admin",
                    LastName = "Admin",
                    Email = data.Email,
                    PhoneNumber = null,
                    Address = null,
                    Password = data.Password
                });
            }
            else if (user == null)
            {
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or password."
                };
            }

            bool isPasswordValid = await _userManager.CheckPasswordAsync(user, data.Password);
            if (!isPasswordValid)
            {
                return new AuthenticationDTO
                {
                    IsAuthenticated = false,
                    Message = "Invalid email or password."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);

            var token = await CreateJWTToken(user);

            return new AuthenticationDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                IsAuthenticated = true,
                Message = "Login successful.",
                Roles = roles.FirstOrDefault() ?? "User"
            };
        }

        #endregion


        #region Forget Password

        /// <summary>
        /// Initiates a password reset process by sending a reset link to the user's email.
        /// </summary>
        /// <param name="dto">The data containing the user's email for password reset.</param>
        /// <returns>An <see cref="ResultDTO"/> containing the status and message of the email operation.</returns>
        public async Task<ResultDTO> ForgetPassword(ForgetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new ResultDTO
                {
                    Message = "Email is not registered!"
                };
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                token = HttpUtility.UrlEncode(token);
                var data = new EmailDTO
                {
                    Name = $"{user.FirstName} {user.LastName}",
                    Contant = GetEmailTemplate($"{user.FirstName} {user.LastName}", $"{_configuration.PasswordResetLink}?email={user.Email}&token={token}"),
                    Email = dto.Email,
                    Subject = "إعادة تعيين كلمة المرور الخاصة بحسابك في متجر العوفي",
                    Title = "إعادة تعيين كلمة المرور"
                };
                var message = await _unitOfWork.EmailServices.SendEmailAsync(data);

                return message;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion


        #region Reset Password

        /// <summary>
        /// Resets a user's password using the provided reset token and new password.
        /// </summary>
        /// <param name="dto">The data containing email, reset token, and new password.</param>
        /// <returns>A string indicating the result of the password reset operation.</returns>
        public async Task<string> ResetPassword(ResetPasswordDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.Password))
            {
                return "Email, reset token, and new password are required.";
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return "Email is not registered!";
            }

            try
            {
                var resetResult = await _userManager.ResetPasswordAsync(user, dto.Token, dto.Password);
                if (!resetResult.Succeeded)
                {
                    var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    return $"Failed to reset password: {errors}";
                }
                return "Password reset successful.";
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion


        #region Change Password

        /// <summary>
        /// Changes a user's password after verifying the old password.
        /// </summary>
        /// <param name="dto">The data containing user ID, old password, and new password.</param>
        /// <returns>A string indicating the result of the password change operation.</returns>
        public async Task<string> ChangePassword(ChangePasswordDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.OldPassword) || string.IsNullOrEmpty(dto.NewPassword))
                return "User ID, old password, and new password are required.";

            if (dto.OldPassword == dto.NewPassword)
                return "New password cannot be the same as the old password.";

            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
                return "User not found!";

            try
            {
                var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, dto.OldPassword);
                if (!isOldPasswordValid)
                    return "Old password is incorrect.";

                var changeResult = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
                if (!changeResult.Succeeded)
                {
                    var errors = string.Join("; ", changeResult.Errors.Select(e => e.Description));
                    return $"Failed to change password: {errors}";
                }

                return "Password changed successfully.";
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        #endregion


        #region Create JWT Token
        /// <summary>
        /// Creates a JWT token for the specified user with their claims and roles.
        /// </summary>
        /// <param name="user">The user for whom the JWT token is generated.</param>
        /// <returns>A <see cref="JwtSecurityToken"/> containing the user's claims and authentication details.</returns>
        private async Task<JwtSecurityToken> CreateJWTToken(ApplicationUser user)
        {
            var UserClaims = await _userManager.GetClaimsAsync(user);

            var Roles = await _userManager.GetRolesAsync(user);
            var RoleClaims = new List<Claim>();

            foreach (var role in Roles)
                RoleClaims.Add(new Claim(ClaimTypes.Role, role));

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }
            .Union(UserClaims)
            .Union(RoleClaims);

            SecurityKey securityKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            SigningCredentials signingCredentials =
                new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var JWTSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.DurationInDays),
                signingCredentials: signingCredentials);
            return JWTSecurityToken;
        }

        #endregion


        #region Reset Password Template

        /// <summary>
        /// Generates an HTML email template for password reset.
        /// </summary>
        /// <param name="name">The name of the user receiving the email.</param>
        /// <param name="ResetLink">The link for resetting the password.</param>
        /// <returns>A string containing the HTML email template.</returns>
        private string GetEmailTemplate(string name, string ResetLink)
        {
            return $@"
        <!DOCTYPE html>
        <html lang=""ar"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>إعادة تعيين كلمة المرور</title>
            <style>
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
                    p {{
                        font-size: 14px !important;
                    }}
                    h2 {{
                        font-size: 20px !important;
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
                                    <h2 style=""margin: 0; font-size: 24px;"">إعادة تعيين كلمة المرور</h2>
                                </td>
                            </tr>
                            <tr>
                                <td class=""content"" style=""padding: 30px;"">
                                    <p style=""font-size: 16px; color: #333;"">مرحبًا {name}،</p>
                                    <p style=""font-size: 16px; color: #333;"">
                                        لقد تلقينا طلبًا لإعادة تعيين كلمة المرور الخاصة بحسابك في <strong>متجر التجارة الإلكترونية</strong>.
                                    </p>
                                    <p style=""font-size: 16px; color: #333;"">
                                        إذا كنت قد أرسلت هذا الطلب، يرجى الضغط على الزر أدناه للمتابعة:
                                    </p>
                                    <p style=""text-align: center; margin: 30px 0;"">
                                        <a href=""{ResetLink}"" class=""button"" style=""background-color: #FFCD11; color: #333; padding: 12px 24px; text-decoration: none; font-size: 16px; border-radius: 5px; display: inline-block;"">إعادة تعيين كلمة المرور</a>
                                    </p>
                                    <p style=""font-size: 16px; color: #333;"">
                                        إذا لم تكن أنت من أرسل هذا الطلب، يمكنك تجاهل هذه الرسالة بأمان، وستظل كلمة المرور الخاصة بك دون تغيير.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td class=""footer"" style=""background-color: #fff5cc; padding: 20px; text-align: center; font-size: 14px; color: #666;"">
                                    <p style=""margin: 0;"">شكرًا لاستخدامك منصتنا،</p>
                                    <p style=""margin: 0;"">فريق متجر التجارة الإلكترونية</p>
                                    <p style=""margin: 10px 0;"">
                                        <a href=""mailto:support@ecommerce.com"" style=""color: #FFCD11; text-decoration: none;"">تواصلوا مع الدعم</a>
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
        
        //        private string GetEmailTemplate(string adminName, string ResetLink)
        //        {
        //            // Hardcoding fictional data
        //            string customerName = "ليلى الساحرة";
        //            string customerEmail = "laila.wizard@moonland.com";
        //            string customerPhone = "+123 987 654 321";
        //            string orderId = "MAGIC-007";
        //            string orderDate = "15 يونيو 2025";
        //            List<(string ProductName, int Quantity, decimal Price)> items = new List<(string, int, decimal)>
        //    {
        //        ("عباءة سحرية", 1, 1500.00m),
        //        ("عصا القوة", 3, 250.00m)
        //    };
        //            decimal total = 2250.00m;

        //            // Build the items table rows dynamically for desktop view
        //            string itemsTableRows = "";
        //            foreach (var item in items)
        //            {
        //                itemsTableRows += $@"
        //                                <tr class=""item-row"" data-product=""{item.ProductName}"" data-quantity=""{item.Quantity}"" data-price=""{item.Price:F2} ريال"">
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{item.ProductName}</td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px; text-align: center;"">{item.Quantity}</td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{item.Price:F2} ريال</td>
        //                                </tr>";
        //            }

        //            // Add total row for desktop view
        //            itemsTableRows += $@"
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"" colspan=""2""><strong>الإجمالي:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>{total:F2} ريال</strong></td>
        //                                </tr>";

        //            return $@"
        //<!DOCTYPE html>
        //<html lang=""ar"">
        //<head>
        //    <meta charset=""UTF-8"">
        //    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        //    <title>إشعار طلب جديد</title>
        //    <style>
        //        /* Base styles */
        //        body {{
        //            font-family: Arial, sans-serif;
        //            background-color: #f8f8f8;
        //            padding: 0;
        //            margin: 0;
        //            direction: rtl;
        //        }}
        //        table {{
        //            border-collapse: collapse;
        //            width: 100%;
        //        }}
        //        a {{
        //            text-decoration: none;
        //        }}
        //        th, td {{
        //            word-wrap: break-word;
        //        }}
        //        /* Desktop styles for product table */
        //        .items-table th {{
        //            background-color: #f9f9f9;
        //            border: 1px solid #ddd;
        //            padding: 10px;
        //        }}
        //        .items-table td {{
        //            border: 1px solid #ddd;
        //            padding: 10px;
        //        }}
        //        /* Responsive styles */
        //        @media only screen and (max-width: 600px) {{
        //            .container {{
        //                width: 100% !important;
        //                min-width: 100% !important;
        //                max-width: 100% !important;
        //            }}
        //            .content {{
        //                padding: 15px !important;
        //            }}
        //            .header, .footer {{
        //                padding: 15px !important;
        //            }}
        //            .button {{
        //                padding: 10px 20px !important;
        //                font-size: 14px !important;
        //            }}
        //            p, td, th {{
        //                font-size: 14px !important;
        //            }}
        //            h2 {{
        //                font-size: 20px !important;
        //            }}
        //            h3 {{
        //                font-size: 16px !important;
        //            }}
        //            /* Stack customer details table */
        //            table.details-table td,
        //            table.details-table th {{
        //                display: block;
        //                width: 100% !important;
        //                box-sizing: border-box;
        //                text-align: right !important;
        //            }}
        //            table.details-table td:first-child,
        //            table.details-table th:first-child {{
        //                border-bottom: none;
        //            }}
        //            /* Transform product table into blocks on mobile */
        //            .items-table {{
        //                border: none !important;
        //            }}
        //            .items-table thead {{
        //                display: none !important; /* Hide headers on mobile */
        //            }}
        //            .items-table tr.item-row {{
        //                display: block;
        //                border: 1px solid #ddd;
        //                border-radius: 5px;
        //                margin-bottom: 10px;
        //                background-color: #fff;
        //                box-shadow: 0 1px 3px rgba(0,0,0,0.05);
        //            }}
        //            .items-table tr.item-row td {{
        //                display: block;
        //                width: 100% !important;
        //                box-sizing: border-box;
        //                border: none !important;
        //                text-align: right !important;
        //                padding: 8px 15px !important;
        //            }}
        //            /* Add labels using pseudo-elements */
        //            .items-table tr.item-row td:nth-child(1)::before {{
        //                content: 'المنتج: ';
        //                font-weight: bold;
        //                color: #333;
        //                margin-left: 5px;
        //            }}
        //            .items-table tr.item-row td:nth-child(2)::before {{
        //                content: 'الكمية: ';
        //                font-weight: bold;
        //                color: #333;
        //                margin-left: 5px;
        //            }}
        //            .items-table tr.item-row td:nth-child(3)::before {{
        //                content: 'السعر: ';
        //                font-weight: bold;
        //                color: #333;
        //                margin-left: 5px;
        //            }}
        //            /* Style the total row separately */
        //            .items-table tr:last-child {{
        //                border: none !important;
        //                box-shadow: none !important;
        //                background: none !important;
        //            }}
        //            .items-table tr:last-child td {{
        //                padding: 10px 15px !important;
        //            }}
        //            .items-table tr:last-child td::before {{
        //                content: none !important;
        //            }}
        //        }}
        //    </style>
        //</head>
        //<body>
        //    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8f8f8; padding: 20px 0;"">
        //        <tr>
        //            <td align=""center"">
        //                <table class=""container"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.05); max-width: 600px; min-width: 320px;"">
        //                    <tr>
        //                        <td class=""header"" style=""background-color: #FFCD11; color: #333; padding: 20px; text-align: center;"">
        //                            <h2 style=""margin: 0; font-size: 24px;"">إشعار طلب جديد</h2>
        //                        </td>
        //                    </tr>
        //                    <tr>
        //                        <td class=""content"" style=""padding: 30px;"">
        //                            <p style=""font-size: 16px; color: #333;"">عزيزي {adminName}،</p>
        //                            <p style=""font-size: 16px; color: #333;"">
        //                                تم تسجيل طلب شراء جديد في <strong>متجر العوفي</strong>. يرجى مراجعة التفاصيل أدناه لاتخاذ الإجراءات اللازمة:
        //                            </p>
        //                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">تفاصيل العميل</h3>
        //                            <table class=""details-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>الاسم:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{customerName}</td>
        //                                </tr>
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>البريد الإلكتروني:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{customerEmail}</td>
        //                                </tr>
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>رقم الهاتف:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{customerPhone}</td>
        //                                </tr>
        //                            </table>
        //                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">تفاصيل الطلب</h3>
        //                            <table class=""details-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>رقم الطلب:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{orderId}</td>
        //                                </tr>
        //                                <tr>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;""><strong>تاريخ الطلب:</strong></td>
        //                                    <td style=""border: 1px solid #ddd; padding: 10px;"">{orderDate}</td>
        //                                </tr>
        //                            </table>
        //                            <h3 style=""font-size: 18px; color: #333; margin: 20px 0 10px;"">المنتجات المطلوبة</h3>
        //                            <table class=""items-table"" width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""font-size: 14px; color: #333;"">
        //                                <thead>
        //                                    <tr style=""background-color: #f9f9f9;"">
        //                                        <th style=""border: 1px solid #ddd; padding: 10px;"">المنتج</th>
        //                                        <th style=""border: 1px solid #ddd; padding: 10px;"">الكمية</th>
        //                                        <th style=""border: 1px solid #ddd; padding: 10px;"">السعر</th>
        //                                    </tr>
        //                                </thead>
        //                                <tbody>
        //                                    {itemsTableRows}
        //                                </tbody>
        //                            </table>
        //                            <p style=""font-size: 16px; color: #333; margin-top: 20px;"">
        //                                يرجى التحقق من الطلب واتخاذ الإجراءات اللازمة عبر لوحة التحكم:
        //                            </p>
        //                            <p style=""text-align: center; margin: 30px 0;"">
        //                                <a href=""{ResetLink}"" class=""button"" style=""background-color: #FFCD11; color: #333; padding: 12px 24px; text-decoration: none; font-size: 16px; border-radius: 5px; display: inline-block;"">عرض الطلب</a>
        //                            </p>
        //                            <p style=""font-size: 16px; color: #333;"">
        //                                إذا كانت هناك أي استفسارات، يرجى التواصل مع فريق الدعم الفني.
        //                            </p>
        //                        </td>
        //                    </tr>
        //                    <tr>
        //                        <td class=""footer"" style=""background-color: #fff5cc; padding: 20px; text-align: center; font-size: 14px; color: #666;"">
        //                            <p style=""margin: 0;"">فريق متجر العوفي</p>
        //                            <p style=""margin: 10px 0;"">
        //                                <a href=""mailto:help@aloufi-store.com"" style=""color: #FFCD11; text-decoration: none;"">تواصلوا مع الدعم</a>
        //                            </p>
        //                        </td>
        //                    </tr>
        //                </table>
        //            </td>
        //        </tr>
        //    </table>
        //</body>
        //</html>";
        //        }
        
        #endregion

    }
}
