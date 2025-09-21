using HarmonyLib;
using KKAPI.MainGame;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK_MaleBreath
{
    internal class MaleBreathController : GameCustomFunctionController
    {
        private static MaleBreathController _instance;
        private static Action<string> ClickButton;
        private static bool _vr;
        private static bool _pov;
        private static ChaControl _povChara;
        private static ClickType _queuedClick = ClickType.None;
        private Harmony _patch;

        internal static bool IsPov => _pov;
        internal static bool IsVR => _vr;
        internal static ChaControl GetPovChara => _povChara;
        internal static bool IsEnabled => MaleBreath.Enable.Value == MaleBreath.EnableState.Always || (IsVR && MaleBreath.Enable.Value == MaleBreath.EnableState.OnlyInVr);

        // Button names in-game, in proper order.
        enum ClickType
        {
            None = -1,
            Insert,
            Insert_novoice,
            InsertAnal,
            InsertAnal_novoice,
            Inside,
            Outside,
            Pull_novoice,
            Insert_female,
            Insert_novoice_female,
            InsertAnal_female,
            InsertAnal_novoice_female,
            Inside_female,
            Outside_female,
            Pull_novoice_female,
            InserDark,
            Insert_novoiceDark,
            InsertAnalDark,
            InsertAnal_novoiceDark,
            InsideDark,
            OutsideDark,
            Pull_novoiceDark
        }

        private void Start()
        {
            _instance = this;
            var type = AccessTools.TypeByName("KK_SensibleH.AutoMode.LoopController");
            if (type != null)
            {
                ClickButton = AccessTools.MethodDelegate<Action<string>>(AccessTools.FirstMethod(type, m => m.Name.Equals("ClickButton")));
            }
            MaleBreath.Enable.SettingChanged += (sender, e) => SettingChanged_Enable();
            SettingChanged_Enable();
        }

        private void SettingChanged_Enable()
        {
            if (IsEnabled)
            {
                if (_patch == null)
                {
                    _patch = Harmony.CreateAndPatchAll(typeof(Patches));
                    if (BreathComponent.hFlag != null && BreathComponent.instances.Count == 0)
                    {
                        AddComponent(BreathComponent.male);
                    }
                }
            }
            else
            {
                if (_patch != null)
                {
                    _patch.UnpatchSelf();
                    _patch = null;
                    if (BreathComponent.instances.Count > 0)
                    {
                        foreach (var instance in BreathComponent.instances)
                        {
                            Component.Destroy(instance);
                        }
                    }
                }
            }
        }

        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            if (!_vr)
            {
                // On 'Start' VR isn't up yet to look up it's instance.
                var vrType = AccessTools.TypeByName("KK_VR.Interpreters.KoikGameInterp");
                if (vrType != null)
                {
                    //var behavior = (MonoBehaviour)GetComponent(vrType);
                    _vr = FindObjectOfType(vrType) != null;
                }
            }
            
            var traverse = Traverse.Create(proc);
            BreathComponent.hFlag = hFlag;
            BreathComponent.male = traverse.Field("male").GetValue<ChaControl>();
            BreathComponent.lstFemale = traverse.Field("lstFemale").GetValue<List<ChaControl>>();
            BreathComponent.handCtrl = traverse.Field("hand").GetValue<HandCtrl>();

            if (IsEnabled)
            {
                AddComponent(BreathComponent.male);
            }
        }

        internal static void UpdateComponents(bool destroy = true)
        {
            // We update components on each new animController to reset voices/states and track visibility of the character.
            if (destroy)
            {
                var charas = new List<ChaControl>();

                foreach (var instance in BreathComponent.instances)
                {
                    charas.Add(instance.GetComponent<ChaControl>());
                }

                foreach (var chara in charas)
                {
                    if (chara == null) continue;

                    AddComponent(chara);
                }
            }
            else
            {
                BreathComponent.OnChangeAnimator();
            }
                
#if DEBUG
            MaleBreath.Logger.LogInfo($"UpdateComponents");
#endif
        }


        private static void AddComponent(ChaControl chara)
        {
            if (chara == null) return;

            var component = chara.GetComponent<BreathComponent>();
            if (component != null)
            {
                Destroy(component);
            }

            if (BreathComponent.hFlag != null && IsModeAvailable(BreathComponent.hFlag.mode))
            {
                chara.gameObject.AddComponent<BreathComponent>();
            }

        }

        internal static bool IsModeAvailable(HFlag.EMode mode)
        {
            return mode switch
            {
                HFlag.EMode.aibu => MaleBreath.RunInAibu.Value,
                HFlag.EMode.lesbian or HFlag.EMode.peeping or HFlag.EMode.masturbation or HFlag.EMode.none => false,
                _ => true
            };
        }

        private static void AddComponent(IEnumerable<ChaControl> charas)
        {
            foreach (var chara in charas)
            {
                AddComponent(chara);
            }
        }

        // Hook for SensibleH to run male voice (this side) on button click.
        // If we play the voice and it's up, we inform SensibleH to continue as usual.

        /// <returns>True if we want to run our side first.</returns>
        public static bool ButtonClick(int button)
        {
            if (IsEnabled)
            {
#if DEBUG
                MaleBreath.Logger.LogInfo($"ButtonClick:{(ClickType)button}:{_queuedClick}");
#endif
                if (_queuedClick != ClickType.None)
                {
                    _queuedClick = ClickType.None;
                }
                else
                {
                    var click = (ClickType)button;
                    var voiceType = GetVoiceType(click);
                    if (voiceType != LoadGameVoice.VoiceType.Idle && BreathComponent.instances.Count > 0 && BreathComponent.instances[0].SetTriggerVoice(voiceType))
                    {
                        CorrectClick(ref click);
                        _queuedClick = click;
                        return true;
                    }
                }
            }
            return false;
        }
 
        private static void CorrectClick(ref ClickType clickType)
        {
            if (clickType == ClickType.Inside) clickType = ClickType.None;
        }
        private static LoadGameVoice.VoiceType GetVoiceType(ClickType clickType)
        {
            return clickType switch
            {
                ClickType.Insert_novoice or ClickType.InsertAnal_novoice => LoadGameVoice.VoiceType.Exclamation,
                ClickType.Insert => LoadGameVoice.VoiceType.Insert,
                ClickType.InsertAnal => LoadGameVoice.VoiceType.InsertAnal,
                ClickType.Inside => LoadGameVoice.VoiceType.BeforeClimaxSelf,
                _ => LoadGameVoice.VoiceType.Idle
            };
        }

        public static void Click()
        {
#if DEBUG
            MaleBreath.Logger.LogDebug($"AttemptToClick:{_queuedClick}");
#endif
            if (ClickButton != null)
            {
                ClickButton(_queuedClick.ToString());
            }
        }
        private static void DestroyComponents()
        {
            foreach (var inst in BreathComponent.instances)
            {
                Destroy(inst);
            }
        }
        internal static void OnPov(bool active, ChaControl chara)
        {
#if DEBUG
            MaleBreath.Logger.LogInfo($"OnPov:active = {active}, chara = {chara}");
#endif
            if (BreathComponent.hFlag != null)
            {
                _pov = active;
                _povChara = chara;
                if (active)
                {
                    if (BreathComponent.hFlag.mode == HFlag.EMode.aibu)
                    {
                        DestroyComponents();
                    }
                    else
                    {
                        UpdateComponents(false);
                    }
                }
                else
                {
                    UpdateComponents(false);
                }
            }
        }
    }
}
