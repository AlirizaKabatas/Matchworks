using System;
using UnityEngine;
namespace WorkshopMatch
{
    public static class AchievementProgressStore
    {
        public const string SaveKey="WorkshopMatch.Achievements.v1";
        public static AchievementProgressData Load()
        {
            string json=PlayerPrefs.GetString(SaveKey,"");
            if(string.IsNullOrEmpty(json))return new AchievementProgressData();
            try {return JsonUtility.FromJson<AchievementProgressData>(json)??new AchievementProgressData();}
            catch(ArgumentException){return new AchievementProgressData();}
        }
        public static void Save(AchievementProgressData data)
        {PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();}
    }
}
