namespace TelegramBotService
{
    /// <summary>
    /// Перечисление возможных состояний, в которых может находиться чат-бот при взаимодействии с пользователем.
    /// </summary>
    public enum State 
    {
        Registration,
        ConfirmationCode,
        Start,
        MainMenu,

        // Поиск информации о контрагенте.
        EnteringCounterpartyName,
        ChoosingCounterparty,
        ShownCounterparty,

        // Поиск документа.
        ChoosingDocumentType,
        EnteringDocumentName,
        ChoosingDocument,
        ShownDocument,

        // Формирование заявки.
        EnteringRequestText
    }
}
