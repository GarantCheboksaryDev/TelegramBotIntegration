using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService;
using BotWorkerService;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TelegramBot
{
    /// <summary>
    /// Обработка обновлений (сообщений и нажатий кнопок) от пользователя.
    /// </summary>
    partial class BotProgram
    {
        /// <summary>
        /// Обработка полученных из чата обновлений.
        /// </summary>
        /// <param name="botClient">Экземпляр клиента чат-бота.</param>
        /// <param name="update">Обновление.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns></returns>
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                // Id чата, по которому получено обновление.
                var chatId = CommonFunctions.GetChatId(update);

                if (!UserInfo.ContainsKey(chatId))
                {
                    UserInfo[chatId] = new UserInfo(null, State.Start);
                    await ShowMainMenu(update, true);
                    return;
                }

                var lastUserCheck = UserInfo[chatId].LastUserCheck;
                if (UserInfo[chatId].State != State.Registration && UserInfo[chatId].State != State.ConfirmationCode && (!lastUserCheck.HasValue || lastUserCheck.Value.AddSeconds(Constants.CheckingPeriod) < DateTime.Now))
                {
                    var identified = await IdentificateUser(update);
                    if (!identified)
                        return;
                }

                // Определение типа обновления. 
                switch (update.Type)
                {
                    #region Текстовое сообщение

                    case UpdateType.Message:

                        Logger.LogTrace(JsonSerializer.Serialize((update.Message), options));

                        if (update.Message != null && (update.Message.Text != null || update.Message.Caption != null) && update.Message.From != null && !update.Message.From.IsBot)
                        {
                            var message = update.Message;
                            var messageText = message.Text ?? message.Caption;

                            await CommonFunctions.RemoveInline(update);

                            // Переход в главное меню.
                            if (messageText == BotWorkerService.Constants.StartMessage || messageText == BotWorkerService.Constants.NavigationConstants.MainMenu)
                            {
                                await ShowMainMenu(update, true);
                                return;
                            }
                            // Повторный поиск записей.
                            else if (messageText == BotWorkerService.Constants.NavigationConstants.SearchAgain)
                            {
                                UserInfo[chatId].Page = 0;
                                await CommonFunctions.RemoveKeyboard(update);

                                switch (UserInfo[chatId].State)
                                {
                                    case State.ChoosingCounterparty:
                                        UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите наименование контрагента", replyMarkup: GetBackToMenuMarkup());
                                        UserInfo[chatId].State = State.EnteringCounterpartyName;
                                        break;
                                    case State.ChoosingDocumentType:
                                        UserInfo[chatId].Entities = CommonFunctions.GetEntitiesListFromDynamic(await IntegrationDirectumRX.GetDocumentTypes());
                                        UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Выберите тип документа", replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
                                        UserInfo[chatId].State = State.ChoosingDocumentType;
                                        break;
                                    case State.ChoosingDocument:
                                        UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите наименование документа", replyMarkup: GetBackToMenuMarkup());
                                        UserInfo[chatId].State = State.EnteringDocumentName;
                                        break;
                                }
                                return;
                            }
                            // Перемещение по большому списку записей.
                            else if (messageText == BotWorkerService.Constants.NavigationConstants.Next || messageText == BotWorkerService.Constants.NavigationConstants.Previous)
                                HandleListNavigation(messageText, chatId);

                            switch (UserInfo[chatId].State)
                            {
                                case State.Start:
                                    await botClient.SendMessage(chatId, $"Для начала введите {BotWorkerService.Constants.StartMessage}");
                                    break;

                                case State.Registration:
                                    Registration(update, chatId);
                                    break;

                                case State.ConfirmationCode:
                                    CheckConfirmationCode(update, message, chatId);
                                    break;

                                case State.EnteringCounterpartyName:
                                    await CommonFunctions.RemoveInline(update);
                                    UserInfo[chatId].Entities = CommonFunctions.GetEntitiesListFromDynamic(await IntegrationDirectumRX.GetCounterpartiesByName(messageText));
                                    UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Выберите контрагента", replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
                                    UserInfo[chatId].State = State.ChoosingCounterparty;
                                    break;

                                case State.ChoosingCounterparty:
                                    var counterparty = UserInfo[chatId].Entities.Where(x => x.Name == messageText).FirstOrDefault();
                                    if (counterparty != null)
                                    {
                                        UserInfo[chatId].Page = 0;
                                        ShowCounterpartyInfo(update, counterparty.Id);
                                        UserInfo[chatId].State = State.ShownCounterparty;
                                        await CommonFunctions.RemoveKeyboard(update);
                                    }
                                    break;

                                case State.ChoosingDocumentType:
                                    var documentType = UserInfo[chatId].Entities.Where(x => x.Name == messageText).FirstOrDefault();
                                    if (documentType != null)
                                    {
                                        UserInfo[chatId].Page = 0;
                                        UserInfo[chatId].DocumentTypeId = documentType.Id;
                                        await CommonFunctions.RemoveKeyboard(update);
                                        UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите наименование документа", replyMarkup: GetBackToMenuMarkup());
                                        UserInfo[chatId].State = State.EnteringDocumentName;
                                    }
                                    break;

                                case State.EnteringDocumentName:
                                    if (UserInfo[chatId].DocumentTypeId.HasValue)
                                    {
                                        var documents = await IntegrationDirectumRX.GetDocuments(update, messageText, UserInfo[chatId].DocumentTypeId.Value);
                                        if (!string.IsNullOrEmpty(documents[Constants.EntityProperties.Error]))
                                        {
                                            UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, documents[Constants.EntityProperties.Error] as string, replyMarkup: GetBackToMenuMarkup());
                                        }
                                        else
                                        {
                                            UserInfo[chatId].Entities = CommonFunctions.GetEntitiesListFromDynamic(documents[Constants.EntityProperties.EntityInfos]);
                                            UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Выберите документ", replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
                                            UserInfo[chatId].State = State.ChoosingDocument;
                                        }
                                    }
                                    break;

                                case State.ChoosingDocument:
                                    var document = UserInfo[chatId].Entities.Where(x => x.Name == messageText).FirstOrDefault();
                                    if (document != null)
                                    {
                                        UserInfo[chatId].Page = 0;
                                        ShowDocument(update, document.Id, chatId);
                                    }
                                    break;

                                case State.EnteringRequestText:
                                    CreateRequest(update);
                                    break;
                            }
                        }
                        break;

                    #endregion

                    #region Нажатие на кнопку в чате

                    case UpdateType.CallbackQuery:

                        Logger.LogTrace(JsonSerializer.Serialize((update.CallbackQuery), options));

                        var callBackQuery = update.CallbackQuery;
                        if (callBackQuery != null)
                        {
                            await botClient.AnswerCallbackQuery(callBackQuery.Id);

                            if (UserInfo.ContainsKey(chatId))
                            {
                                await CommonFunctions.RemoveInline(update);

                                if (UserInfo[chatId].State == State.Start)
                                {
                                    await botClient.SendMessage(chatId, "Для начала введите /start");
                                    break;
                                }
                                else if (callBackQuery.Data == Constants.NavigationConstants.MainMenu)
                                {
                                    await ShowMainMenu(update, false);
                                    break;
                                }

                                switch (UserInfo[chatId].State)
                                {
                                    case State.ConfirmationCode:
                                        switch (callBackQuery.Data)
                                        {
                                            case Constants.NavigationConstants.Registration.EnterEmainAgain:
                                                await ShowMainMenu(update, false);
                                                break;
                                            case Constants.NavigationConstants.Registration.SendNewCode:
                                                Registration(update, chatId);
                                                break;
                                        }
                                        break;

                                    case State.MainMenu:
                                        switch (callBackQuery.Data)
                                        {
                                            case Constants.NavigationConstants.MainMenuActions.SendRequest:
                                                UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите текст заявки. При необходимости, прикрепите к сообщению фото, видео или документ", replyMarkup: GetBackToMenuMarkup());
                                                UserInfo[chatId].State = State.EnteringRequestText;
                                                break;
                                            case Constants.NavigationConstants.MainMenuActions.FindDocument:
                                                UserInfo[chatId].Entities = CommonFunctions.GetEntitiesListFromDynamic(await IntegrationDirectumRX.GetDocumentTypes());
                                                UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Выберите тип документа", replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
                                                UserInfo[chatId].State = State.ChoosingDocumentType;
                                                break;
                                            case Constants.NavigationConstants.MainMenuActions.FindCounterparty:
                                                UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите наименование контрагента", replyMarkup: GetBackToMenuMarkup());
                                                UserInfo[chatId].State = State.EnteringCounterpartyName;
                                                break;
                                        }
                                        break;

                                    case State.ShownCounterparty:
                                        switch (callBackQuery.Data)
                                        {
                                            case Constants.NavigationConstants.SearchAgain:
                                                UserInfo[chatId].LastMessage = await botClient.SendMessage(chatId, "Введите наименование контрагента", replyMarkup: GetBackToMenuMarkup());
                                                UserInfo[chatId].State = State.EnteringCounterpartyName;
                                                break;
                                        }
                                        break;

                                    case State.ShownDocument:
                                        if (callBackQuery.Data == Constants.NavigationConstants.Previous)
                                        {
                                            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Выберите документ", replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
                                            UserInfo[chatId].State = State.ChoosingDocument;
                                            UserInfo[chatId].Page = 0;
                                        }
                                        break;
                                }
                            }
                        }
                        break;

                        #endregion
                }
            }
            catch (Exception ex)
            {
                HandleError(update, ex);
            }
        }
    }
}
