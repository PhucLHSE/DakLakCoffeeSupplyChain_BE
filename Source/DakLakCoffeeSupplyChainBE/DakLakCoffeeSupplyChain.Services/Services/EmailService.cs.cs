﻿using DakLakCoffeeSupplyChain.Services.IServices;
using System.Net;
using System.Net.Mail;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _port = 587;
        private readonly string _fromEmail = "namptse150442@fpt.edu.vn"; 
        private readonly string _appPassword = "qpcy acnm bpqv kpex";     

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var client = new SmtpClient(_smtpServer, _port)
                {
                    Credentials = new NetworkCredential(_fromEmail, _appPassword),
                    EnableSsl = true
                };

                var mail = new MailMessage(_fromEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                Console.WriteLine($"🚀 Đang gửi email tới: {toEmail}");
                await client.SendMailAsync(mail);
                Console.WriteLine("✅ Email đã gửi thành công.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi gửi email: " + ex.Message);
                throw;
            }
        }
    }
}
