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
        /// <returns>Id чата с текущим пользователем.</returns>
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
        /// <returns>Логин текущего пользователя в telegram.</returns>
        public static string GetUserName(Update update)
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



        /// <summary>
        /// Скрыть конпки в клавиатуре.
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async Task RemoveKeyboard(Update update)
        {
            var chatId = GetChatId(update);
            var message = await BotProgram.Telegram.SendMessage(chatId: chatId, text: "ㅤ", replyMarkup: new ReplyKeyboardRemove(), disableNotification: true);
            await BotProgram.Telegram.DeleteMessage(chatId, message.MessageId);
        }

        /// <summary>
        /// Скрыть кнопки в чате.
        /// </summary>
        /// <param name="update">Обновление.</param>
        public static async Task RemoveInline(Update update)
        {
            try
            {
                var chatId = GetChatId(update);
                if (BotProgram.UserInfo.ContainsKey(chatId) && BotProgram.UserInfo[chatId].LastMessage != null && BotProgram.UserInfo[chatId].LastMessage.ReplyMarkup != null)
                    BotProgram.UserInfo[chatId].LastMessage = await BotProgram.Telegram.EditMessageReplyMarkup(chatId, BotProgram.UserInfo[chatId].LastMessage.MessageId);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Проверка на то, что строка не null и не пустая.
        /// </summary>
        /// <param name="text">Строка (object, т.к. проверяются строки, полученные через odata).</param>
        /// <returns>Если строка не null и не пустая, то возвращается true. Иначе - false.</returns>
        public static bool CheckString(object text) => text != null && !string.IsNullOrEmpty(text.ToString()) ? true : false;

        /// <summary>
        /// Получить кнопки клавиатуры для вывода списка сущностей.
        /// </summary>
        /// <param name="chatId">ИД чата.</param>
        /// <returns>Кнопки клавиатуры для вывода списка сущностей.</returns>
        public static ReplyKeyboardMarkup GetMarkupFromEntities(long chatId)
        {
            List<KeyboardButton[]> buttons = new List<KeyboardButton[]>();
            var backOrForward = new KeyboardButton[] { };
            if (BotProgram.UserInfo.ContainsKey(chatId))
            {
                var entities = BotProgram.UserInfo[chatId].Entities;
                var allbuttons = GetButtonsFromEntitesList(entities);

                buttons = allbuttons.Skip(BotProgram.UserInfo[chatId].Page * Constants.NavigationConstants.EntitiesOnPage)
                    .Take(Constants.NavigationConstants.EntitiesOnPage).ToList();

                if (allbuttons.Count > Constants.NavigationConstants.EntitiesOnPage)
                {
                    if (BotProgram.UserInfo[chatId].Page == 0)
                    {
                        backOrForward = new KeyboardButton[] { Constants.NavigationConstants.Next };
                    }
                    else
                    {
                        if ((BotProgram.UserInfo[chatId].Page + 1) * Constants.NavigationConstants.EntitiesOnPage >= allbuttons.Count)
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
                    if (info["Name"] == null)
                        continue;
                    entity.Name = info["Name"].Trim();
                    if (entity.Name.Length > 127)
                        entity.Name = entity.Name.Substring(0, 127);
                    entity.Id = info["Id"];
                    entities.Add(entity);
                }
            }
            return entities;
        }

        /// <summary>
        /// Получить список массивов кнопок клавиатуры из списка сущностей.
        /// </summary>
        /// <param name="entities">Список сущностей.</param>
        /// <returns>Список массивов кнопок клавиатуры.</returns>
        static List<KeyboardButton[]> GetButtonsFromEntitesList(List<EntityInfo> entities)
        {
            var buttons = new List<KeyboardButton []>();
            var row = new List<KeyboardButton>();
            int currentLength = 0;

            foreach (var entity in entities)
            {
                if (currentLength + entity.Name.Length > Constants.NavigationConstants.MaxTotalLengthInMarkup && row.Any())
                {
                    buttons.Add(row.ToArray());
                    row = new List<KeyboardButton>();
                    currentLength = 0;
                }

                row.Add(entity.Name);
                currentLength += entity.Name.Length;
            }

            if (row.Count > 0)
                buttons.Add(row.ToArray());

            return buttons;
        }
    }
}
