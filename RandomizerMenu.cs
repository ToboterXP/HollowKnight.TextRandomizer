using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextRandomizer
{
    //Looks Menu's back on the menu, boys!
    internal class RandomizerMenu
    {
        internal MenuPage TextRandoPage;

        internal SmallButton MenuActivationButton;

        internal static RandomizerMenu Instance { get; private set; }

        ToggleButton EnableToggle1;

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage((landingPage) => Instance = new(landingPage), ReturnMainMenuButton);
        }

        public static bool ReturnMainMenuButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.MenuActivationButton;
            return true;
        }

        private void UpdateEnable(bool value)
        {
            TextRandomizer.SaveData.active = value;
        }



        public void UpdateSettings()
        {
            EnableToggle1.SetValue(TextRandomizer.SaveData.active);
        }


        private RandomizerMenu(MenuPage landingPage)
        {
            Instance = this;

            TextRandoPage = new MenuPage(TextRandomizer.ModName, landingPage);

            landingPage.BeforeShow += () => MenuActivationButton.Text.color = TextRandomizer.SaveData.active ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;

            VerticalItemPanel layout = new(TextRandoPage, new(0, 300), 150, true);

            EnableToggle1 = new(TextRandoPage, "Enable " + TextRandomizer.ModName);
            EnableToggle1.ValueChanged += UpdateEnable;


            layout.Add(new MenuLabel(TextRandoPage, TextRandomizer.ModName, MenuLabel.Style.Title));
            layout.Add(new MenuLabel(TextRandoPage, "Shuffle around all the text in the game", MenuLabel.Style.Body));

            layout.Add(EnableToggle1);

            MenuActivationButton = new(landingPage, TextRandomizer.ModName);
            MenuActivationButton.AddHideAndShowEvent(landingPage, TextRandoPage);
        }
    }
}
