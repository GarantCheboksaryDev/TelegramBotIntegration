using ConfigSettings;
using CustomJobHandler;
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
    partial class BotWrapper
    {
        public static TelegramBotClient Telegram { get; set; }
        public static ODataClient OdataClient { get; set; }
        public static Sungero.Logging.ILog Logger { get; set; }

        public static ServiceSettings settings = ServiceSettings.Instance;

        /// <summary>
        /// Api-токен чат-бота.
        /// </summary>
        public static string Token = settings.Token;
        /// <summary>
        /// Адрес сервиса интеграции Directum RX.
        /// </summary>
        public static string IntegrationServiceUrl = settings.IntegrationServiceUrl;
        /// <summary>
        /// Логин для аутентификации в Directum RX.
        /// </summary>
        public static string Login = settings.Login;
        /// <summary>
        /// Пароль для аутентификации в Directum RX.
        /// </summary>
        public static string Password = settings.Password;
        /// <summary>
        /// Пароль для аутентификации в Directum RX.
        /// </summary>
        public static int EntitiesOnPage = settings.EntitiesOnPage;
        /// <summary>
        /// Пароль для аутентификации в Directum RX.
        /// </summary>
        public static int MaxTotalLengthInMarkup = settings.MaxTotalLengthInMarkup;
        /// <summary>
        /// Пароль для аутентификации в Directum RX.
        /// </summary>
        public static int UserCheckInterval = settings.UserCheckInterval;

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
