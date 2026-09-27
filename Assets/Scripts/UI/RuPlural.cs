namespace UI
{
    /// <summary>Russian noun agreement with a number: 1 монета, 2 монеты, 5 монет, 11 монет, 21 монета.</summary>
    public static class RuPlural
    {
        public static string Pick(int n, string one, string few, string many)
        {
            int abs = n < 0 ? -n : n;
            int mod100 = abs % 100;
            int mod10 = abs % 10;

            if (mod100 >= 11 && mod100 <= 14)
            {
                return many;
            }

            if (mod10 == 1)
            {
                return one;
            }

            return mod10 >= 2 && mod10 <= 4 ? few : many;
        }

        /// <summary>"5 монет"</summary>
        public static string Coins(int n) => $"{n} {Pick(n, "монету", "монеты", "монет")}";

        /// <summary>"4 дня"</summary>
        public static string Days(int n) => $"{n} {Pick(n, "день", "дня", "дней")}";
    }
}
