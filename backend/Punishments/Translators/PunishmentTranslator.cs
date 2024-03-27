using Bot.Abstractions;
using Bot.Enums;
using Discord;

namespace Punishments.Translators;

public class PunishmentTranslator : Translator
{
    public string CaseCount() =>
        PreferredLanguage switch
        {
            Language.De => "Fallzahl",
            Language.Fr => "Nombre de cas",
            Language.Es => "Recuento de casos",
            Language.Ru => "Количество дел",
            Language.It => "Conteggio casi",
            _ => "Case Count"
        };

    public string Punishment() =>
        PreferredLanguage switch
        {
            Language.De => "Bestrafung",
            Language.Fr => "Châtiment",
            Language.Es => "Castigo",
            Language.Ru => "Наказание",
            Language.It => "Punizione",
            _ => "Punishment"
        };

    public string PunishedUntil() =>
        PreferredLanguage switch
        {
            Language.De => "Bestrafung bis",
            Language.Fr => "Puni jusqu'à",
            Language.Es => "Castigado hasta",
            Language.Ru => "Наказан до",
            Language.It => "Punito fino a",
            _ => "Punished until"
        };

    public string AlreadyFinalWarned() =>
        PreferredLanguage switch
        {
            Language.De => "Dieser Benutzer ist bereits endgültig gewarnt!",
            Language.Fr => "Cet utilisateur est déjà prévenu définitivement!",
            Language.Es => "¡Este usuario ya es el último advertido!",
            Language.Ru => "Этот пользователь уже получил окончательное предупреждение!",
            Language.It => "Questo utente è già stato avvisato definitivamente!",
            _ => "This user is already final warned!"
        };

    public string DmReceipt() =>
        PreferredLanguage switch
        {
            Language.De => "DM-Quittung",
            Language.Fr => "Reçu DM",
            Language.Es => "Recibo de mensaje directo",
            Language.Ru => "Квитанция DM",
            Language.It => "Ricevuta DM",
            _ => "DM Receipt"
        };

    public string Cases() =>
        PreferredLanguage switch
        {
            Language.De => "Vorfälle",
            Language.Fr => "Cas",
            Language.Es => "Casos",
            Language.Ru => "Случаи",
            Language.It => "casi",
            _ => "Cases"
        };

    public string Severity() =>
        PreferredLanguage switch
        {
            Language.De => "Schwere",
            Language.Fr => "Gravité",
            Language.Es => "Gravedad",
            Language.Ru => "Строгость",
            Language.It => "Gravità",
            _ => "Severity"
        };

    public string NoCases() =>
        PreferredLanguage switch
        {
            Language.De => "Es gibt keine Fälle für diesen Benutzer.",
            Language.Fr => "Il n'y a pas de cas pour cet utilisateur.",
            Language.Es => "No hay casos para este usuario.",
            Language.Ru => "Для этого пользователя нет случаев.",
            Language.It => "Non ci sono casi per questo utente.",
            _ => "There are no cases for this user."
        };

    public string CaseId() =>
        PreferredLanguage switch
        {
            Language.De => "Fall-ID",
            Language.Fr => "ID de cas",
            Language.Es => "Identificación del caso",
            Language.Ru => "Идентификатор дела",
            Language.It => "ID Caso",
            _ => "Case ID"
        };

    public string ActivePunishments() =>
        PreferredLanguage switch
        {
            Language.De => "Aktive Bestrafungen",
            Language.Fr => "Punitions actives",
            Language.Es => "Castigos activos",
            Language.Ru => "Активные наказания",
            Language.It => "punizioni attive",
            _ => "Active punishments"
        };

    public string Imported() =>
        PreferredLanguage switch
        {
            Language.De => "Importiert",
            Language.Fr => "Importé",
            Language.Es => "Importado",
            Language.Ru => "Импортный",
            Language.It => "importato",
            _ => "Imported"
        };

    internal string UserTriggeredOnFinalWarn() =>
        PreferredLanguage switch
        {
            Language.De => "Letzter verwarnter Benutzer hat einen Verstoß ausgelöst",
            Language.Fr => "Le dernier utilisateur averti a déclenché une infraction",
            Language.Es => "El último usuario advertido ha provocado una infracción",
            Language.Ru => "Последний предупрежденный пользователь спровоцировал нарушение",
            Language.It => "L'utente con avviso finale ha attivato un'infrazione",
            _ => "Final warned user has triggered an infraction"
        };

    public string ImportedFromExistingBans() =>
        PreferredLanguage switch
        {
            Language.De => "Importiert aus bestehenden Sperren",
            Language.Fr => "Importé à partir des interdictions existantes",
            Language.Es => "Importado de prohibiciones existentes",
            Language.Ru => "Импортировано из существующих банов",
            Language.It => "Importato da divieti esistenti",
            _ => "Imported from existing bans"
        };

    public string CaseCreated(int caseId, string caseLink, int caseCount) =>
        PreferredLanguage switch
        {
            Language.De =>
                $"Fall `#{caseId}` erstellt: {caseLink}\nDer Benutzer hat `{caseCount}` Mod-Fälle aufgezeichnet.",
            Language.Fr => $"Cas `#{caseId}` créé : {caseLink}\nL'utilisateur a `{caseCount}` cas de mod enregistrés.",
            Language.Es =>
                $"Caso `# {caseId}` creado: {caseLink}\nEl usuario tiene `{caseCount}` caso(s) de modificación registrados.",
            Language.Ru =>
                $"Обращение `# {caseId}` создано: {caseLink}\nУ пользователя зарегистрировано `{caseCount}` случаев модификации.",
            Language.It => $"Caso `#{caseId}` creato: {caseLink}\nL'utente ha registrato casi mod `{caseCount}`.",
            _ => $"Case `#{caseId}` created: {caseLink}\nUser has `{caseCount}` mod-case(s) recorded."
        };

    public string ReportFailed() =>
        PreferredLanguage switch
        {
            Language.De => "Interner Benachrichtigungsversand an Moderatoren für Meldebefehl fehlgeschlagen.",
            Language.Fr => "Échec de l'envoi de la notification interne aux modérateurs pour la commande de rapport.",
            Language.Es => "No se pudo enviar una notificación interna a los moderadores para el comando de informe.",
            Language.Ru => "Не удалось отправить внутреннее уведомление модераторам для команды отчета.",
            Language.It => "Impossibile inviare una notifica interna ai moderatori per il comando di segnalazione.",
            _ => "Failed to send internal notification to moderators for report command."
        };

    public string ReportCreated() =>
        PreferredLanguage switch
        {
            Language.De => "Bericht Erstellt",
            Language.Fr => "Rapport Créé",
            Language.Es => "Informe Creado",
            Language.Ru => "Отчет создан",
            Language.It => "Rapporto Creato",
            _ => "Report Created"
        };

    public string ReportSent() =>
        PreferredLanguage switch
        {
            Language.De => "Meldung gesendet.",
            Language.Fr => "Rapport envoyé.",
            Language.Es => "Reporte enviado.",
            Language.Ru => "Отчет отправлен.",
            Language.It => "Rapporto inviato.",
            _ => "Report sent."
        };

    public string ReportContent(IUser user, IMessage message, IMentionable channel) =>
        PreferredLanguage switch
        {
            Language.De =>
                $"{user.Mention} meldete eine Nachricht von {message.Author.Mention} in {channel.Mention}.",
            Language.Fr =>
                $"{user.Mention} a signalé un message de {message.Author.Mention} dans {channel.Mention}.",
            Language.Es =>
                $"{user.Mention} informó un mensaje de {message.Author.Mention} en {channel.Mention}.",
            Language.Ru =>
                $"{user.Mention} сообщил о сообщении от {message.Author.Mention} в {channel.Mention}.",
            Language.It =>
                $"{user.Mention} ha segnalato un messaggio da {message.Author.Mention} in {channel.Mention}.",
            _ =>
                $"{user.Mention} reported a message from {message.Author.Mention} in {channel.Mention}."
        };

    public string NotAllowedToViewCase() =>
        PreferredLanguage switch
        {
            Language.De => "Du darfst diesen Fall nicht ansehen.",
            Language.Fr => "Vous n'êtes pas autorisé à voir ce cas.",
            Language.Es => "No se le permite ver este caso.",
            Language.Ru => "Вам не разрешено просматривать это дело.",
            Language.It => "Non sei autorizzato a visualizzare questo caso.",
            _ => "You are not allowed to view this case."
        };

    public string Result() =>
        PreferredLanguage switch
        {
            Language.De => "Ergebnis",
            Language.Fr => "Résultat",
            Language.Es => "Resultado",
            Language.Ru => "Результат",
            Language.It => "Risultato",
            _ => "Result"
        };

    public string WaitingForApproval() =>
        PreferredLanguage switch
        {
            Language.De => "Warte auf Bestätigung.",
            Language.Fr => "En attente d'approbation.",
            Language.Es => "A la espera de la aprobación.",
            Language.Ru => "Ожидание подтверждения.",
            Language.It => "In attesa di approvazione.",
            _ => "Waiting for approval."
        };

    public string Canceled() =>
        PreferredLanguage switch
        {
            Language.De => "Abgebrochen",
            Language.Fr => "Annulé",
            Language.Es => "Cancelado",
            Language.Ru => "Отменено",
            Language.It => "Annullato",
            _ => "Canceled"
        };

    public string Cancel() =>
        PreferredLanguage switch
        {
            Language.De => "Abbrechen",
            Language.Fr => "Annuler",
            Language.Es => "Cancelar",
            Language.Ru => "Отмена",
            Language.It => "Annulla",
            _ => "Cancel"
        };

    public string CreatedAt() =>
        PreferredLanguage switch
        {
            Language.De => "Erstellt am.",
            Language.Fr => "Créé à.",
            Language.Es => "Creado en.",
            Language.Ru => "Создано в.",
            Language.It => "Creato a.",
            _ => "Created at."
        };

    public string NoActiveModCases() =>
        PreferredLanguage switch
        {
            Language.De => "Keine aktiven Mod-Fälle wurden gefunden.",
            Language.Fr => "Aucun modcase actif n'a été trouvé.",
            Language.Es => "No se han encontrado casos de modulación activos.",
            Language.Ru => "Активных модкейсов не обнаружено.",
            Language.It => "Nessun modcase attivo è stato trovato.",
            _ => "No active modcases have been found."
        };

    public string FoundCasesForUnmute(int caseCount) =>
        PreferredLanguage switch
        {
            Language.De =>
                $"Es wurden `{caseCount}` aktive Fälle gefunden. Möchtest du alle deaktivieren oder löschen, um den Benutzer nicht mehr stummgeschaltet zu lassen?",
            Language.Fr =>
                $"`{caseCount}` cas actifs trouvés. Voulez-vous les désactiver ou les supprimer tous pour réactiver le son de l'utilisateur ?",
            Language.Es =>
                $"Se encontraron casos activos `{caseCount}`. ¿Quieres desactivarlos o eliminarlos todos para dejar de silenciar al usuario?",
            Language.Ru =>
                $"Обнаружены активные обращения `{caseCount}`. Вы хотите деактивировать или удалить их все, чтобы включить микрофон для пользователя?",
            Language.It =>
                $"Trovati casi attivi di `{caseCount}`. Vuoi disattivarli o eliminarli tutti per riattivare l'audio dell'utente?",
            _ =>
                $"Found `{caseCount}` active cases. Do you want to deactivate or delete all of them to unmute the user?"
        };

    public string MutesDeleted() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen gelöscht",
            Language.Fr => "Muets supprimés",
            Language.Es => "Silenciados eliminados",
            Language.Ru => "Без звука удалено",
            Language.It => "Disattiva audio cancellato",
            _ => "Mutes deleted"
        };

    public string MutesDeactivated() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen deaktiviert",
            Language.Fr => "Muet désactivé",
            Language.Es => "Silencios desactivados",
            Language.Ru => "Отключение звука отключено",
            Language.It => "Mute disattivate",
            _ => "Mutes deactivated"
        };

    public string DeleteMutes() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen löschen",
            Language.Fr => "Supprimer les sourdines",
            Language.Es => "Eliminar silencios",
            Language.Ru => "Удалить отключение звука",
            Language.It => "Elimina mute",
            _ => "Delete Mutes"
        };

    public string DeactivateMutes() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen deaktivieren",
            Language.Fr => "Désactiver les sourdines",
            Language.Es => "Silenciar desactivados",
            Language.Ru => "Отключить отключение звука",
            Language.It => "Disattiva sordina",
            _ => "Deativate Mutes"
        };

    public string FoundCasesForUnban(int caseCount) =>
        PreferredLanguage switch
        {
            Language.De =>
                $"Es wurden `{caseCount}` aktive Fälle gefunden. Möchtest du alle deaktivieren oder löschen, um den Benutzer entsperren zu lassen?",
            Language.Fr =>
                $"`{caseCount}` cas actifs trouvés. Voulez-vous les désactiver ou les supprimer tous pour annuler l'interdiction de l'utilisateur ?",
            Language.Es =>
                $"Se encontraron casos activos `{caseCount}`. ¿Quieres desactivarlos o eliminarlos todos para desbloquear al usuario?",
            Language.Ru =>
                $"Обнаружены активные обращения `{caseCount}`. Вы хотите деактивировать или удалить их все, чтобы разблокировать пользователя?",
            Language.It =>
                $"Trovati casi attivi di `{caseCount}`. Vuoi disattivarli o eliminarli tutti per riabilitare l'utente?",
            _ => $"Found `{caseCount}` active cases. Do you want to deactivate or delete all of them to unban the user?"
        };

    public string BansDeleted() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen gelöscht",
            Language.Fr => "Interdictions supprimées",
            Language.Es => "Prohibiciones eliminadas",
            Language.Ru => "Баны удалены",
            Language.It => "Divieti cancellati",
            _ => "Bans deleted"
        };

    public string BansDeactivated() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen deaktiviert",
            Language.Fr => "Interdictions désactivées",
            Language.Es => "Prohibiciones desactivadas",
            Language.Ru => "Баны отключены",
            Language.It => "Divieti disattivati",
            _ => "Bans deactivated"
        };

    public string DeleteBans() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen löschen",
            Language.Fr => "Supprimer les bannissements",
            Language.Es => "Eliminar prohibiciones",
            Language.Ru => "Удалить баны",
            Language.It => "Elimina ban",
            _ => "Delete Bans"
        };

    public string DeactivateBans() =>
        PreferredLanguage switch
        {
            Language.De => "Sperrungen deaktivieren",
            Language.Fr => "Désactiver les interdictions",
            Language.Es => "Prohibiciones de desactivación",
            Language.Ru => "Деактивировать баны",
            Language.It => "Divieti di disattivazione",
            _ => "Deativate Bans"
        };
}
