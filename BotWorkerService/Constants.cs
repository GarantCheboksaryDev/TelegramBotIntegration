namespace BotWorkerService
{
    public class Constants
    {
        /// <summary>
        /// Сообщение для запуска чат-бота.
        /// </summary>
        public const string StartMessage = "/start";
        /// <summary>
        /// Период проверки авторизации пользователя в Directum RX в секундах.
        /// </summary>
        public const int CheckingPeriod = 100;
        /// <summary>
        /// Наименования атрибутов справочника "Пользователи чат-бота".
        /// </summary>
        public class BotUserProperties
        {
            /// <summary>
            /// Статус - Active/Closed.
            /// </summary>
            public const string Status = "Status";
            /// <summary>
            /// Идентификатор чата в Telegram.
            /// </summary>
            public const string ChatId = "ChatId";
        }

        /// <summary>
        /// Возможные значения атрибута Статус.
        /// </summary>
        public class StatusItems
        {
            /// <summary>
            /// Статус Действующий.
            /// </summary>
            public const string Active = "Active";
            /// <summary>
            /// Статус Закрыт.
            /// </summary>
            public const string Closed = "Closed";
        }

        /// <summary>
        /// Наименования атрибутов сущностей.
        /// </summary>
        public class EntityProperties 
        {
            /// <summary>
            /// Идентификатор сущности.
            /// </summary>
            public const string Id = "Id";
            /// <summary>
            /// Наименование сущности.
            /// </summary>
            public const string Name = "Name";
            /// <summary>
            /// Ошибка при получении сущностей.
            /// </summary>
            public const string Error = "Error";
            /// <summary>
            /// Найденные сущности.
            /// </summary>
            public const string EntityInfos = "EntityInfos";
        }

        /// <summary>
        /// Константы, используемые в действиях для навигации.
        /// </summary>
        public class NavigationConstants
        {
            /// <summary>
            /// Текст кнопки для перехода вперед по списку записей.
            /// </summary>
            public const string Next = "Далее";
            /// <summary>
            /// Текст кнопки для перехода назад по списку записей.
            /// </summary>
            public const string Previous = "Назад";
            /// <summary>
            /// Текст кнопки для перехода в главнео меню.
            /// </summary>
            public const string MainMenu = "Главное меню";
            /// <summary>
            /// Текст кнопки для повтора поиска записей.
            /// </summary>
            public const string SearchAgain = "Повторить поиск";
            /// <summary>
            /// Количество сущностей, одновременно отображаемых при выводе большого списка сущностей.
            /// </summary>
            public const int EntitiesOnPage = 4;
            /// <summary>
            /// Максимальная суммарная длина строк для вывода в один ряд в кнопках клавиатуры.
            /// </summary>
            public const int MaxTotalLengthInMarkup = 50;
            /// <summary>
            /// Действия главного меню.
            /// </summary>
            public class MainMenuActions
            {
                /// <summary>
                /// Текст кнопки для действия "Найти контрагента".
                /// </summary>
                public const string FindCounterparty = "Найти контрагента";
                /// <summary>
                /// Текст кнопки для действия "Найти документ".
                /// </summary>
                public const string FindDocument = "Найти документ";
                /// <summary>
                /// Текст кнопки для действия "Сформировать заявку".
                /// </summary>
                public const string SendRequest = "Сформировать заявку";
            }

            /// <summary>
            /// Действия при регистрации.
            /// </summary>
            public class Registration
            {
                /// <summary>
                /// Текст кнопки для повтора ввода электронной почты при регистрации.
                /// </summary>
                public const string EnterEmainAgain = "Ввести почту заново";
                /// <summary>
                /// Текст кнопки повторной отправки кода на почту при регистрации.
                /// </summary>
                public const string SendNewCode = "Отправить новый код";
            }
        }

        /// <summary>
        /// Константы, используемые для интеграции с Directum RX.
        /// </summary>
        public class Integration 
        {
            /// <summary>
            /// Наименование модуля Directum RX, в котором находятся функции интеграции.
            /// </summary>
            public const string ModuleName = "TelegramBot";

            /// <summary>
            /// Наименования функций интеграции.
            /// </summary>
            public class Functions
            {
                /// <summary>
                /// Наименование функции для создания и отправки в работу заявки.
                /// </summary>
                public const string CreateRequest = "CreateRequest";

                /// <summary>
                /// Наименование функции для создания записи справочника "Пользователи чат-бота".
                /// </summary>
                public const string CreateBotUser = "CreateBotUser";

                /// <summary>
                /// Наименование функции для записи ИД чата бота с пользователем в справочнике "Пользователи чат-бота".
                /// </summary>
                public const string SetChatId = "SetChatId";

                /// <summary>
                /// Наименование функции для получения информации о контрагентах.
                /// </summary>
                public const string GetCounterparties = "GetCounterparties";

                /// <summary>
                /// Наименование функции для получения типов документов.
                /// </summary>
                public const string GetDocumentTypes = "GetDocumentTypes";

                /// <summary>
                /// Наименование функции для получения информации о документах.
                /// </summary>
                public const string GetDocuments = "GetDocuments";
                /// <summary>
                /// Наименование функции для получения информации о версии документа.
                /// </summary>
                public const string GetDocumentVersion = "GetDocumentVersion";
            }
        }
    }
}
