using System;
using UnityEngine;

namespace UI
{
    /// <summary>Inspector pair "key from json → Sprite" (iconKey, speaker, ...). JSON can't reference Unity assets, so art is mapped here.</summary>
    [Serializable]
    public struct KeyedSprite
    {
        public string key;
        public Sprite sprite;

        /// <summary>Null if the key isn't in the list — callers keep their placeholder sprite then.</summary>
        public static Sprite Find(KeyedSprite[] list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            foreach (KeyedSprite item in list)
            {
                if (item.key == key)
                {
                    return item.sprite;
                }
            }

            return null;
        }
    }
}
