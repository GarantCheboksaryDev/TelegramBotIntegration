using BotWorkerService;
using Simple.OData.Client;
using System.Text;
using System.Xml.Linq;
using Telegram.Bot.Types;
using TelegramBotService;
using Windows.System;

namespace TelegramBot
{
    public static class IntegrationDirectumRX
    {
        /// <summary>
        /// Авторизация через OData в Directum RX.
        /// </summary>
        /// <param name="integrationServiceUrl">Адрес сервиса интеграции.</param>
        /// <param name="login">Лоигин пользователя сервиса интеграции.</param>
        /// <param name="password">Пароль пользователя сервиса  интеграции.</param>
        /// <returns>Экземпляр клиента Odata.</returns>
        public static ODataClient AuthenticationOdataCLient(string integrationServiceUrl, string login, string password)
        {
            var prefix = $"AuthenticationOdataCLient. ";

            try
            {
                var odataClientSettings = new ODataClientSettings(new Uri(integrationServiceUrl));
                odataClientSettings.BeforeRequest += (HttpRequestMessage message) =>
                {
                    var authenticationHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{password}"));
                    message.Headers.Add("Authorization", "Basic " + authenticationHeaderValue);
                };
                var odataClient = new ODataClient(odataClientSettings);
                BotWrapper.Logger.Info($"{prefix}Авторизация в Directum RX прошла успешно.");
                return odataClient;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при авторизации в Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить пользователя чат-бота из Directum RX
        /// </summary>
        /// <param name="userId">ИД пользователя в telegram.</param>
        /// <returns>Пользователь Directum RX.</returns>
        public static async Task<dynamic> GetTelegramUser(long userId)
        {
            var prefix = $"GetTelegramUser. Id: {userId}. ";

            try
            {
                var x = ODataDynamic.Expression;
                IEnumerable<dynamic> telegramUser = await BotWrapper.OdataClient
                    .For(x.ITelegramBotBotUsers)
                    .Expand(x.Employee, x.Employee.Login)
                    .Filter(x.UserId == userId.ToString())
                    .Select(x.Id, x.Username, x.UserId, x.ChatId, x.Employee.Id, x.Employee.Status, x.Employee.Login.Status, x.Status)
                    .FindEntriesAsync();

                if (telegramUser != null && telegramUser.Any())
                {
                    BotWrapper.Logger.Info($"{prefix}Пользователь чат-бота успешно получен из Directum RX.");
                    return telegramUser?.FirstOrDefault();
                }
                else
                {
                    BotWrapper.Logger.Warn($"{prefix}В Directum RX не найден пользователь чат-бота по Id пользователя телеграм.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении пользователя чат-бота из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Найти активного сотрудника в Directum RX по адресу эл. почты.
        /// </summary>
        /// <param name="mail">Электронная почта сотрудника.</param>
        /// <returns>Структура, содержащая Id и имя сотрудника.</returns>
        public static async Task<dynamic> GetEmployeeByMail(string mail)
        {
            var prefix = $"GetEmployeeByMail. Email: {mail}. ";

            try
            {
                var x = ODataDynamic.Expression;
                IEnumerable<dynamic> employee = await BotWrapper.OdataClient
                    .For(x.IEmployees)
                    .Expand(x.Login)
                    .Filter(x.Email.ToLower() == mail.ToLower() && x.Status == Constants.StatusItems.Active && x.Login.Status == Constants.StatusItems.Active)
                    .Select(x.Id, x.Name)
                    .FindEntriesAsync();
                if (employee != null && employee.Any())
                {
                    BotWrapper.Logger.Info($"{prefix}Сотрудник успешно получен из Directum RX.");
                    return employee?.FirstOrDefault();
                }
                else
                {
                    BotWrapper.Logger.Warn($"{prefix}В Directum RX не найден действующий сотрудник по адресу электронной почты.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении сотрудника по адресу электронной почты из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Создать запись справочника "Пользователи чат-бота".
        /// </summary>
        /// <param name="mail">Электронная почта сотрудника. По электронной почте происходит поиск сотрудника в Directum RX.</param>
        /// <param name="chatId">ИД чата.</param>
        /// <param name="username">Логин пользователя в telegram.</param>
        /// <param name="userId">ИД пользователя в telegram.</param>
        /// <returns>Результат создания пользователя чат-бота. В случае успешного создания возвращается true.</returns>
        public static async Task<bool> CreateBotUser(string mail, long chatId, string username, long userId)
        {
            var prefix = $"CreateBotUser. Email: {mail}. ИД чата: {chatId}. Логин: {username}. ИД пользователя: {userId}. ";

            try
            {
                await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                  .Action(Constants.Integration.Functions.CreateBotUser)
                  .Set(new { mail = mail, chatId = chatId.ToString(), username = username, userId = userId })
                  .ExecuteAsync();

                BotWrapper.Logger.Info($"{prefix}Карточка пользователя чат-бота в Directum RX успешно создана.");

                return true;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при создании карточки пользователя чат-бота в Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Привязка ИД пользователя телеграм и логина в телеграм к карточке пользователя чат-бота по регистрационному токену. 
        /// </summary>
        /// <param name="token">Регистрационный токен.</param>
        /// <param name="chatId">ИД чата.</param>
        /// <param name="username">Логин пользователя в телеграм.</param>
        /// <param name="userId">ИД пользователя в телеграм.</param>
        /// <returns>Результат регистрации пользователя. В случае успеха возвращается пустая строка. В случае возникновения ошибки - текст ошибки.</returns>
        public static async Task<dynamic> RegisterUserByToken(string token, long chatId, string username, long userId)
        {
            var prefix = $"RegisterUserByToken. ИД чата: {chatId}. Логин: {username}. ИД пользователя: {userId}";

            try
            {
                var result = await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                  .Action(Constants.Integration.Functions.RegisterUserByToken)
                  .Set(new { token = token, chatId = chatId.ToString(), username = username, userId = userId })
                  .ExecuteAsSingleAsync();

                return result;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при связывании пользователя телеграм с карточкой пользователя чат-бота в RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Установить Id чата в справочнике "Пользователи чат-бота".
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async void SetChatId(Update update)
        {
            var chatId = CommonFunctions.GetChatId(update);
            var prefix = $"SetChatId. ИД чата: {chatId}. Id: {chatId}. ";
            try
            {
                await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                  .Action(Constants.Integration.Functions.SetChatId)
                  .Set(new { userId = chatId, chatId = chatId.ToString() })
                  .ExecuteAsync();

                BotWrapper.Logger.Info($"{prefix}ИД чата в карточке пользователя чат-бота в Directum RX установлен успешно.");
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при установке ИД чата в карточке пользователя чат-бота в Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Вызов функции интеграции Directum RX для создания и отправки заявки в работу.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="requestText">Текст заявки.</param>
        /// <returns></returns>
        public static async Task<bool> SendRequest(Update update, string requestText, string fileInfos)
        {
            var userId = CommonFunctions.GetChatId(update);
            var prefix = $"SendRequest. Id: {userId}. ";

            try
            {
                await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                    .Action(Constants.Integration.Functions.CreateRequestFromBot)
                    .Set(new { requestText = requestText, userId = userId, fileInfos = fileInfos })
                    .ExecuteAsync();
                BotWrapper.Logger.Info($"{prefix}Заявка успешно отправлена в Directum RX");
                return true;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при отправке заявки в Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить информацию о контрагентах из Directum RX по наименованию контрагента.
        /// </summary>
        /// <param name="counterpartyName">Наименование контрагента.</param>
        /// <returns>Список контрагентов, подходящих по наименованию. Список ограничивается 200 записями.</returns>
        public static async Task<dynamic> GetCounterpartiesByName(string counterpartyName)
        {
            var prefix = $"GetCounterpartiesByName. Наименование контрагента: {counterpartyName}. ";

            try
            {
                var result = await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                    .Function(Constants.Integration.Functions.GetCounterparties)
                    .Set(new { name = counterpartyName })
                    .ExecuteAsArrayAsync();
                BotWrapper.Logger.Info($"{prefix}Спиок найденных по запросу контрагентов успешно получен.");

                return result;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении списка контрагентов из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить контрагента по ИД.
        /// </summary>
        /// <param name="counterpartyId">ИД контрагента.</param>
        /// <returns></returns>
        public static async Task<dynamic> GetCounterpartyById(long counterpartyId)
        {
            var prefix = $"GetCounterpartyById. ИД контрагента: {counterpartyId}. ";

            try
            {
                var x = ODataDynamic.Expression;
                IEnumerable<dynamic> counterparty = await BotWrapper.OdataClient
                    .For(x.ICompanies)
                    .Filter(x.Id == counterpartyId)
                    .Expand(x.HeadCompany, x.Responsible, x.Region, x.City, x.Bank)
                    .FindEntriesAsync();
                if (counterparty != null)
                {
                    BotWrapper.Logger.Info($"{prefix}Успешно получена информация о контрагенте из Directum RX.");
                    return counterparty.FirstOrDefault();
                }
                else
                {
                    BotWrapper.Logger.Warn($"{prefix}Не удалось получить информацию о контрагенте из Directum RX.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении информации о контрагенте из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить типы документов из Directum RX.
        /// </summary>
        /// <returns>Список всех типов документов из справочника "Типы документов".</returns>
        public static async Task<dynamic> GetDocumentTypes()
        {
            var prefix = $"GetDocumentTypes. ";

            try
            {
                var result = await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                    .Function(Constants.Integration.Functions.GetDocumentTypes)
                    .ExecuteAsArrayAsync();
                BotWrapper.Logger.Info($"{prefix}Список типов документов успешно получен из Directum RX.");
                return result;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении списка типов документов из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить документы по наименованию и типу документа из Directum RX.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="name">Наименование документа.</param>
        /// <param name="documentTypeId">ИД типа документа.</param>
        /// <returns>Список найденной информации о документах по заданным критериям и с учетом прав доступа сотрудника.</returns>
        public static async Task<dynamic> GetDocuments(Update update, string name, long documentTypeId)
        {
            var userId = CommonFunctions.GetChatId(update);
            var prefix = $"GetDocuments. Наименование документа: {name}. ИД типа документа: {documentTypeId}. Логин пользователя: {userId}. ";

            try
            {
                var result = await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                    .Function(Constants.Integration.Functions.GetDocuments)
                    .Set(new { documentTypeId = documentTypeId, name = name, userId = userId })
                    .ExecuteAsSingleAsync();
                if (string.IsNullOrEmpty(result[Constants.EntityProperties.Error] as string))
                    BotWrapper.Logger.Info($"{prefix}Найденные документы успешно получены из Directum RX.");
                else
                    BotWrapper.Logger.Warn($"{prefix}Получена ошибка из Diretum RX: {result[Constants.EntityProperties.Error] as string}.");
                return result;
            }
            catch (Exception ex)
            {
                var message = $"{prefix}Произошла ошибка при получении списка документов из Directum RX: {ex}";
                BotWrapper.Logger.Error(message);
                throw new HttpRequestException(message);
            }
        }

        /// <summary>
        /// Получить тело документа и его приложение-обработчик из Directum RX по ИД.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <param name="documentId">ИД документа.</param>
        /// <returns>Тело документа и его приложение-обработчик</returns>
        public static async Task<dynamic> GetDocumentBodyById(Update update, long documentId)
        {
            var prefix = $"GetDocumentBodyById. ИД документа: {documentId}. ";
            var userId = CommonFunctions.GetChatId(update);
            try
            {
                BotWrapper.Logger.Info($"{prefix}Получение версии документа из Directum RX.");

                var x = ODataDynamic.Expression;
                var document = await BotWrapper.OdataClient.For(Constants.Integration.ModuleName)
                    .Function(Constants.Integration.Functions.GetDocumentVersion)
                    .Set(new { documentId = documentId, userId = userId })
                    .ExecuteAsSingleAsync();
                BotWrapper.Logger.Info($"{prefix}Версия документа успешно получена из Directum RX.");
                return document;
            }
            catch (Exception ex)
            {
                BotWrapper.HandleError(update, ex, "Произошла ошибка при получении версии документа из Directum RX");
                return null;
            }
        }
    }
}
