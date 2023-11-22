using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using StackExchange.Redis;

class Program
{
    static void Main()
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        var queueKey = "fallasQueue";

        var smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io")
        {
            Port = 587,
            Credentials = new NetworkCredential("9eba10672c4457", "23836081ca86b8"),
            EnableSsl = true,
            UseDefaultCredentials = false,
        };

        var thread = new Thread(() =>
        {
            while (true)
            {
                var message = DequeueMessage(redis, queueKey);

                if (message != null)
                {
                    Console.WriteLine($"Procesando mensaje: {message}");
                    SendEmail(smtpClient, "arielsebastiandiaz454@gmail.com", "Notificación de Falla", message);
                }

                Thread.Sleep(1000);
            }
        });

        thread.Start();

        EnqueueMessage(redis, queueKey, "Nueva falla detectada");
        EnqueueMessage(redis, queueKey, "Vulnerabilidad encontrada");

        Console.WriteLine("Presiona Enter para salir.");
        Console.ReadLine();

        thread.Abort();
    }

    static void EnqueueMessage(ConnectionMultiplexer redis, string queueKey, string message)
    {
        var db = redis.GetDatabase();
        db.ListRightPush(queueKey, message);
    }

    static string DequeueMessage(ConnectionMultiplexer redis, string queueKey)
    {
        var db = redis.GetDatabase();
        return db.ListLeftPop(queueKey);
    }

    static void SendEmail(SmtpClient smtpClient, string to, string subject, string message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress("tu_correo@mailtrap.io", "Nombre del Remitente"), 
            Subject = subject,
            Body = $"Se ha detectado una nueva falla: {message}",
        };

        mailMessage.To.Add(to);

        smtpClient.Send(mailMessage);

        Console.WriteLine($"Correo electrónico enviado a {to}: {message}");
    }
}
