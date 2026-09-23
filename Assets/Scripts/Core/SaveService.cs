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

        public SaveService()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
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
