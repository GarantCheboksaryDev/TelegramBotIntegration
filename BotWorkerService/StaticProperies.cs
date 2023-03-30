using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramBotService;

namespace TelegramBot
{
    /// <summary>
    /// Статические поля класса.
    /// </summary>
    partial class BotProgram
    {
        private static string IntegrationServiceUrl { get; } = System.Configuration.ConfigurationManager.AppSettings["IntegrationServiceUrl"];
        private static string Login { get; } = System.Configuration.ConfigurationManager.AppSettings["IntegrationUserLogin"];
        private static string Password { get; } = System.Configuration.ConfigurationManager.AppSettings["IntegrationUserPassword"];
        private static string Token { get; } = System.Configuration.ConfigurationManager.AppSettings["TelegramBotApiKey"];
        private static string MailAddress { get; } = System.Configuration.ConfigurationManager.AppSettings["MailAddress"];
        private static string SmtpLogin { get; } = System.Configuration.ConfigurationManager.AppSettings["SmtpLogin"];
        private static string SmtpPassword { get; } = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];
        private static string SmtpHost { get; } = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"];
        private static int SmtpPort { get; } = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"]);
        private static string SmtpSecure { get; } = System.Configuration.ConfigurationManager.AppSettings["SmtpSecure"];

        public static TelegramBotClient Telegram { get; set; }
        public static ODataClient OdataClient { get; set; }
        public static ILogger<BotService> Logger { get; set; }

        /// <summary>
        /// Словарь для хранения информации о состояни ипользователей. Первый параметр массива - Id чата
        /// </summary>
        public static Dictionary<long, UserInfo> UserInfo { get; set; } = new Dictionary<long, UserInfo>();

        /// <summary>
        /// Словарь для хранения информации о коде подверждения для регистрации в чат-боте. Первый параметр массива - Id чата
        /// </summary>
        public static Dictionary<long, ConfirmationCode> ConfirmationInfo { get; set; } = new Dictionary<long, ConfirmationCode>();
    }
}
