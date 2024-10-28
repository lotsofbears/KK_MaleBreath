using HarmonyLib;
using KKAPI.MainGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KK_MaleBreathVR
{
    internal class MaleBreathController : GameCustomFunctionController
    {
        private static Action<string> ClickButton;
        internal static readonly List<BreathComponent> breathComponents = new List<BreathComponent>();
        private static ClickType _queuedClick = ClickType.None;
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            var traverse = Traverse.Create(proc);
            var male = traverse.Field("male").GetValue<ChaControl>();
            MaleBreath.Logger.LogDebug($"OnStartH:{male}");
            if (male == null) return;

            BreathComponent._hFlag = hFlag;
            BreathComponent._lstFemale = traverse.Field("lstFemale").GetValue<List<ChaControl>>();
            BreathComponent._handCtrl = traverse.Field("hand").GetValue<HandCtrl>();
            breathComponents.Add(male.gameObject.AddComponent<BreathComponent>());
            var type = AccessTools.TypeByName("KK_SensibleH.AutoMode.LoopController");
            if (type != null)
            {
                ClickButton = AccessTools.MethodDelegate<Action<string>>(AccessTools.FirstMethod(type, m => m.Name.Equals("ClickButton")));
            }
        }
        /// <returns>True if we want to run our side first.</returns>
        public static bool ButtonClick(int button)
        {
            MaleBreath.Logger.LogDebug($"ButtonClick:{button}:{_queuedClick}");
            if (_queuedClick != ClickType.None)
            {
                _queuedClick = ClickType.None;
            }
            else
            {
                var click = (ClickType)button;
                if (!click.ToString().Contains("_novoice") && breathComponents[0].SetTriggerVoice(GetVoiceType(click)))
                {
                    _queuedClick = click;
                    return true;
                }
            }
            return false;
        }
        private static LoadVoice.VoiceType GetVoiceType(ClickType clickType)
        {
            return clickType switch
            {
                ClickType.Insert => LoadVoice.VoiceType.Insert,
                ClickType.InsertAnal => LoadVoice.VoiceType.InsertAnal,
                ClickType.Inside => LoadVoice.VoiceType.BeforeClimaxSelf,
                ClickType.Outside => LoadVoice.VoiceType.BeforeClimaxSelf,
                _ => LoadVoice.VoiceType.Idle
            };

        }
        public static void Click()
        {
            MaleBreath.Logger.LogDebug($"AttemptToClick:{_queuedClick}");
            if (_queuedClick != ClickType.None && ClickButton != null)
            {
                ClickButton(_queuedClick.ToString());
            }
        }
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
        
    }
}
