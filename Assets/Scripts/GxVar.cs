using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public static class GxVar
    {
        public static string GetString(string key, string defaultValue = "")
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetString(key);
            }
            else
            {
                return defaultValue;
            }
		}
		public static void SetString(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
		}

		public static int GetInt(string key, int defaultValue = 0)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key);
            }
            else
            {
                return defaultValue;
            }
		}

		public static void SetInt(string key, int value)
		{
			PlayerPrefs.SetInt(key, value);
		}

		public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetFloat(key);
            }
            else
            {
                return defaultValue;
            }
        }
		public static void SetFloat(string key, float value)
		{
			PlayerPrefs.SetFloat(key, value);
		}

		public static bool GetBool(string key, bool defaultValue = false)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key) == 1;
            }
            else
            {
                return defaultValue;
            }
		}
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
		}

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
		}

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        public static void Save()
        {
            PlayerPrefs.Save();
        }
	}
}