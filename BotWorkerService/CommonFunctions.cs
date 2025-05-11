using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using BotWorkerService;
using Telegram.Bot;
using TelegramBotService;

namespace TelegramBot
{
    public class CommonFunctions
    {
        /// <summary>
        /// Получить Id чата с текущим пользователем в telegram.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <returns>Id чата с текущим пользователем. Возвращает 0, если update равен null или тип обновления не поддерживается.</returns>
        public static long GetChatId(Update update)
        {
            if (update != null)
            {
                switch (update.Type)
                {
                    case UpdateType.Message: return update.Message.From.Id;
                    case UpdateType.CallbackQuery: return update.CallbackQuery.From.Id;
                    case UpdateType.InlineQuery: return update.InlineQuery.From.Id;
                    case UpdateType.EditedMessage: return update.EditedMessage.From.Id;
                    case UpdateType.PollAnswer: return update.PollAnswer.User.Id;
                    case UpdateType.ChosenInlineResult: return update.ChosenInlineResult.From.Id;
                    case UpdateType.ChatMember: return update.ChatMember.NewChatMember.User.Id;
                    case UpdateType.Unknown: return update.MyChatMember.Chat.Id;
                    default: return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// Получить логин текущего пользователя в telegram.
        /// </summary>
        /// <param name="update">Обновление.</param>
        /// <returns>Логин текущего пользователя в telegram. Возвращает пустую строку, если update равен null или тип обновления не поддерживается.</returns>
        public static string GetUserName(Update update)
        {
            if (update != null)
            {

                switch (update.Type)
                {
                    case UpdateType.Message: return update.Message.From.Username;
                    case UpdateType.CallbackQuery: return update.CallbackQuery.From.Username;
                    case UpdateType.InlineQuery: return update.InlineQuery.From.Username;
                    case UpdateType.EditedMessage: return update.EditedMessage.From.Username;
                    case UpdateType.PollAnswer: return update.PollAnswer.User.Username;
                    case UpdateType.ChosenInlineResult: return update.ChosenInlineResult.From.Username;
                    case UpdateType.ChatMember: return update.ChatMember.NewChatMember.User.Username;
                    case UpdateType.Unknown: return update.MyChatMember.Chat.Username;
                    default: return string.Empty;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Получить идентификатор и наименование вложения из сообщения.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <returns>Структура с информацией о вложении.</returns>
        public static AttachmentInfo GetAttachmentInfoFromMessage(Message message)
        {
            var attachmentInfo = new AttachmentInfo();
            if (message.Document != null)
            {
                attachmentInfo.Id = message.Document.FileId;
                attachmentInfo.Name = message.Document.FileName;
            }
            if (message.Video != null)
            {
                attachmentInfo.Id = message.Video.FileId;
                attachmentInfo.Name = message.Video.FileName;
            }
            if (message.Audio != null)
            {
                attachmentInfo.Id = message.Audio.FileId;
                attachmentInfo.Name = message.Audio.FileName;
            }
            if (message.Photo != null)
                attachmentInfo.Id = message.Photo.LastOrDefault()?.FileId;
            if (message.VideoNote != null)
                attachmentInfo.Id = message.VideoNote.FileId;
            if (message.Voice != null)
                attachmentInfo.Id = message.Voice.FileId;

            return attachmentInfo;
        }

        /// <summary>
        /// Получает информацию о файлах в формате JSON, скачивая их из Telegram.
        /// </summary>
        /// <param name="attachmentInfos">Список информации о вложениях (ID и имя файла).</param>
        /// <returns>JSON-строка с массивом объектов FileInfo (содержит base64-данные и имя файла).</returns>
        /// <remarks>
        /// Для файлов без имени используется имя из filePath.
        /// </remarks>
        /// <example>
        /// Возвращает строку вида: [{"Data":"base64string","Name":"filename"}]
        /// </example>
        public static async Task<string> GetFilesInfo(List<AttachmentInfo> attachmentInfos)
        {
            var fileInfos = new List<TelegramBotService.FileInfo>();
            foreach (var attachmentInfo in attachmentInfos)
            {
                if (attachmentInfo.Id != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        var file = await BotWrapper.Telegram.GetInfoAndDownloadFile(attachmentInfo.Id, stream);
                        if (attachmentInfo.Name == null)
                            attachmentInfo.Name = file.FilePath?.Split('/')?.LastOrDefault();
                        fileInfos.Add(new TelegramBotService.FileInfo(Convert.ToBase64String(stream.ToArray()), attachmentInfo.Name));
                    }
                }
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(fileInfos);
        }

        /// <summary>
        /// Скрыть конпки в клавиатуре. Функция тправляет и сразу же удаляет служебное сообщение в чат.
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async Task RemoveKeyboard(Update update)
        {
            var chatId = GetChatId(update);
            var message = await BotWrapper.Telegram.SendMessage(chatId: chatId, text: "ㅤ", replyMarkup: new ReplyKeyboardRemove(), disableNotification: true);
            await BotWrapper.Telegram.DeleteMessage(chatId, message.MessageId);
        }

        /// <summary>
        /// Скрыть кнопки в чате. Метод редактирует последнее отправленное сообщение от бота, если в нем были кнопки клавиатуры.
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async Task RemoveInline(Update update)
        {
            try
            {
                var chatId = GetChatId(update);
                if (BotWrapper.UserInfo.ContainsKey(chatId) && BotWrapper.UserInfo[chatId].LastMessage != null && BotWrapper.UserInfo[chatId].LastMessage.ReplyMarkup != null)
                    BotWrapper.UserInfo[chatId].LastMessage = await BotWrapper.Telegram.EditMessageReplyMarkup(chatId, BotWrapper.UserInfo[chatId].LastMessage.MessageId);
            }
            catch
            { }
        }

        /// <summary>
        /// Проверка на то, что строка не null и не пустая.
        /// </summary>
        /// <param name="text">Строка (object, т.к. проверяются строки, полученные через odata).</param>
        /// <returns>Если строка не null и не пустая, то возвращается true. Иначе - false.</returns>
        public static bool CheckString(object text) => !string.IsNullOrEmpty(text?.ToString());

        /// <summary>
        /// Получить кнопки клавиатуры для вывода списка сущностей.
        /// </summary>
        /// <param name="chatId">ИД чата.</param>
        /// <returns>Кнопки клавиатуры для вывода списка сущностей.</returns>
        public static ReplyKeyboardMarkup GetMarkupFromEntities(long chatId)
        {
            List<KeyboardButton[]> buttons = new List<KeyboardButton[]>();
            var backOrForward = new KeyboardButton[] { };
            if (BotWrapper.UserInfo.ContainsKey(chatId))
            {
                var entities = BotWrapper.UserInfo[chatId].Entities;
                var allbuttons = GetButtonsFromEntitesList(entities);

                buttons = allbuttons.Skip(BotWrapper.UserInfo[chatId].Page * BotWrapper.EntitiesOnPage)
                    .Take(BotWrapper.EntitiesOnPage).ToList();

                if (allbuttons.Count > BotWrapper.EntitiesOnPage)
                {
                    if (BotWrapper.UserInfo[chatId].Page == 0)
                    {
                        backOrForward = new KeyboardButton[] { Constants.NavigationConstants.Next };
                    }
                    else
                    {
                        if ((BotWrapper.UserInfo[chatId].Page + 1) * BotWrapper.EntitiesOnPage >= allbuttons.Count)
                            backOrForward = new KeyboardButton[] { Constants.NavigationConstants.Previous };
                        else
                            backOrForward = new KeyboardButton[] { Constants.NavigationConstants.Previous, Constants.NavigationConstants.Next };
                    }
                }
            }
            buttons.AddRange(
            new[]
            {
                backOrForward,
                new KeyboardButton[] { Constants.NavigationConstants.SearchAgain, Constants.NavigationConstants.MainMenu }
            });
            ReplyKeyboardMarkup replyMarkup = new ReplyKeyboardMarkup(buttons.ToArray());
            replyMarkup.ResizeKeyboard = true;
            replyMarkup.OneTimeKeyboard = false;
            return replyMarkup;
        }

        /// <summary>
        /// Получить список сущностей из информации, полученной от сервиса интеграции Directum RX.
        /// </summary>
        /// <param name="infos">Информация, полученная от сервиса интеграции Directum RX.</param>
        /// <returns>Список сущностей.</returns>
        public static List<EntityInfo> GetEntitiesListFromDynamic(dynamic infos)
        {
            var entities = new List<EntityInfo>();
            if (infos != null)
            {
                foreach (var info in infos)
                {
                    var entity = new EntityInfo();
                    if (info[Constants.EntityProperties.Name] == null)
                        continue;
                    entity.Name = info[Constants.EntityProperties.Name].Trim();
                    //Имя сущности обрезается до 127 символов, т.к. если не обрезать явно, то telegram автоматически это делает, и добавляет в конце строки символ '…', из-за чего могут впоследствии возникать проблемы при сопоставлении наименований.
                    if (entity.Name.Length > Constants.NavigationConstants.MaxNameLengthInButton)
                        entity.Name = entity.Name.Substring(0, Constants.NavigationConstants.MaxNameLengthInButton);
                    entity.Id = info[Constants.EntityProperties.Id];
                    entities.Add(entity);
                }
            }
            return entities;
        }

        /// <summary>
        /// Получить список массивов кнопок клавиатуры из списка сущностей. Метод разбивает кнопки по длине текста, содержащегося в кнопках.
        /// </summary>
        /// <param name="entities">Список сущностей.</param>
        /// <returns>Список массивов кнопок клавиатуры.</returns>
        static List<KeyboardButton[]> GetButtonsFromEntitesList(List<EntityInfo> entities)
        {
            var buttons = new List<KeyboardButton[]>();
            var row = new List<KeyboardButton>();
            int currentLength = 0;

            foreach (var entity in entities)
            {
                if (currentLength + entity.Name.Length > BotWrapper.MaxTotalLengthInMarkup && row.Any())
                {
                    buttons.Add(row.ToArray());
                    row = new List<KeyboardButton>();
                    currentLength = 0;
                }

                row.Add(entity.Name);
                currentLength += entity.Name.Length;
            }

            if (row.Any())
                buttons.Add(row.ToArray());

            return buttons;
        }
    }
}
