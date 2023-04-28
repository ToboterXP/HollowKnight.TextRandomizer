using Modding;
using RandomizerMod.RC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace TextRandomizer
{
    public class SaveData
    {
        public bool active = false;
        public Dictionary<string, string> languageKeyMap = new();
    }

    public class TextRandomizer : Mod, ILocalSettings<SaveData>
    {
        internal static TextRandomizer Instance;

        //public override List<ValueTuple<string, string>> GetPreloadNames()
        //{
        //    return new List<ValueTuple<string, string>>
        //    {
        //        new ValueTuple<string, string>("White_Palace_18", "White Palace Fly")
        //    };
        //}

        public static SaveData saveData { get; set; } = new SaveData();
        public void OnLoadLocal(SaveData s) => saveData = s;
        public SaveData OnSaveLocal() => saveData;

        public static string ModName = "Text Randomizer";

        public TextRandomizer() : base(ModName)
        {
            Instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;
            RequestBuilder.OnUpdate.Subscribe(10000f, Randomize);

            ModHooks.LanguageGetHook += OnLanguageGet;

            Menu.Hook();

            Log("Initialized");

        }

        /*public override int LoadPriority()
        {
            return int.MaxValue - 100; //Load after every other mod, to allow randomization of modded strings
        }*/



        public override string GetVersion()
        {
            return "v1.0.0.0";
        }

        public string OnLanguageGet(string key, string sheetTitle, string orig)
        {
            if (saveData == null || !saveData.active) return orig;


            saveData.active = false;

            string mapping;
            if (!saveData.languageKeyMap.TryGetValue(sheetTitle + ";" + key, out mapping)) mapping = sheetTitle + ";" + key;
            string newSheet = mapping.Split(';')[0];
            string newKey = mapping.Split(';')[1];

            string ret = Language.Language.Get(newKey, newSheet);

            saveData.active = true;

            return ret;
        }



        public void Randomize(RequestBuilder rb)
        {
            if (!saveData.active) return;
            System.Random rng = rb.rng;

            saveData.languageKeyMap.Clear();

            var language = (Dictionary<string, Dictionary<string, string>>)typeof(Language.Language).GetField("currentEntrySheets", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            List<(string, int)> languageKeys = new();
            foreach (var sheet in language.Keys)
            {
                foreach(var key in language[sheet].Keys)
                {
                    string s = Language.Language.Get(key, sheet);
                    int t;
                    if (int.TryParse(s, out t)) continue;//ignore numeric values

                    languageKeys.Add((sheet + ";" + key, s.Length));
                }
            }

            languageKeys.Sort((x, y) =>
            {
                if (x.Item2 < y.Item2) return -1;
                else if (x.Item2 > y.Item2) return 1;
                else return 0;
            });



            

            for (int i=0; i<languageKeys.Count; i++)
            {

                Log(languageKeys[i]);
                int dir = 1;
                if (rng.Next(2) == 0) dir = -1;


                int maxShift = 0;

                if (dir == -1) maxShift = i;
                if (dir == 1) maxShift = languageKeys.Count - i - 1;

                /*int offset = maxShift == 0 ? 0 : 1;
                while (rng.Next(100) < 90 && offset < maxShift) offset += 1;*/

                if (maxShift > 100) maxShift = 100;
                int offset = rng.Next(maxShift);

                saveData.languageKeyMap[languageKeys[i].Item1] = languageKeys[i + offset*dir].Item1;
            }
        }
    }
}