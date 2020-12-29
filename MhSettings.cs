using System.Collections.Generic;
using Modding;

namespace MoreHealing
{
    class MhSettings : ModSettings
    {
        // insert default values here
        public List<bool> gotCharms = new List<bool>() { true, true, true, true };
        public List<bool> newCharms = new List<bool>() { false, false, false, false };
        public List<bool> equippedCharms = new List<bool>() { false, false, false, false };
        public List<int> charmCosts = new List<int>() { 4, 5, 5, 6 };
    }
}
