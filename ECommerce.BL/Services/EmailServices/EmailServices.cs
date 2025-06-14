using ECommerce.BL.DTO.EmailDTOs;
using ECommerce.BL.DTO.GlobalDTOs;
using ECommerce.BL.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ECommerce.BL.Services.EmailServices
{
    public class EmailServices : IEmailServices
    {
        private readonly EmailConfiguration _configuration;
        public EmailServices(IOptions<EmailConfiguration> configuration)
        {
            _configuration = configuration.Value;
        }

        /// <summary>
        /// Asynchronously sends an email using the provided email data.
        /// </summary>
        /// <param name="dto">The email data containing recipient, subject, and content.</param>
        /// <returns>An <see cref="ResultDTO"/> indicating the IsSuccess or failure of the email operation.</returns>
        public async Task<ResultDTO> SendEmailAsync(EmailDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Contant))
            {
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "Invalid email data provided."
                };
            }

            if (string.IsNullOrWhiteSpace(_configuration.SmtpServer) ||
                string.IsNullOrWhiteSpace(_configuration.UserName) ||
                string.IsNullOrWhiteSpace(_configuration.Password) ||
                string.IsNullOrWhiteSpace(_configuration.From))
            {
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "SMTP configuration is incomplete."
                };
            }

            using var client = new SmtpClient();
            try
            {
                var mailMessage = CreateEmailMessage(dto);

                await client.ConnectAsync(_configuration.SmtpServer, _configuration.Port, SecureSocketOptions.StartTls);

                client.AuthenticationMechanisms.Remove("XOAUTH2");

                await client.AuthenticateAsync(_configuration.UserName, _configuration.Password);

                await client.SendAsync(mailMessage);

                return new ResultDTO
                {
                    IsSuccess = true,
                    Message = "Email sent IsSuccessfully."
                };
            }
            catch (SmtpCommandException ex)
            {
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = $"SMTP error: {ex.Message}"
                };
            }
            catch (Exception)
            {
                return new ResultDTO
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred while sending the email."
                };
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }


        /// <summary>
        /// Creates a MimeMessage object from the provided email data.
        /// </summary>
        /// <param name="dto">The email data containing recipient, subject, and content.</param>
        /// <returns>A <see cref="MimeMessage"/> configured with sender, recipient, subject, and HTML body.</returns>
        private MimeMessage CreateEmailMessage(EmailDTO dto)
        {
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress("العوفي" ?? "Sender", _configuration.From));
            mailMessage.To.Add(new MailboxAddress(dto.Name ?? "", dto.Email));
            mailMessage.Subject = dto.Subject ?? "No Subject";
            mailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = dto.Contant
            };
            return mailMessage;
        }
    }
}