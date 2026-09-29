using Core;
using Finance;

namespace Mail
{
    /// <summary>Texts of the "Итоги дня" letter — shared by the inbox row and the opened letter.</summary>
    public static class MailFormat
    {
        public static string Subject(MailLetter letter) => $"Итоги дня: {FinanceFormat.Date(letter.day)}";

        /// <summary>"25.09" for the inbox row.</summary>
        public static string ShortDate(MailLetter letter) => GameDay.ToDate(letter.day).ToString("dd.MM");

        /// <summary>One line under the subject in the inbox.</summary>
        public static string Preview(MailLetter letter) =>
            $"Монеты {letter.coins} · Копилка {letter.jarCoins} · Уровень {letter.level}";

        public static string Body(MailLetter letter) =>
            "Привет! День закончился, и я сохранил, как всё было к вечеру:\n\n" +
            $"Монеты: {letter.coins}\n" +
            $"Копилка: {letter.jarCoins}\n" +
            $"Уровень: {letter.level}\n" +
            $"Сытость: {letter.satiety:0}%\n" +
            $"Настроение: {letter.mood:0}%\n" +
            $"Здоровье: {letter.health:0}%\n\n" +
            Notes(letter) +
            "Если что-то пошло не так, можно вернуться к этому сохранению. " +
            "Только помни: всё, что было после, пропадёт.";

        /// <summary>"А ночью:\n• ..." block from DayService, empty when nothing happened (or an old letter).</summary>
        private static string Notes(MailLetter letter)
        {
            if (letter.notes == null || letter.notes.Count == 0)
            {
                return "";
            }

            return "А ночью:\n• " + string.Join("\n• ", letter.notes) + "\n\n";
        }

        public static string ConfirmRestore(MailLetter letter) =>
            $"Вернуться к сохранению за {FinanceFormat.Date(letter.day)}?\nВсё, что было после, пропадёт.";
    }
}
