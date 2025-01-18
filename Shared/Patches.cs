using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Manager;
using KKAPI.Utilities;

namespace KK_MaleBreath
{
    // Set volume once on creation of chara voice instead of updating each frame with UniRx observable.
    // All voice lines are very short and the volume externally is never manipulated by the game/plugins.
    // The only "downfall" of this - player won't see immediate volume change when editing that of particular personality through config,
    // will have to wait for another voice proc for changes to appear.

    // Why not use audioSource.minDistance ? It works awry if at all when voice is played very close/right at listener transform.

    internal class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
        public static void HSceneProcChangeAnimatorPostfix()
        {
            MaleBreathController.UpdateComponents();
        }
#if KK
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadVoice), nameof(LoadVoice.Start), MethodType.Enumerator)]
        public static void LoadVoiceStartPostfix()
        {
            LoadGameVoice.UpdateVolume();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LoadVoice), nameof(LoadVoice.Start), MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> LoadVoiceStartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var done = false;
            var injected = false;
            var nested = typeof(LoadVoice).GetNestedType("<Start>c__Iterator1", BindingFlags.NonPublic);
            if (nested == null)
            {
                yield break;
            }
            var asyncInstance = AccessTools.Field(nested, "$this");
            if (asyncInstance == null)
            {
                yield break;
            }

            foreach (var code in instructions)
            {
                if (!done)
                {
                    if (!found)
                    {
                        if (code.opcode == OpCodes.Pop) found = true;
                    }
                    else
                    {
                        if (!injected)
                        {
                            // return Ldarg_0
                            yield return code;
                            yield return new CodeInstruction(OpCodes.Ldfld, asyncInstance);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(LoadAudioBase), "audioSource"));
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Manager.Voice), "Instance"));
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, asyncInstance);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LoadVoice), "no"));
                            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.FirstMethod(typeof(Manager.Voice), m => m.Name.Equals("GetVolume")));
                            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(UnityEngine.AudioSource), "volume"));
                            injected = true;
                            continue;
                        }
                        else
                        {
                            if (code.opcode == OpCodes.Pop) done = true;
                            yield return new CodeInstruction(OpCodes.Nop);
                            continue;
                        }
                    }
                }
                yield return code;
            }
        }



#else
        /// <summary>
        /// Removes 'forces volume change' observable from the voice loading and instead changes volume once on creation.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Manager.Voice), "Play_Standby")]
        public static IEnumerable<CodeInstruction> VoicePlay_StandbyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var counter = 0;
            var done = false;
            foreach (CodeInstruction code in instructions)
            {
                bool flag = !done;
                if (flag)
                {
                    if (!found)
                    {
                        if (code.opcode == OpCodes.Callvirt && code.operand.ToString().Contains("set_pitch"))
                        {
                            found = true;
                        }
                    }
                    else
                    {
                        if (counter == 0)
                        {
                            if (code.opcode == OpCodes.Stfld)
                            {
                                counter++;
                            }
                            yield return new CodeInstruction(OpCodes.Nop);
                            continue;
                        }
                        else if (counter == 1)
                        {
                            if (code.opcode == OpCodes.Call)
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_1);
                                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Voice.Loader), "no"));
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.FirstMethod(typeof(Voice), (MethodInfo m) => m.Name.Contains("GetVolume")));
                                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(AudioSource), "volume"));
                                counter++;
                                continue;
                            }
                        }                        
                        else
                        {
                            if (code.opcode == OpCodes.Pop)
                            {
                                done = true;
                            }
                            yield return new CodeInstruction(OpCodes.Nop);
                            continue;
                        }
                    }
                }
                yield return code;
            
        }}
#endif
    }
}