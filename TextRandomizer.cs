using Modding;
using Satchel.BetterMenus;
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
        public int seed = new System.Random().Next();
    }

    public class TextRandomizer : Mod, ILocalSettings<SaveData>, ICustomMenuMod
    {
        internal static TextRandomizer Instance;

        //public override List<ValueTuple<string, string>> GetPreloadNames()
        //{
        //    return new List<ValueTuple<string, string>>
        //    {
        //        new ValueTuple<string, string>("White_Palace_18", "White Palace Fly")
        //    };
        //}

        public static SaveData SaveData { get; set; } = new();

        public bool ToggleButtonInsideMenu { get; }

        public void OnLoadLocal(SaveData s)
        {
            SaveData = s;
        }
        public SaveData OnSaveLocal() => SaveData;

        private Menu MenuRef;
        private Element[] menuElements;
        private TextPanel mainMenuWarning;

        public static string ModName = "Text Randomizer";

        public TextRandomizer() : base(ModName)
        {
            Instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            ModHooks.LanguageGetHook += OnLanguageGet;

            On.QuitToMenu.Start += ResetSave;
            On.HeroController.Start += (s, e) => { SetVisible(true); s(e); };

            if (ModHooks.GetMod("Randomizer 4") is Mod) RandomizerIntegration.Init();

            Log("Initialized");

        }

        /*public override int LoadPriority() 
        {
            return int.MaxValue; //Load after every other mod, to allow randomization of modded strings
        }*/

        public IEnumerator ResetSave(On.QuitToMenu.orig_Start orig, global::QuitToMenu self)
        {
            SaveData = null;
            SetVisible(false);
            return orig(self);
        } 


        public override string GetVersion()
        {
            return "v1.1.0.0";
        }

        public string OnLanguageGet(string key, string sheetTitle, string orig)
        {
            if (SaveData == null || !SaveData.active) return orig;


            SaveData.active = false;

            string mapping;
            if (!SaveData.languageKeyMap.TryGetValue(sheetTitle + ";" + key, out mapping)) mapping = sheetTitle + ";" + key;
            string newSheet = mapping.Split(';')[0];
            string newKey = mapping.Split(';')[1];

            string ret = Language.Language.Get(newKey, newSheet);

            SaveData.active = true;

            return ret;
        }

        public void Randomize()
        {
            if (SaveData == null) return;
            Randomize(new System.Random(SaveData.seed));
        }


        public void Randomize(System.Random rng)
        {
            if (SaveData == null) return;
            if (!SaveData.active) return;

            SaveData.languageKeyMap.Clear();

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
                int dir = 1;
                if (rng.Next(2) == 0) dir = -1;


                int maxShift = 0;

                if (dir == -1) maxShift = i;
                if (dir == 1) maxShift = languageKeys.Count - i - 1;

                /*int offset = maxShift == 0 ? 0 : 1;
                while (rng.Next(100) < 90 && offset < maxShift) offset += 1;*/

                if (maxShift > 100) maxShift = 100;
                int offset = rng.Next(maxShift);

                if (languageKeys[i].Item2 > 0 && languageKeys[i + offset * dir].Item2 == 0) dir = 1; //avoid removing important buttons

                SaveData.languageKeyMap[languageKeys[i].Item1] = languageKeys[i + offset*dir].Item1;
            }
        }

        public void ReplaceAll(string sheetKey)
        {
            var language = (Dictionary<string, Dictionary<string, string>>)typeof(Language.Language).GetField("currentEntrySheets", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var sheet in language.Keys)
            {
                foreach (var key in language[sheet].Keys)
                {
                    string s = Language.Language.Get(key, sheet);
                    int t;
                    if (int.TryParse(s, out t)) continue;//ignore numeric values
                    SaveData.languageKeyMap[sheet + ";" + key] = sheetKey;
                }
            }
        }

        public void ZoteMode()
        {
            var language = (Dictionary<string, Dictionary<string, string>>)typeof(Language.Language).GetField("currentEntrySheets", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            System.Random rng = new(SaveData.seed);
            foreach (var sheet in language.Keys)
            {
                foreach (var key in language[sheet].Keys)
                {
                    string s = Language.Language.Get(key, sheet);
                    int t;
                    if (int.TryParse(s, out t)) continue;//ignore numeric values

                    string sheetKey = "Titles;ZOTE_MAIN";
                    if (s.Length > 20 && rng.Next(100) < 70) sheetKey = "Zote;PRECEPT_" + rng.Next(1, 58);
                    SaveData.languageKeyMap[sheet + ";" + key] = sheetKey;
                }
            }
        }

        public void SetActive(bool val)
        {
            SaveData.active = val;
            if (ModHooks.GetMod("Randomizer 4") is Mod) RandomizerMenu.Instance.UpdateSettings();
        }

        public void SetVisible(bool val)
        {
            foreach (var e in menuElements) { e.isVisible = val; }
            mainMenuWarning.isVisible = !val;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            mainMenuWarning = new TextPanel("Please open a save file first");
            menuElements = new Element[]
            {
                mainMenuWarning,
                new MenuButton("Randomize", "Shuffles all text in the game", (buttton) => {SetActive(true); Randomize(); } ),
                new MenuButton("Change Seed", "Changes the current randomization to a new one", (buttton) => {SetActive(true); SaveData.seed++; Randomize(); } ),
                new MenuButton("No Text", "Remove all text", (buttton) => {SetActive(true); ReplaceAll("UI;UI_BLANK"); } ),
                new MenuButton("Zote Mode", "Zote Mode", (buttton) => {SetActive(true); ZoteMode(); } ),
                new MenuButton("Disable", "Turns off the mod", (buttton) => {SetActive(false); } ),
            };
            MenuRef ??= new Menu(ModName, menuElements);

            SetVisible(false);

            var ret = MenuRef.GetMenuScreen(modListMenu);

            return ret; 
        }
    }
}