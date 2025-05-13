using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using CustomJobHandler;

namespace TelegramBot
{
    /// <summary>
    /// Служебные функции для работы сервиса.
    /// </summary>
    partial class BotWrapper
    {
        /// <summary>
        /// Метод, вызываемый при возникновении исключения.
        /// </summary>
        /// <param name="botClient">Экземпляр клиента TelegramBot.</param>
        /// <param name="exception">Исключение.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns></returns>
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Logger.Error(exception.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Точка входа в обработчик чат-бота.
        /// </summary>
        /// <param name="logger">Экземпляр логгера.</param>
        public static void Start(Sungero.Logging.ILog logger)
        {
            Logger = logger;

            Simple.OData.Client.V3Adapter.Reference();
            Simple.OData.Client.V4Adapter.Reference();

            Telegram = new TelegramBotClient(Token);
            OdataClient = IntegrationDirectumRX.AuthenticationOdataCLient(IntegrationServiceUrl, Login, Password);
            logger.Info(Telegram.GetMe().Result.FirstName + Environment.NewLine);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            Telegram.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
        }

        /// <summary>
        /// Обработка ошибок.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="ex">Ошибка.</param>
        /// <param name="text">Текст для пользователя.</param>
        public static async void HandleError(Update update, Exception ex, string text)
        {
            Logger.Error(ex.ToString());
            var chatId = CommonFunctions.GetChatId(update);
            if (string.IsNullOrEmpty(text)) 
                text = "Произошла ошибка, повторите попытку позже";
            await Telegram.SendMessage(chatId, text, replyMarkup: new ReplyKeyboardRemove());
            UserInfo[chatId].State = TelegramBotService.State.Start;
            await Telegram.SendMessage(chatId, "Для начала введите /start");
        }

        public static async void HandleError(Update update, Exception ex) => HandleError(update, ex, string.Empty);

    }
}
