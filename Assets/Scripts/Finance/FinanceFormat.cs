using System;
using Core;
using UI;
using Work;

namespace Finance
{
    /// <summary>Which pocket a history line is about — picks the amount color.</summary>
    public enum MoneyFlow
    {
        WalletIn,
        WalletOut,
        Jar
    }

    /// <summary>Texts shared by the planning screen, the history and the result popup.</summary>
    public static class FinanceFormat
    {
        private static readonly string[] MonthsGenitive =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        /// <summary>"Сегодня" / "Вчера" / "24 сентября".</summary>
        public static string DayTitle(int day)
        {
            int today = GameDay.Today;
            if (day == today)
            {
                return "Сегодня";
            }

            if (day == today - 1)
            {
                return "Вчера";
            }

            return Date(day);
        }

        /// <summary>"24 сентября"</summary>
        public static string Date(int day)
        {
            DateTime date = GameDay.ToDate(day);
            return $"{date.Day} {MonthsGenitive[date.Month - 1]}";
        }

        /// <summary>"26 сентября", "26–28 сентября", "30 сентября – 2 октября".</summary>
        public static string Period(int startDay, int days)
        {
            if (days <= 1)
            {
                return Date(startDay);
            }

            DateTime start = GameDay.ToDate(startDay);
            DateTime end = GameDay.ToDate(startDay + days - 1);
            return start.Month == end.Month
                ? $"{start.Day}–{end.Day} {MonthsGenitive[end.Month - 1]}"
                : $"{Date(startDay)} – {Date(startDay + days - 1)}";
        }

        /// <summary>"ещё 2 дня" while a day or more is left, "ещё 05:12:33" on the last day.</summary>
        public static string Remaining(float seconds)
        {
            if (seconds >= 86400f)
            {
                return $"ещё {RuPlural.Days((int)Math.Ceiling(seconds / 86400f))}";
            }

            return $"ещё {JobFormat.Countdown(seconds)}";
        }

        public static string EntryTitle(MoneyEntry entry)
        {
            switch (entry.kind)
            {
                case MoneyKind.JobIncome: return $"Работа: {entry.title}";
                case MoneyKind.BuyNeed: return entry.count > 1 ? $"Надо: {entry.title} ×{entry.count}" : $"Надо: {entry.title}";
                case MoneyKind.BuyWant: return $"Хочу: {entry.title}";
                case MoneyKind.Cashback: return $"Кешбэк в копилку ({entry.title})";
                case MoneyKind.JarDeposit: return "Отложил в копилку";
                case MoneyKind.JarWithdraw: return "Забрал из копилки";
                case MoneyKind.DreamPurchase: return $"Куплена мечта: {entry.title}";
                default: return entry.title;
            }
        }

        public static string EntryAmount(MoneyEntry entry)
        {
            switch (entry.kind)
            {
                case MoneyKind.Cashback: return $"+{entry.amount} в копилку";
                case MoneyKind.DreamPurchase: return $"−{entry.amount} из копилки";
                default: return Flow(entry.kind) == MoneyFlow.WalletIn || entry.kind == MoneyKind.JarWithdraw
                    ? $"+{entry.amount}"
                    : $"−{entry.amount}";
            }
        }

        public static MoneyFlow Flow(MoneyKind kind)
        {
            switch (kind)
            {
                case MoneyKind.JobIncome:
                case MoneyKind.PlanReward:
                case MoneyKind.DepositReturn:
                case MoneyKind.DepositInterest:
                    return MoneyFlow.WalletIn;
                case MoneyKind.BuyNeed:
                case MoneyKind.BuyWant:
                case MoneyKind.DepositOpen:
                    return MoneyFlow.WalletOut;
                default:
                    return MoneyFlow.Jar;
            }
        }

        public static string EntryTime(MoneyEntry entry)
        {
            return DateTimeOffset.FromUnixTimeSeconds(entry.unix).LocalDateTime.ToString("HH:mm");
        }
    }
}
