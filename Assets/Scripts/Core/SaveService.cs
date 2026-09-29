using System;
using System.IO;
using UnityEngine;

namespace Core
{
    /// <summary>Reads/writes save.json in persistentDataPath. Plain JsonUtility, no encryption — good enough for the hackathon build.</summary>
    public class SaveService
    {
        private const string FileName = "save.json";
        private readonly string _path;

        /// <summary>Set after a snapshot was copied over save.json: the old scene is about to be reloaded and must not overwrite the restored file on its way out.</summary>
        private bool _frozen;

        public SaveService()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
        }

        /// <summary>Writes data to any file (daily snapshots for the mail). Ignores the freeze — only save.json is protected.</summary>
        public void WriteTo(PlayerData data, string path)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: failed to write {path}. {e}");
            }
        }

        /// <summary>Replaces save.json with a copy of the given file and blocks further writes from this scene. The caller must reload the scene right after.</summary>
        public bool RestoreFrom(string path)
        {
            try
            {
                File.Copy(path, _path, true);
                _frozen = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: failed to restore {_path} from {path}. {e}");
                return false;
            }
        }

        /// <summary>
        /// "Удалить аккаунт": deletes save.json and the saves/ folder (daily snapshots + mail) and blocks further writes
        /// from this scene, like RestoreFrom. The caller must reload the scene right after — it starts as a brand-new profile.
        /// Settings in PlayerPrefs (volume, animations) are kept.
        /// </summary>
        public bool DeleteAll()
        {
            _frozen = true;
            try
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                string saves = Path.Combine(Application.persistentDataPath, "saves");
                if (Directory.Exists(saves))
                {
                    Directory.Delete(saves, true);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: failed to delete the save. {e}");
                return false;
            }
        }

        public PlayerData Load()
        {
            if (!File.Exists(_path))
            {
                return new PlayerData();
            }

            try
            {
                string json = File.ReadAllText(_path);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                return data ?? new PlayerData();
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: failed to read {_path}, starting a fresh save. {e}");
                return new PlayerData();
            }
        }

        public void Save(PlayerData data)
        {
            if (_frozen)
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: failed to write {_path}. {e}");
            }
        }
    }
}
