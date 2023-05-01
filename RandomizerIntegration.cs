using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomizerMod.RC;

namespace TextRandomizer
{
    internal class RandomizerIntegration
    {
        public static void Init()
        {
            RequestBuilder.OnUpdate.Subscribe(10000f, Randomize);
            RandomizerMenu.Hook();
        }

        public static void Randomize(RequestBuilder rb)
        {
            TextRandomizer.SaveData.seed = rb.gs.Seed + 1;
            TextRandomizer.Instance.Randomize();
        }
    }
}
