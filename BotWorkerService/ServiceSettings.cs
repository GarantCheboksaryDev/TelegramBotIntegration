using ConfigSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomJobHandler
{
    public class ServiceSettings
    {
        /// <summary>
        /// Получатель настроек.
        /// </summary>
        protected ConfigSettingsGetter ConfigSettings { get; }

        /// <summary>
        /// Инстанс синглтона.
        /// </summary>
        private static ServiceSettings _instance;
        public static ServiceSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ServiceSettings();
                return _instance;
            }
        }
        /// <summary>
        /// Api-токен чат-бота.
        /// </summary>
        public virtual string Token => this.ConfigSettings.Get("TELEGRAM_BOT_API_KEY", string.Empty);
        /// <summary>
        /// Адрес сервиса интеграции Directum RX.
        /// </summary>
        public virtual string IntegrationServiceUrl => this.ConfigSettings.Get("INTEGRATION_SERVICE_URL", string.Empty);
        /// <summary>
        /// Логин для аутентификации в Directum RX.
        /// </summary>
        public virtual string Login => this.ConfigSettings.Get("INTEGRATION_USER_LOGIN", string.Empty);
        /// <summary>
        /// Пароль для аутентификации в Directum RX.
        /// </summary>
        public virtual string Password => this.ConfigSettings.Get("INTEGRATION_USER_PASSWORD", string.Empty);
        /// <summary>
        /// E-mail адрес для отправки кода подтверждения при регистрации.
        /// </summary>
        public virtual string SmtpAddress => this.ConfigSettings.Get("SMTP_ADDRESS", string.Empty);
        /// <summary>
        /// Логин ящика электронной почты.
        /// </summary>
        public virtual string SmtpLogin => this.ConfigSettings.Get("SMTP_LOGIN", string.Empty);
        /// <summary>
        /// Пароль от ящика электронной почты.
        /// </summary>
        public virtual string SmtpPassword => this.ConfigSettings.Get("SMTP_PASSWORD", string.Empty);
        /// <summary>
        /// Адрес сервера электронной почты.
        /// </summary>
        public virtual string SmtpHost => this.ConfigSettings.Get("SMTP_HOST", string.Empty);
        /// <summary>
        /// Порт сервера электронной почты.
        /// </summary>
        public virtual int SmtpPort => this.ConfigSettings.Get("SMTP_PORT", 0);
        /// <summary>
        /// Способ защиты соединения при работе с сервером электронной почты..
        /// </summary>
        public virtual string SmtpSecure => this.ConfigSettings.Get("SMTP_SECURE", string.Empty);
        /// <summary>
        /// Способ защиты соединения при работе с сервером электронной почты..
        /// </summary>
        public virtual int EntitiesOnPage => this.ConfigSettings.Get("ENTITIES_ON_PAGE", 4);
        /// <summary>
        /// Способ защиты соединения при работе с сервером электронной почты..
        /// </summary>
        public virtual int MaxTotalLengthInMarkup => this.ConfigSettings.Get("MAX_TOTAL_LENGTH_IN_MARKUP", 50);
        /// <summary>
        /// Способ защиты соединения при работе с сервером электронной почты..
        /// </summary>
        public virtual int UserCheckInterval => this.ConfigSettings.Get("USER_CHECK_INTERVAL", 60);
        private ServiceSettings()
        {
            this.ConfigSettings = new ConfigSettingsGetter();
        }
    }
}
