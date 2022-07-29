using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using MoreHealing.Consts;
using SFCore;
using SFCore.Generics;
using SFCore.Utils;
using UnityEngine;

namespace MoreHealing
{
    class MoreHealing : SaveSettingsMod<MhSettings>
    {

        public TextureStrings Ts { get; private set; }
        public List<int> CharmIDs { get; private set; }

        // Thx to 56
        public override string GetVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            string ver = asm.GetName().Version.ToString();

            SHA1 sha1 = SHA1.Create();
            FileStream stream = File.OpenRead(asm.Location);

            byte[] hashBytes = sha1.ComputeHash(stream);

            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            stream.Close();
            sha1.Clear();

            string ret = $"{ver}-{hash.Substring(0, 6)}";

            return ret;
        }

        public MoreHealing() : base("More Healing")
        {
            Ts = new TextureStrings();
        }

        public override void Initialize()
        {
            Log("Initializing");

            CharmIDs = CharmHelper.AddSprites(Ts.Get(TextureStrings.QuickerFocusKey), Ts.Get(TextureStrings.QuickestFocusKey), Ts.Get(TextureStrings.DeeperFocusKey), Ts.Get(TextureStrings.DeepestFocusKey));

            InitCallbacks();

            Log("Initialized");
        }

        private void InitCallbacks()
        {
            ModHooks.GetPlayerBoolHook += OnGetPlayerBoolHook;
            ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
            ModHooks.GetPlayerIntHook += OnGetPlayerIntHook;
            ModHooks.AfterSavegameLoadHook += InitSaveSettings;
            ModHooks.LanguageGetHook += OnLanguageGetHook;
            //ModHooks.Instance.CharmUpdateHook += OnCharmUpdateHook;

            On.HeroController.Start += OnHeroControllerStart;
            On.GameManager.Update += GameManagerUpdate;
        }

        #region Mod Reload

        private void GameManagerUpdate(On.GameManager.orig_Update orig, GameManager self)
        {
            orig(self);
        }

        #endregion

        private bool _changed = false;
        private void OnHeroControllerStart(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            if (_changed) return;
            _changed = true;

            self.StartCoroutine(AddCharmStates(self));

            _changed = true;
        }
        private void InitSaveSettings(SaveGameData data)
        {
            // Found in a project, might help saving, don't know, but who cares
            // Charms
            SaveSettings.gotCharms = SaveSettings.gotCharms;
            SaveSettings.newCharms = SaveSettings.newCharms;
            SaveSettings.equippedCharms = SaveSettings.equippedCharms;
            SaveSettings.charmCosts = SaveSettings.charmCosts;

            _changed = false;
        }

        private IEnumerator AddCharmStates(HeroController self)
        {
            var spellFsm = self.gameObject.LocateMyFSM("Spell Control");
            var spellFsmVar = spellFsm.FsmVariables;

            #region Quick Focus Speeds

            var fmAction = new FloatMultiply();
            fmAction.floatVariable = spellFsmVar.FindFsmFloat("Time Per MP Drain");
            fmAction.multiplyBy = 2f / 3f;

            spellFsm.CopyState("Set Focus Speed", "Set QuickerFocus Speed");
            spellFsm.RemoveAction("Set QuickerFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickerFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickerFocus Speed", 2);
            spellFsm.GetAction<PlayerDataBoolTest>("Set QuickerFocus Speed", 0).boolName = $"equippedCharm_{CharmIDs[0]}";
            spellFsm.AddAction("Set QuickerFocus Speed", fmAction);
            spellFsm.GetAction<FloatMultiply>("Set QuickerFocus Speed", 2).multiplyBy = Mathf.Pow(2f/3f, 2);

            spellFsm.CopyState("Set Focus Speed", "Set QuickestFocus Speed");
            spellFsm.RemoveAction("Set QuickestFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickestFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickestFocus Speed", 2);
            spellFsm.GetAction<PlayerDataBoolTest>("Set QuickestFocus Speed", 0).boolName = $"equippedCharm_{CharmIDs[1]}";
            spellFsm.AddAction("Set QuickestFocus Speed", fmAction);
            spellFsm.GetAction<FloatMultiply>("Set QuickestFocus Speed", 2).multiplyBy = Mathf.Pow(2f/3f, 3);

            spellFsm.ChangeTransition("Set Focus Speed", "FINISHED", "Set QuickerFocus Speed");
            spellFsm.ChangeTransition("Set QuickerFocus Speed", "FINISHED", "Set QuickestFocus Speed");

            #endregion

            #region Deep Focus Speeds

            spellFsm.CopyState("Deep Focus Speed", "Deeper Focus Speed");
            spellFsm.GetAction<PlayerDataBoolTest>("Deeper Focus Speed", 0).boolName = $"equippedCharm_{CharmIDs[2]}";
            spellFsm.GetAction<FloatMultiply>("Deeper Focus Speed", 1).multiplyBy = Mathf.Pow(1.65f, 2);

            spellFsm.CopyState("Deep Focus Speed", "Deepest Focus Speed");
            spellFsm.GetAction<PlayerDataBoolTest>("Deepest Focus Speed", 0).boolName = $"equippedCharm_{CharmIDs[3]}";
            spellFsm.GetAction<FloatMultiply>("Deepest Focus Speed", 1).multiplyBy = Mathf.Pow(1.65f, 3);

            spellFsm.ChangeTransition("Deep Focus Speed", "FINISHED", "Deeper Focus Speed");
            spellFsm.ChangeTransition("Deeper Focus Speed", "FINISHED", "Deepest Focus Speed");

            #endregion

            #region Hp Amounts

            var iaa2 = new IntAdd
            {
                intVariable = spellFsmVar.FindFsmInt("Health Increase"),
                add = 2
            };
            var iaa3 = new IntAdd
            {
                intVariable = spellFsmVar.FindFsmInt("Health Increase"),
                add = 3
            };

            spellFsm.CopyState("Set HP Amount", "Set HP Amount Deeper");
            spellFsm.RemoveAction("Set HP Amount Deeper", 0);
            spellFsm.RemoveAction("Set HP Amount Deeper", 1);
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount Deeper", 0).boolName = $"equippedCharm_{CharmIDs[2]}";
            spellFsm.AddAction("Set HP Amount Deeper", iaa2);

            spellFsm.CopyState("Set HP Amount", "Set HP Amount Deepest");
            spellFsm.RemoveAction("Set HP Amount Deepest", 0);
            spellFsm.RemoveAction("Set HP Amount Deepest", 1);
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount Deepest", 0).boolName = $"equippedCharm_{CharmIDs[3]}";
            spellFsm.AddAction("Set HP Amount Deepest", iaa3);

            spellFsm.CopyState("Set HP Amount 2", "Set HP Amount 2 Deeper");
            spellFsm.RemoveAction("Set HP Amount 2 Deeper", 0);
            spellFsm.RemoveAction("Set HP Amount 2 Deeper", 1);
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount 2 Deeper", 0).boolName = $"equippedCharm_{CharmIDs[2]}";
            spellFsm.AddAction("Set HP Amount 2 Deeper", iaa2);

            spellFsm.CopyState("Set HP Amount 2", "Set HP Amount 2 Deepest");
            spellFsm.RemoveAction("Set HP Amount 2 Deepest", 0);
            spellFsm.RemoveAction("Set HP Amount 2 Deepest", 1);
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount 2 Deepest", 0).boolName = $"equippedCharm_{CharmIDs[3]}";
            spellFsm.AddAction("Set HP Amount 2 Deepest", iaa3);

            spellFsm.ChangeTransition("Set HP Amount", FsmEvent.Finished.Name, "Set HP Amount Deeper");
            spellFsm.ChangeTransition("Set HP Amount Deeper", FsmEvent.Finished.Name, "Set HP Amount Deepest");
            spellFsm.ChangeTransition("Set HP Amount 2", FsmEvent.Finished.Name, "Set HP Amount 2 Deeper");
            spellFsm.ChangeTransition("Set HP Amount 2 Deeper", FsmEvent.Finished.Name, "Set HP Amount 2 Deepest");

            #endregion

            yield break;
        }

        #region ModHooks

        private string[] _charmNames =
        {
            "Quicker Focus",
            "Quickest Focus",
            "Deeper Focus",
            "Deepest Focus",
            "Temp",
            "Temp"
        };
        private string[] _charmDescriptions =
        {
            "A dense charm containing a crystal lens.<br><br>Increases the speed of focusing SOUL, allowing the bearer to heal damage even faster.",
            "A very dense charm containing a crystal lens.<br><br>Increases the speed of focusing SOUL, allowing the bearer to heal damage faster than nothing else.",
            "Naturally formed within a crystal over a longer period. Draws in SOUL from the surrounding air.<br><br>The bearer will focus SOUL at a slower rate, but the healing effect will triple.",
            "Naturally formed within a crystal over the longest period. Draws in SOUL from the surrounding air.<br><br>The bearer will focus SOUL at a slower rate, but the healing effect will quadruple.",
            "Temp",
            "Temp"
        };
        private string OnLanguageGetHook(string key, string sheet, string orig)
        {
            if (key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (CharmIDs.Contains(charmNum))
                {
                    return _charmNames[CharmIDs.IndexOf(charmNum)];
                }
            }
            else if (key.StartsWith("CHARM_DESC_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (CharmIDs.Contains(charmNum))
                {
                    return _charmDescriptions[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }
        private bool OnGetPlayerBoolHook(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.gotCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.newCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.equippedCharms[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }
        private bool OnSetPlayerBoolHook(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.gotCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.newCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    SaveSettings.equippedCharms[CharmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            return orig;
        }
        private int OnGetPlayerIntHook(string target, int orig)
        {
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (CharmIDs.Contains(charmNum))
                {
                    return SaveSettings.charmCosts[CharmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }

        #endregion
    }
}
