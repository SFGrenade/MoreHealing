using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using Modding;
using MoreHealing.Consts;
using SFCore;
using UnityEngine;

namespace MoreHealing
{
    class MoreHealing : Mod<MhSettings>
    {
        public TextureStrings ts { get; private set; }
        public CharmHelper ch { get; private set; }

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
            ts = new TextureStrings();
        }

        public override void Initialize()
        {
            Log("Initializing");

            ch = new CharmHelper();
            ch.customCharms = 4;
            ch.customSprites = new Sprite[] { ts.Get(TextureStrings.QuickerFocusKey), ts.Get(TextureStrings.QuickestFocusKey), ts.Get(TextureStrings.DeeperFocusKey), ts.Get(TextureStrings.DeepestFocusKey) };

            initCallbacks();

            Log("Initialized");
        }

        private void initCallbacks()
        {
            ModHooks.Instance.GetPlayerBoolHook += OnGetPlayerBoolHook;
            ModHooks.Instance.SetPlayerBoolHook += OnSetPlayerBoolHook;
            ModHooks.Instance.GetPlayerIntHook += OnGetPlayerIntHook;
            ModHooks.Instance.AfterSavegameLoadHook += InitSaveSettings;
            ModHooks.Instance.LanguageGetHook += OnLanguageGetHook;
            //ModHooks.Instance.CharmUpdateHook += OnCharmUpdateHook;

            On.HeroController.Start += OnHeroControllerStart;
        }

        private bool changed = false;
        private void OnHeroControllerStart(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            if (changed) return;
            changed = true;

            self.StartCoroutine(addCharmStates(self));

            changed = true;
        }
        private void InitSaveSettings(SaveGameData data)
        {
            // Found in a project, might help saving, don't know, but who cares
            // Charms
            Settings.gotCharms = Settings.gotCharms;
            Settings.newCharms = Settings.newCharms;
            Settings.equippedCharms = Settings.equippedCharms;
            Settings.charmCosts = Settings.charmCosts;

            changed = false;
        }

        private IEnumerator addCharmStates(HeroController self)
        {
            yield return new WaitWhile(() => ch.charmIDs.Count < 4);

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
            spellFsm.GetAction<PlayerDataBoolTest>("Set QuickerFocus Speed", 0).boolName = $"equippedCharm_{ch.charmIDs[0]}";
            spellFsm.AddAction("Set QuickerFocus Speed", fmAction);
            spellFsm.GetAction<FloatMultiply>("Set QuickerFocus Speed", 2).multiplyBy = Mathf.Pow(2f/3f, 2);

            spellFsm.CopyState("Set Focus Speed", "Set QuickestFocus Speed");
            spellFsm.RemoveAction("Set QuickestFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickestFocus Speed", 0);
            spellFsm.RemoveAction("Set QuickestFocus Speed", 2);
            spellFsm.GetAction<PlayerDataBoolTest>("Set QuickestFocus Speed", 0).boolName = $"equippedCharm_{ch.charmIDs[1]}";
            spellFsm.AddAction("Set QuickestFocus Speed", fmAction);
            spellFsm.GetAction<FloatMultiply>("Set QuickestFocus Speed", 2).multiplyBy = Mathf.Pow(2f/3f, 3);

            spellFsm.ChangeTransition("Set Focus Speed", "FINISHED", "Set QuickerFocus Speed");
            spellFsm.ChangeTransition("Set QuickerFocus Speed", "FINISHED", "Set QuickestFocus Speed");

            #endregion

            #region Deep Focus Speeds

            spellFsm.CopyState("Deep Focus Speed", "Deeper Focus Speed");
            spellFsm.GetAction<PlayerDataBoolTest>("Deeper Focus Speed", 0).boolName = $"equippedCharm_{ch.charmIDs[2]}";
            spellFsm.GetAction<FloatMultiply>("Deeper Focus Speed", 1).multiplyBy = Mathf.Pow(1.65f, 2);

            spellFsm.CopyState("Deep Focus Speed", "Deepest Focus Speed");
            spellFsm.GetAction<PlayerDataBoolTest>("Deepest Focus Speed", 0).boolName = $"equippedCharm_{ch.charmIDs[3]}";
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
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount Deeper", 0).boolName = $"equippedCharm_{ch.charmIDs[2]}";
            spellFsm.AddAction("Set HP Amount Deeper", iaa2);

            spellFsm.CopyState("Set HP Amount", "Set HP Amount Deepest");
            spellFsm.RemoveAction("Set HP Amount Deepest", 0);
            spellFsm.RemoveAction("Set HP Amount Deepest", 1);
            spellFsm.GetAction<PlayerDataBoolTest>("Set HP Amount Deepest", 0).boolName = $"equippedCharm_{ch.charmIDs[3]}";
            spellFsm.AddAction("Set HP Amount Deepest", iaa3);

            spellFsm.ChangeTransition("Set HP Amount", FsmEvent.Finished.Name, "Set HP Amount Deeper");
            spellFsm.ChangeTransition("Set HP Amount Deeper", FsmEvent.Finished.Name, "Set HP Amount Deepest");

            #endregion
        }

        #region ModHooks

        private string[] charmNames =
        {
            "Quicker Focus",
            "Quickest Focus",
            "Deeper Focus",
            "Deepest Focus"
        };
        private string[] charmDescriptions =
        {
            "A dense charm containing a crystal lens.<br><br>Increases the speed of focusing SOUL, allowing the bearer to heal damage even faster.",
            "A very dense charm containing a crystal lens.<br><br>Increases the speed of focusing SOUL, allowing the bearer to heal damage faster than nothing else.",
            "Naturally formed within a crystal over a longer period. Draws in SOUL from the surrounding air.<br><br>The bearer will focus SOUL at a slower rate, but the healing effect will triple.",
            "Naturally formed within a crystal over the longest period. Draws in SOUL from the surrounding air.<br><br>The bearer will focus SOUL at a slower rate, but the healing effect will quadruple."
        };
        private string OnLanguageGetHook(string key, string sheet)
        {
            if (key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return charmNames[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            else if (key.StartsWith("CHARM_DESC_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return charmDescriptions[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            return Language.Language.GetInternal(key, sheet);
        }
        private bool OnGetPlayerBoolHook(string target)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return Settings.gotCharms[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return Settings.newCharms[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return Settings.equippedCharms[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            return PlayerData.instance.GetBoolInternal(target);
        }
        private void OnSetPlayerBoolHook(string target, bool val)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    Settings.gotCharms[ch.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    Settings.newCharms[ch.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    Settings.equippedCharms[ch.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            PlayerData.instance.SetBoolInternal(target, val);
        }
        private int OnGetPlayerIntHook(string target)
        {
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (ch.charmIDs.Contains(charmNum))
                {
                    return Settings.charmCosts[ch.charmIDs.IndexOf(charmNum)];
                }
            }
            return PlayerData.instance.GetIntInternal(target);
        }

        #endregion
    }
}
