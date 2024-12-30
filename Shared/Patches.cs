#if KKS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Manager;

namespace KK_MaleBreath
{
    internal class Patches
    {
        /// <summary>
        /// Removes 'forces volume change' observable from the voice loading and instead changes volume once on creation.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Manager.Voice), "Play_Standby")]
        public static IEnumerable<CodeInstruction> VoicePlay_StandbyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Good reason to remake it.
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
            }
        }
    }
}
#endif