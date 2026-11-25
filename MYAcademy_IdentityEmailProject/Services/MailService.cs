using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MYAcademy_IdentityEmailProject.Services
{
    // Derste RegisterController içinde yazdığımız MailKit kodunu tek yerde topladık.
    // Kayıt kodu, şifre yenileme bağlantısı ve dış adrese mesaj gönderimi bu sınıfı kullanır.
    public class MailService
    {
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendMail(string toEmail, string subject, string body, bool isHtml = false)
        {
            var settings = _configuration.GetSection("MailSettings");

            MimeMessage mimeMessage = new MimeMessage();

            MailboxAddress mailboxAddressFrom = new MailboxAddress(settings["DisplayName"], settings["Email"]);
            mimeMessage.From.Add(mailboxAddressFrom);

            MailboxAddress mailboxAddressTo = new MailboxAddress("User", toEmail);
            mimeMessage.To.Add(mailboxAddressTo);

            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Connect(settings["Host"], int.Parse(settings["Port"]!), SecureSocketOptions.StartTls);
            smtpClient.Authenticate(settings["Email"], settings["Password"]);
            smtpClient.Send(mimeMessage);
            smtpClient.Disconnect(true);
        }
    }
}
