﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotService;
using MimeKit;
using MailKit.Net.Smtp;
using BotWorkerService;
using System.IO;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CustomJobHandler;

namespace TelegramBot
{
    partial class BotWrapper
    {
        /// <summary>
        /// Идентифицировать пользователя в RX.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <returns>Признак того, найден ли пользователь в системе.</returns>
        public static async Task<bool> IdentificateUser(Update update)
        {
            var chatId = CommonFunctions.GetChatId(update);
            string userName = CommonFunctions.GetUserName(update);
            var prefix = $"IdentificateUser. Логин: {userName}. ИД: {chatId}. ";

            try
            {
                // Проверка наличия пользователя в системе
                Logger.Info($"{prefix}Старт идентификации пользователя в Directum RX.");

                var botUser = await IntegrationDirectumRX.GetTelegramUser(chatId);
                if (botUser == null)
                {
                    if (!UserInfo.ContainsKey(chatId))
                        UserInfo[chatId] = new UserInfo();
                    UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Вы еще не зарегистрированы в чат-боте. Для регистрации введите регистрационный токен, полученный от администратора, или корпоративную почту.");
                    UserInfo[chatId].State = State.Registration;
                    Logger.Info($"{prefix}Пользователь не зарегистрирован в чат-боте.");
                    return false;
                }
                else if (botUser[Constants.BotUserProperties.Status] == Constants.StatusItems.Closed
                    || botUser["Employee"][Constants.BotUserProperties.Status] == Constants.StatusItems.Closed
                    || botUser["Employee"]["Login"][Constants.BotUserProperties.Status] == Constants.StatusItems.Closed)
                {
                    if (!UserInfo.ContainsKey(chatId))
                        UserInfo[chatId] = new UserInfo();
                    UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Ваша учетная запись заблокирована", replyMarkup: new ReplyKeyboardRemove());
                    UserInfo[chatId].State = State.Start;
                    Logger.Info($"{prefix}Карточка сотрудника, учетной записи или пользователя чат-бота закрыты в Directum RX.");
                    return false;
                }
                else
                {
                    if (botUser[Constants.BotUserProperties.ChatId] == null || botUser[Constants.BotUserProperties.ChatId] != chatId.ToString())
                    {
                        //Установка Id чата
                        IntegrationDirectumRX.SetChatId(update);
                    }
                    UserInfo[chatId].LastUserCheck = DateTime.Now;
                    Logger.Info($"{prefix}Пользователь идентифицирован.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                HandleError(update, ex);
                return false;
            }
        }


        /// <summary>
        /// Проверка наличия в Directum RX действующего сотрудника с указанной электронной почтой. Если сотрудник найден, то на его электронную почту отправляется код подтверждения.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="chatId">ИД чата.</param>
        public static async void Registration(Update update, long chatId)
        {
            var prefix = $"Registration. ";

            try
            {
                if (UserInfo.ContainsKey(chatId))
                {
                    string emailOrToken = string.Empty;
                    if (update.Message != null)
                        emailOrToken = update?.Message?.Text?.Trim();

                    if (System.Guid.TryParse(emailOrToken, out _))
                    {
                        var result = await IntegrationDirectumRX.RegisterUserByToken(emailOrToken, chatId, CommonFunctions.GetUserName(update), CommonFunctions.GetChatId(update));
                        if (result == null)
                        {
                            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId: chatId, text: "Вы успешно зарегистрировались в чат-боте!");
                            UserInfo[chatId].State = State.MainMenu;
                            ShowMainMenu(update, false);
                        }
                        else
                        {
                            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId: chatId, text: $"Произошла ошибка при регистрации в чат-боте: \"{result}\". Обратитесь к Администратору.");
                            UserInfo[chatId].State = State.Registration;
                        }
                    }
                    else
                    {
                        UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Проверяю электронную почту");

                        if (update.Message == null && ConfirmationInfo.ContainsKey(chatId))
                            emailOrToken = ConfirmationInfo[chatId].Mail;

                        prefix += $"Электронная почта: {emailOrToken}. ";

                        Logger.Info($"{prefix}Поиск сотрудника по электронной почте в Directum RX.");

                        var employee = await IntegrationDirectumRX.GetEmployeeByMail(emailOrToken);
                        if (employee == null)
                        {
                            Logger.Info($"{prefix}Пользователь с данной электронной почтой не найден в Directum RX.");

                            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId: chatId, text: $"Пользователь с данной электронной почтой не найден в Directum RX");
                            UserInfo[chatId].State = State.Registration;
                            return;
                        }

                        Random rand = new Random();
                        int confirmationCode = rand.Next(100000, 999999);
                        if (ConfirmationInfo.ContainsKey(chatId) && ConfirmationInfo[chatId].Mail == emailOrToken)
                            ConfirmationInfo[chatId].Code.Add(confirmationCode);
                        else
                            ConfirmationInfo[chatId] = new ConfirmationCode(emailOrToken, confirmationCode);
                        UserInfo[chatId].State = State.ConfirmationCode;
                        await SendConfirmationCodeAsync(update, emailOrToken, confirmationCode);

                        Logger.Info($"{prefix}Код подтверждения отправлен на почту сотрудника.");

                        await Telegram.SendMessage(chatId, "На указанную почту отправлен код подтверждения. Напишите его в чат.");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(update, ex);
            }
        }

        /// <summary>
        /// Отправить код подтверждения на почту.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="email">Электронная почта получателя.</param>
        /// <param name="code">Код подтверждения.</param>
        /// <returns></returns>
        public static async Task SendConfirmationCodeAsync(Update update, string email, int code)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Чат бот телеграм", ServiceSettings.Instance.SmtpAddress));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = "Регистрация в чат-боте";
            var text = $"<p>Код для регистрации в чат-боте: {code}</p>";
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = text
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(ServiceSettings.Instance.SmtpHost, ServiceSettings.Instance.SmtpPort, Enum.Parse<MailKit.Security.SecureSocketOptions>(ServiceSettings.Instance.SmtpSecure));
                if (!string.IsNullOrEmpty(ServiceSettings.Instance.SmtpLogin))
                    await client.AuthenticateAsync(ServiceSettings.Instance.SmtpLogin, ServiceSettings.Instance.SmtpPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }

        /// <summary>
        /// Показ главного меню.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="hideMarkup">Признак необходимости скрытия кнопок клавиатуры.</param>
        /// <returns></returns>
        public static async Task ShowMainMenu(Update update, bool hideMarkup)
        {
            var chatId = CommonFunctions.GetChatId(update);
            UserInfo[chatId] = new UserInfo(null, State.MainMenu);

            var identified = await IdentificateUser(update);
            if (!identified)
                return;

            var replyMarkup = new InlineKeyboardMarkup(new[]
            {
               new []
               {
                   InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.MainMenuActions.FindDocument),
                   InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.MainMenuActions.FindCounterparty),
               },
               new []
               {
                   InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.MainMenuActions.SendRequest),
               },
            });

            if (hideMarkup)
                await CommonFunctions.RemoveKeyboard(update);

            if (!UserInfo.ContainsKey(chatId))
                UserInfo[chatId] = new UserInfo();

            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Добрый день. Какой у Вас запрос?", replyMarkup: replyMarkup);
            return;
        }

        /// <summary>
        /// Проверить введенный код регистрации на корректность.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="message">Сообщение.</param>
        /// <param name="chatId">ИД чата.</param>
        public static async void CheckConfirmationCode(Update update, Message message, long chatId)
        {
            var email = ConfirmationInfo[chatId].Mail;
            var prefix = $"CheckConfirmationCode. {email}";

            await CommonFunctions.RemoveInline(update);
            var codeText = message?.Text?.Trim();

            if (ConfirmationInfo.ContainsKey(chatId))
            {
                int.TryParse(codeText, out int code);
                if (code == 0 || !ConfirmationInfo[chatId].Code.Contains(code))
                {
                    Logger.Warn($"{prefix}Введен неверный код.");

                    InlineKeyboardMarkup replyMarkup = new InlineKeyboardMarkup(new[]
                    {
                    new []
                        {
                           InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.Registration.EnterEmainAgain),
                           InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.Registration.SendNewCode),
                        },
                    });
                    await Telegram.SendMessage(chatId: chatId, replyMarkup: replyMarkup, text: "Неверный код. Введите код подверждения заново или выберите действие.");
                }
                else if (ConfirmationInfo[chatId].Code.Contains(code))
                {
                    Logger.Warn($"{prefix}Введен верный код подтверждения.");

                    var employee = await IntegrationDirectumRX.GetEmployeeByMail(email);
                    if (employee != null)
                    {
                        var success = await IntegrationDirectumRX.CreateBotUser(email, chatId, CommonFunctions.GetUserName(update), CommonFunctions.GetChatId(update));
                        if (success)
                        {
                            if (!UserInfo.ContainsKey(chatId))
                                UserInfo[chatId] = new UserInfo();

                            Logger.Info($"{prefix}Пользователь успешно зарегистрирован в чат-боте.");

                            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId: chatId, text: $"{employee[Constants.EntityProperties.Name]}, Вы успешно зарегистрировались в чат-боте!");
                            ConfirmationInfo.Remove(chatId);
                            await ShowMainMenu(update, true);
                        }
                    }
                    else
                    {
                        if (!UserInfo.ContainsKey(chatId))
                            UserInfo[chatId] = new UserInfo();

                        Logger.Info($"{prefix}Пользователь с данной почтой не найден в Directum RX.");

                        UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId: chatId, text: $"Пользователь с данной почтой не найден в Directum RX");
                        UserInfo[chatId].State = State.Start;
                    }
                }
            }
        }

        /// <summary>
        /// Получить кнопку чата для возврата в главное меню + дополнительная кнопка.
        /// </summary>
        /// <param name="buttonText">Текст дополнительной кнопки.</param>
        /// <returns>Кнопка чата для возврата в главное меню + дополнительная кнопка.</returns>
        public static InlineKeyboardMarkup GetBackToMenuMarkup(string buttonText)
        {
            if (buttonText == null)
            {
                return new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.MainMenu)
                    },
                });
            }
            else
                return new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(buttonText),
                        InlineKeyboardButton.WithCallbackData(Constants.NavigationConstants.MainMenu)
                    },
                });
        }

        /// <summary>
        /// Получить кнопку чата для возврата в главное меню.
        /// </summary>
        /// <returns>Кнопка чата для возврата в главное меню.</returns>
        public static InlineKeyboardMarkup GetBackToMenuMarkup() => GetBackToMenuMarkup(null);

        /// <summary>
        /// Вывод информации о контрагенте по ИД.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="counterpartyId">ИД контрагента.</param>
        public static async void ShowCounterpartyInfo(Update update, long? counterpartyId)
        {
            var chatId = CommonFunctions.GetChatId(update);

            if (counterpartyId.HasValue)
            {
                var counterparty = await IntegrationDirectumRX.GetCounterpartyById(counterpartyId.Value);
                string info = CommonFunctions.CheckString(counterparty["LegalName"]) ? $"<b>Юрид. наименование:</b> {counterparty["LegalName"]}{Environment.NewLine}" : string.Empty;
                info += counterparty["HeadCompany"] != null ? $"<b>Головная орг.:</b> {counterparty["HeadCompany"]["Name"]}{Environment.NewLine}" : string.Empty;
                info += counterparty["Nonresident"] != null ? $"<b>Нерезидент:</b> {(counterparty["Nonresident"] == true ? "Да" : "Нет")}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["TIN"]) ? $"<b>ИНН:</b> {counterparty["TIN"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["TRRC"]) ? $"<b>КПП:</b> {counterparty["TRRC"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["PSRN"]) ? $"<b>ОГРН:</b> {counterparty["PSRN"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["NCEO"]) ? $"<b>ОКПО:</b> {counterparty["NCEO"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["NCEA"]) ? $"<b>ОКВЭД:</b> {counterparty["NCEA"]}{Environment.NewLine}" : string.Empty;
                info += counterparty["City"] != null ? $"<b>Населенный пункт:</b> {counterparty["City"]["Name"]}{Environment.NewLine}" : string.Empty;
                info += counterparty["Region"] != null ? $"<b>Регион:</b> {counterparty["Region"]["Name"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["LegalAddress"]) ? $"<b>Юридический адрес (RU):</b> {counterparty["LegalAddress"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["PostalAddress"]) ? $"<b>Почтовый адрес:</b> {counterparty["PostalAddress"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["Phones"]) ? $"<b>Телефоны:</b> {counterparty["Phones"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["Email"]) ? $"<b>Эл. почта:</b> {counterparty["Email"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["Homepage"]) ? $"<b>Сайт:</b> {counterparty["Homepage"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["Account"]) ? $"<b>Номер счета:</b> {counterparty["Account"]}{Environment.NewLine}" : string.Empty;
                info += counterparty["Bank"] != null ? $"<b>Банк:</b> {counterparty["Bank"]["Name"]}{Environment.NewLine}" : string.Empty;
                info += CommonFunctions.CheckString(counterparty["Note"]) ? $"<b>Примечание:</b> {counterparty["Note"]}{Environment.NewLine}" : string.Empty;
                UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, info, parseMode: ParseMode.Html, replyMarkup: GetBackToMenuMarkup(Constants.NavigationConstants.SearchAgain));
            }
            else
                UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Не удалось получить информацию о контрагенте", replyMarkup: GetBackToMenuMarkup(Constants.NavigationConstants.SearchAgain));
        }

        /// <summary>
        /// Обработать перемещение вперед-назад по списку сущностей.
        /// </summary>
        /// <param name="messageText">Тект сообщения от пользователя.</param>
        /// <param name="chatId">ИД чата.</param>
        public static async void HandleListNavigation(string messageText, long chatId)
        {
            if (messageText == BotWorkerService.Constants.NavigationConstants.Next)
                UserInfo[chatId].Page++;
            else
                UserInfo[chatId].Page--;

            var replyMessageText = "Выберите";
            switch (UserInfo[chatId].State)
            {
                case State.ChoosingCounterparty:
                    replyMessageText = "Выберите контрагента";
                    break;
                case State.ChoosingDocumentType:
                    replyMessageText = "Выберите тип документа";
                    break;
                case State.ChoosingDocument:
                    replyMessageText = "Выберите документ";
                    break;
            }

            UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, replyMessageText, replyMarkup: CommonFunctions.GetMarkupFromEntities(chatId));
        }

        /// <summary>
        /// Отправить документ в чат с пользователем.
        /// </summary>
        /// <param name="documentId">ИД документа.</param>
        /// <param name="chatId">ИД чата.</param>
        public static async void ShowDocument(Update update, long documentId, long chatId)
        {
            await CommonFunctions.RemoveKeyboard(update);
            var docBody = await IntegrationDirectumRX.GetDocumentBodyById(update, documentId);
            if (docBody != null)
            {
                var extension = docBody["Extension"];
                var filename = $"{(docBody["Name"].Length < 60 ? docBody["Name"] : docBody["Name"].Substring(0, 60))}.{extension}".Replace("\"", "");
                using (MemoryStream memoryStream = new MemoryStream(docBody["VersionBody"]))
                {
                    UserInfo[chatId].LastMessage = await Telegram.SendDocument(
                        chatId,
                        new InputFileStream(memoryStream, fileName: filename),
                        replyMarkup: GetBackToMenuMarkup(Constants.NavigationConstants.Previous));
                }
                UserInfo[chatId].State = State.ShownDocument;
            }
        }

        /// <summary>
        /// Создание заявки в Directum RX.
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async void CreateRequest(Update update)
        {
            try
            {
                var message = update.Message;
                var chatId = CommonFunctions.GetChatId(update);

                var filesJson = await CommonFunctions.GetFilesInfo(UserInfo[chatId].Attachments);
                var requestText = UserInfo[chatId].RequestText;
                await IntegrationDirectumRX.SendRequest(update, requestText, filesJson);
                UserInfo[chatId].Attachments = new List<AttachmentInfo>();
                UserInfo[chatId].RequestText = null;

                UserInfo[chatId].LastMessage = await Telegram.SendMessage(chatId, "Заявка успешно отправлена в работу ответственному за заявки");
                ShowMainMenu(update, false);
            }
            catch (Exception ex)
            {
                HandleError(update, ex);
            }
        }
    }
}
