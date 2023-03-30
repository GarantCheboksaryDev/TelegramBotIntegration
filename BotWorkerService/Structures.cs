using Telegram.Bot.Types;

namespace TelegramBotService
{
    /// <summary>
    /// Класс для хранения информации о кодах подтверждения.
    /// </summary>
    internal class ConfirmationCode
    {
        public string? Mail { get; set; }
        public List<int> Code { get; set; } = new List<int>();
        public ConfirmationCode() { }
        public ConfirmationCode(string mail, int code)
        {
            Mail = mail;
            Code.Add(code);
        }

    }

    /// <summary>
    /// Класс для хранения информация о пользователе чат-бота.
    /// </summary>
    public class UserInfo
    {
        private int _page;
        /// <summary>
        /// Последнее сообщение, отправленное ботом данному пользователю.
        /// </summary>
        public Message? LastMessage { get; set; }

        /// <summary>
        /// Этап, на котором неаходится пользователь в общении с чат-ботом.
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// Список сущностей, полученных из Directum RX.
        /// </summary>
        public List<EntityInfo> Entities { get; set; }

        /// <summary>
        /// ИД выбранного типа документов.
        /// </summary>
        public long? DocumentTypeId { get; set; }

        /// <summary>
        /// Дата и время последней проверки пользователя на авторизацию в Directum RX.
        /// </summary>
        public DateTime? LastUserCheck { get; set; }

        /// <summary>
        /// Страница, на которой находится пользователь, при выборе из большого списка записей.
        /// </summary>
        public int Page
        {
            get => _page;
            set
            {
                if (value >= 0)
                    _page = value;
            }
        }

        public UserInfo() { }

        public UserInfo(Message message, State state)
        {
            LastMessage = message;
            State = state;
        }
    }

    /// <summary>
    /// Структура для получения информации о сущностях из Directum RX.
    /// </summary>
    public class EntityInfo
    {
        public string Name { get; set; }
        public long Id { get; set; }

        public EntityInfo() { }

        public EntityInfo(string name, long id) 
        {
            Name = name;
            Id = id;
        }
    }
}
