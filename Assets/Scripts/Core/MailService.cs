using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Core
{
    /// <summary>One "Итоги дня" letter: the day it's about + what the save looked like at the end of it (for the letter text).</summary>
    [Serializable]
    public class MailLetter
    {
        public int day;
        public bool read;
        public int coins;
        public int jarCoins;
        public int level;
        public float satiety;
        public float mood;
        public float health;

        /// <summary>What happened overnight (DayService): cashback, spoiled food, stat decay. Empty for letters from before economy v3.</summary>
        public List<string> notes = new List<string>();
    }

    /// <summary>Everything in mail.json. Kept outside save.json on purpose — restoring a snapshot must not bring back an old inbox.</summary>
    [Serializable]
    public class MailBox
    {
        /// <summary>Newest first.</summary>
        public List<MailLetter> letters = new List<MailLetter>();
    }

    /// <summary>
    /// Daily save snapshots + the mail about them. When a GameDay ends (seen on launch, by Bootstrap's
    /// periodic check, or the debug day skip), the save as it was at the end of that day is copied to
    /// saves/day_N.json and a letter for day N arrives. Restoring copies the snapshot over save.json,
    /// deletes letters/snapshots of later days ("no going forward") — the caller then reloads the scene.
    /// At most MaxLetters days back are kept. Must run before FinanceService touches the new day.
    /// </summary>
    public class MailService
    {
        public const int MaxLetters = 7;
        public const string Sender = "Барсук";

        private const string FolderName = "saves";
        private const string MailFileName = "mail.json";

        private readonly SaveService _saveService;
        private readonly PlayerDataService _playerData;
        private readonly DayService _dayService;
        private readonly string _folder;
        private readonly MailBox _box;

        public MailService(SaveService saveService, PlayerDataService playerData, DayService dayService)
        {
            _saveService = saveService;
            _playerData = playerData;
            _dayService = dayService;
            _folder = Path.Combine(Application.persistentDataPath, FolderName);
            Directory.CreateDirectory(_folder);
            _box = LoadBox();
            DropLettersWithoutSnapshot();
            CheckDay();
        }

        /// <summary>Newest first.</summary>
        public IReadOnlyList<MailLetter> Letters => _box.letters;
        public int UnreadCount => _box.letters.Count(l => !l.read);

        /// <summary>Cheap to call often — does real work once per day change.</summary>
        public void CheckDay()
        {
            int today = GameDay.Today;
            int last = _playerData.LastActiveDay;
            if (last == today)
            {
                return;
            }

            // last == 0: save from before the mail existed. last > today: debug offset went back — just follow it.
            if (last > 0 && last < today)
            {
                // The snapshot is the state at the end of that day; the night (cashback, spoiled food, stat decay)
                // happens after it and goes into that day's letter.
                SnapshotDay(last);
                List<string> notes = _dayService?.ProcessNewDays(last, today);
                MailLetter letter = Find(last);
                if (letter != null && notes != null && notes.Count > 0)
                {
                    letter.notes = notes;
                    SaveBoxAndPublish();
                }

                UI.Feedback.Show("Новый день! Итоги вчерашнего — в почте");
            }

            _playerData.SetLastActiveDay(today);
        }

        public void MarkRead(int day)
        {
            MailLetter letter = Find(day);
            if (letter == null || letter.read)
            {
                return;
            }

            letter.read = true;
            SaveBoxAndPublish();
        }

        /// <summary>Copies day's snapshot over save.json and removes later letters. After true the caller must reload the scene (SaveService is frozen until then).</summary>
        public bool TryRestore(int day)
        {
            string path = SnapshotPath(day);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"MailService: no snapshot for day {day}.");
                return false;
            }

            foreach (MailLetter later in _box.letters.Where(l => l.day > day).ToList())
            {
                Remove(later);
            }

            SaveBox();
            return _saveService.RestoreFrom(path);
        }

        /// <summary>Snapshot of the current state + letter for that day. Replaces an existing letter of the same day (keeps its read mark).</summary>
        private void SnapshotDay(int day)
        {
            _playerData.WriteCopyTo(SnapshotPath(day));

            MailLetter existing = Find(day);
            CharacterStats stats = _playerData.Stats;
            var letter = new MailLetter
            {
                day = day,
                read = existing != null && existing.read,
                coins = _playerData.Coins,
                jarCoins = _playerData.JarCoins,
                level = _playerData.Level,
                satiety = stats.satiety,
                mood = stats.mood,
                health = stats.health
            };

            if (existing != null)
            {
                _box.letters.Remove(existing);
            }

            _box.letters.Add(letter);
            _box.letters.Sort((a, b) => b.day.CompareTo(a.day));

            while (_box.letters.Count > MaxLetters)
            {
                Remove(_box.letters[_box.letters.Count - 1]);
            }

            SaveBoxAndPublish();
        }

        private void Remove(MailLetter letter)
        {
            _box.letters.Remove(letter);
            string path = SnapshotPath(letter.day);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private MailLetter Find(int day) => _box.letters.FirstOrDefault(l => l.day == day);

        private string SnapshotPath(int day) => Path.Combine(_folder, $"day_{day}.json");

        /// <summary>Snapshot file deleted by hand → its letter would have a dead button.</summary>
        private void DropLettersWithoutSnapshot()
        {
            if (_box.letters.RemoveAll(l => !File.Exists(SnapshotPath(l.day))) > 0)
            {
                SaveBox();
            }
        }

        private MailBox LoadBox()
        {
            string path = Path.Combine(_folder, MailFileName);
            if (!File.Exists(path))
            {
                return new MailBox();
            }

            try
            {
                MailBox box = JsonUtility.FromJson<MailBox>(File.ReadAllText(path)) ?? new MailBox();
                box.letters.Sort((a, b) => b.day.CompareTo(a.day));
                return box;
            }
            catch (Exception e)
            {
                Debug.LogError($"MailService: failed to read {path}, starting an empty inbox. {e}");
                return new MailBox();
            }
        }

        private void SaveBox()
        {
            string path = Path.Combine(_folder, MailFileName);
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(_box, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"MailService: failed to write {path}. {e}");
            }
        }

        private void SaveBoxAndPublish()
        {
            SaveBox();
            EventBus.Publish(new MailChangedEvent());
        }

        // ---------- Debug (StatsDebug) ----------

        /// <summary>The current state as the snapshot of yesterday, with its letter.</summary>
        public void DebugSnapshotYesterday() => SnapshotDay(GameDay.Today - 1);

        /// <summary>Letters for the last MaxLetters days, all with the current state.</summary>
        public void DebugFillWeek()
        {
            for (int i = MaxLetters; i >= 1; i--)
            {
                SnapshotDay(GameDay.Today - i);
            }
        }

        public void DebugClear()
        {
            foreach (MailLetter letter in _box.letters.ToList())
            {
                Remove(letter);
            }

            SaveBoxAndPublish();
        }
    }
}
