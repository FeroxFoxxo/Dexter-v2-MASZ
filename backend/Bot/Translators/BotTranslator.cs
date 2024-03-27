using Bot.Abstractions;

namespace Bot.Translators;

public class BotTranslator : Translator
{
    public string Action() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Aktion",
            Enums.Language.Fr => "action",
            Enums.Language.Es => "Acción",
            Enums.Language.Ru => "Действие",
            Enums.Language.It => "Azione",
            _ => "Action"
        };

    public string GuildId() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Gilden-ID",
            Enums.Language.Fr => "Veuillez spécifier un identifiant de guilde valide.",
            Enums.Language.Es => "Guild ID",
            Enums.Language.Ru => "Идентификатор гильдии.",
            Enums.Language.It => "ID Gilda",
            _ => "Guild ID"
        };

    public string Author() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Autor",
            Enums.Language.Fr => "Auteur",
            Enums.Language.Es => "Autor",
            Enums.Language.Ru => "Автор",
            Enums.Language.It => "Autore",
            _ => "Author"
        };

    public string Id() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "ID",
            Enums.Language.Fr => "identifiant",
            Enums.Language.Es => "ID",
            Enums.Language.Ru => "Я БЫ",
            Enums.Language.It => "ID",
            _ => "ID"
        };

    public string MessageUrl() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nachrichten-URL",
            Enums.Language.Fr => "URL des messages",
            Enums.Language.Es => "URL del mensaje",
            Enums.Language.Ru => "URL-адрес сообщения",
            Enums.Language.It => "URL del messaggio",
            _ => "Message Url"
        };

    public string User() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nutzer",
            Enums.Language.Fr => "Utilisateur",
            Enums.Language.Es => "Usuario",
            Enums.Language.Ru => "Пользователь",
            Enums.Language.It => "Utente",
            _ => "User"
        };

    public string UserId() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "NutzerId",
            Enums.Language.Fr => "Identifiant d'utilisateur",
            Enums.Language.Es => "ID del Usuario",
            Enums.Language.Ru => "ID пользователя",
            Enums.Language.It => "ID utente",
            _ => "User ID"
        };

    public string Channel() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Kanal",
            Enums.Language.Fr => "Canaliser",
            Enums.Language.Es => "Canal",
            Enums.Language.Ru => "Канал",
            Enums.Language.It => "Canale",
            _ => "Channel"
        };

    public string ChannelId() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "KanalId",
            Enums.Language.Fr => "Identifiant de la chaine",
            Enums.Language.Es => "ID del Canal",
            Enums.Language.Ru => "ChannelId",
            Enums.Language.It => "Canale ID",
            _ => "ChannelId"
        };

    public string NotFound() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nicht gefunden.",
            Enums.Language.Fr => "Pas trouvé.",
            Enums.Language.Es => "No encontrado.",
            Enums.Language.Ru => "Не найден.",
            Enums.Language.It => "Non trovato.",
            _ => "Not found."
        };

    public string MessageContent() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nachrichteninhalt",
            Enums.Language.Fr => "Contenu du message",
            Enums.Language.Es => "Contenido del mensaje",
            Enums.Language.Ru => "Содержание сообщения",
            Enums.Language.It => "Contenuto del messaggio",
            _ => "Message content"
        };

    public string Attachments() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Anhänge",
            Enums.Language.Fr => "Pièces jointes",
            Enums.Language.Es => "Archivos adjuntos",
            Enums.Language.Ru => "Вложения",
            Enums.Language.It => "Allegati",
            _ => "Attachments"
        };

    public string Attachment() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Anhang",
            Enums.Language.Fr => "Attachement",
            Enums.Language.Es => "Adjunto",
            Enums.Language.Ru => "Вложение",
            Enums.Language.It => "allegato",
            _ => "Attachment"
        };

    public string AndXMore(int count) =>
        PreferredLanguage switch
        {
            Enums.Language.De => $"und {count} weitere...",
            Enums.Language.Fr => $"et {count} plus...",
            Enums.Language.Es => $"y {count} más ...",
            Enums.Language.Ru => $"и еще {count} ...",
            Enums.Language.It => $"e {count} altro...",
            _ => $"and {count} more..."
        };

    public string Until() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "bis",
            Enums.Language.Fr => "jusqu'à",
            Enums.Language.Es => "Hasta",
            Enums.Language.Ru => "до",
            Enums.Language.It => "fino a",
            _ => "until"
        };

    public string Description() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Beschreibung",
            Enums.Language.Fr => "La description",
            Enums.Language.Es => "Descripción",
            Enums.Language.Ru => "Описание",
            Enums.Language.It => "Descrizione",
            _ => "Description"
        };

    public string Labels() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Labels",
            Enums.Language.Fr => "Étiquettes",
            Enums.Language.Es => "Etiquetas",
            Enums.Language.Ru => "Этикетки",
            Enums.Language.It => "etichette",
            _ => "Labels"
        };

    public string Filename() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Dateiname",
            Enums.Language.Fr => "Nom de fichier",
            Enums.Language.Es => "Nombre del archivo",
            Enums.Language.Ru => "Имя файла",
            Enums.Language.It => "Nome del file",
            _ => "Filename"
        };

    public string Message() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nachricht",
            Enums.Language.Fr => "Un message",
            Enums.Language.Es => "Mensaje",
            Enums.Language.Ru => "Сообщение",
            Enums.Language.It => "Messaggio",
            _ => "Message"
        };

    public string Type() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Typ",
            Enums.Language.Fr => "Taper",
            Enums.Language.Es => "Escribe",
            Enums.Language.Ru => "Тип",
            Enums.Language.It => "Tipo",
            _ => "Type"
        };

    public string Joined() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Beigetreten",
            Enums.Language.Fr => "Inscrit",
            Enums.Language.Es => "Unido",
            Enums.Language.Ru => "Присоединился",
            Enums.Language.It => "Partecipato",
            _ => "Joined"
        };

    public string Registered() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Registriert",
            Enums.Language.Fr => "Inscrit",
            Enums.Language.Es => "Registrado",
            Enums.Language.Ru => "Зарегистрировано",
            Enums.Language.It => "Registrato",
            _ => "Registered"
        };

    public string OnlyTextChannel() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nur Textkanäle sind erlaubt.",
            Enums.Language.Fr => "Seuls les canaux de texte sont autorisés.",
            Enums.Language.Es => "Solo se permiten canales de texto.",
            Enums.Language.Ru => "Разрешены только текстовые каналы.",
            Enums.Language.It => "Sono consentiti solo canali di testo.",
            _ => "Only text channels are allowed."
        };

    public string OnlyBotChannel() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Nur Botkanäle sind erlaubt,",
            Enums.Language.Fr => "Seuls les canaux de bots sont autorisés,",
            Enums.Language.Es => "Solo se permiten calanes de bots,",
            Enums.Language.Ru => "Разрешены только бот-каналы,",
            Enums.Language.It => "Sono consentiti solo canali bot,",
            _ => "Only bot channels are allowed, such as"
        };

    public string CannotViewOrDeleteInChannel() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Ich darf keine Nachrichten in diesem Kanal sehen oder löschen!",
            Enums.Language.Fr => "Je ne suis pas autorisé à afficher ou supprimer les messages de cette chaîne !",
            Enums.Language.Es => "¡No puedo ver ni borrar mensajes en este canal!",
            Enums.Language.Ru => "Мне не разрешено просматривать или удалять сообщения на этом канале!",
            Enums.Language.It => "Non sono autorizzato a visualizzare o eliminare i messaggi in questo canale!",
            _ => "I'm not allowed to view or delete messages in this channel!"
        };

    public string CannotFindChannel() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Kanal konnte nicht gefunden werden.",
            Enums.Language.Fr => "Impossible de trouver la chaîne.",
            Enums.Language.Es => "No se puede encontrar el canal.",
            Enums.Language.Ru => "Не могу найти канал.",
            Enums.Language.It => "Impossibile trovare il canale.",
            _ => "Cannot find channel."
        };

    public string NoWebhookConfigured() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Dieser Server hat keinen internen Webhook für Benachrichtigungen konfiguriert.",
            Enums.Language.Fr => "Cette guilde n'a pas configuré de webhook pour les notifications internes.",
            Enums.Language.Es => "Este gremio no tiene configurado ningún webhook para notificaciones internas.",
            Enums.Language.Ru => "У этой гильдии нет настроенного веб-перехватчика для внутренних уведомлений.",
            Enums.Language.It => "Questa gilda non ha webhook per le notifiche interne configurate.",
            _ => "This guild has no webhook for internal notifications configured."
        };

    public string SomethingWentWrong() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Etwas ist schief gelaufen.",
            Enums.Language.Fr => "Quelque chose s'est mal passé.",
            Enums.Language.Es => "Algo salió mal.",
            Enums.Language.Ru => "Что-то пошло не так.",
            Enums.Language.It => "Qualcosa è andato storto.",
            _ => "Something went wrong."
        };

    public string Code() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Code",
            Enums.Language.Fr => "Code",
            Enums.Language.Es => "Código",
            Enums.Language.Ru => "Код",
            Enums.Language.It => "Codice",
            _ => "Code"
        };

    public string Language() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Sprache",
            Enums.Language.Fr => "Langue",
            Enums.Language.Es => "Idioma",
            Enums.Language.Ru => "Язык",
            Enums.Language.It => "Lingua",
            _ => "Language"
        };

    public string Timestamps() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Zeitstempel",
            Enums.Language.Fr => "Horodatage",
            Enums.Language.Es => "Marcas de tiempo",
            Enums.Language.Ru => "Отметки времени",
            Enums.Language.It => "Timestamp",
            _ => "Timestamps"
        };

    public string Support() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Support",
            Enums.Language.Fr => "Soutien",
            Enums.Language.Es => "Apoyo",
            Enums.Language.Ru => "Служба поддержки",
            Enums.Language.It => "Supporto",
            _ => "Support"
        };

    public string Features() =>
        PreferredLanguage switch
        {
            Enums.Language.De => "Features",
            Enums.Language.Fr => "Caractéristiques",
            Enums.Language.Es => "Características",
            Enums.Language.Ru => "Функции",
            Enums.Language.It => "Caratteristiche",
            _ => "Features"
        };
}
