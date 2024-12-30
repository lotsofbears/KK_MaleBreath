using BepInEx;
using Illusion;
using Illusion.Game;
using Manager;
using ParadoxNotion.Services;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UniRx;
using UnityEngine;
using static Illusion.Utils;
using static KK_MaleBreath.MaleBreath;

namespace KK_MaleBreath
{
    internal static class LoadVoice
    {
        internal static string lastLoadedAsset;
        private static string _path = "sound/data/pcm/c{0:00}/";
        //private static readonly Dictionary<int, string> extraPersonalities = new Dictionary<int, string>()
        //{
        //    { 30, "14" },
        //    { 31, "15" },
        //    { 32, "16" },
        //    { 33, "17" },
        //    { 34, "20" },
        //    { 35, "20" },
        //    { 36, "20" },
        //    { 37, "20" },
        //    { 38, "50" }
        //};


        public enum HMode
        {
            None = -1,
            Aibu,
            Houshi,
            Sonyu
        }
        public enum VoiceType
        {
            Idle,
            Insert,
            InsertAnal,
            InsertIdle,
            InsertIdleAnal,
            ClimaxSelf,
            ClimaxBoth,
            BeforeClimaxSelf,
            BeforeClimaxBoth,
            AfterClimaxSelf,
            AfterClimaxBoth,
            LoopSlow,
            LoopFast,
            LateLoopPartnerSlow,
            LateLoopPartnerFast,
            LateLoopSelfSlow,
            LateLoopSelfFast,
            LateLoopBothSlow,
            LateLoopBothFast,
            KissDuringLoopSlow,
            KissDuringLoopFast,
            Exclamation,
        }
        public enum BreathType
        {
            Normal,
            StrainedToNormal,
            Strained,
            ResistWeakSlow,
            ResistWeakFast,
            ResistStrongSlow,
            ResistStrongFast,
            LoopWeakSlow,
            LoopWeakFast,
            LoopStrongSlow,
            LoopStrongFast,
            LateLoopWeakSlow,
            LateLoopWeakFast,
            LateLoopStrongSlow,
            LateLoopStrongFast,
            KissExclamation,
            KissSlow,
            KissFast,
           // KissStrongSlow,
            //KissStrongFast,
            KissDuringLoopWeakSlow,
            KissDuringLoopWeakFast,
            KissDuringLoopStrongSlow,
            KissDuringLoopStrongFast,
            LickSlow,
            LickFast,
            //LickWeakSlow,
            //LickWeakFast,
            //LickStrongSlow,
            //LickStrongFast,
            SuckWeakSlow,
            SuckWeakFast,
            SuckStrongSlow,
            SuckStrongFast,
            Gasp,


        }
        struct VoiceEntry
        {
            internal string bundle;
            internal string asset;
            internal int hExp;
        }
        private static string GetBundleName(int id, bool h)
        {
#if KK
            return id switch
            {
                30 => "14",
                31 => "15",
                32 => "16",
                33 => "17",
                34 or 35 or 36 or 37 => "20",
                38 => "50",
                _ => "00"
            };
#else
            return id switch
            {
                40 or 41 or 42 or 43 => h ? "71" : "70",
                _ => h ? "01" : "00"
            };
#endif
        }
        private static string GetBundle(int id, bool hVoice)
        {
            return GetBundleName(id, hVoice) +
#if KK
                (hVoice ? "_00.unity3d" : ".unity3d");
#else
                ".unity3d";
#endif
        }
        public static Transform PlayBreath(BreathType breathType, ChaControl chara = null, Transform breathTransform = null)//, bool setCooldown)
        {
            if (chara == null && breathTransform == null) return null;
            var voiceList = GetBreathList(breathType);

            //var hExp = Game.Instance.HeroineList
            //    .Where(heroine => heroine.chaCtrl == chara)
            //    .Select(heroine => heroine.HExperience)
            //    .FirstOrDefault();

            var personalityId = (int)MaleBreath.PlayerPersonality.Value;
            var hExp = MaleBreath.PreferredBreathExperience.Value;
            if (hExp != MaleBreath.HExp.Any)
            {
                if (hExp ==  MaleBreath.HExp.不慣れ)
                {
                    hExp =  MaleBreath.HExp.初めて;
                }
                var sortedList = voiceList
                    .Where(s => s.Contains("}_0" + (int)hExp))
                    .ToList();
                if (sortedList.Count > 0) voiceList = sortedList;
            }
            var bundle = _path + voiceList[UnityEngine.Random.Range(0, voiceList.Count)];

            // Right now string format is "sound/data/pcm/c{0:00}/h/h_ko_{0:00}_00_000"
            // Replace personality id.
            bundle = string.Format(bundle, personalityId);

            var index = bundle.LastIndexOf('/');

            // Extract Asset from the string at the end.
            var asset = bundle.Substring(index + 1);

            // Remove it from the string.
            bundle = bundle.Remove(index + 1);

            var h = bundle.EndsWith("h/", StringComparison.OrdinalIgnoreCase);
            bundle += GetBundle(personalityId, hVoice: h);
            MaleBreath.Logger.LogDebug($"LoadBreath:{bundle}:{asset}");

            var setting = new Illusion.Game.Utils.Voice.Setting
            {
                no = personalityId,
                assetBundleName = bundle,
                assetName = asset,
                pitch = chara == null ? 1f : chara.fileParam.voicePitch,
                voiceTrans = chara == null ? breathTransform : chara.dictRefObj[ChaReference.RefObjKey.a_n_mouth].transform,

            };

            //chara.ChangeMouthPtn(0, true);
#if KK

            breathTransform = Illusion.Game.Utils.Voice.OnecePlayChara(setting);
            breathTransform.gameObject.GetComponent<AudioSource>().minDistance = MaleBreath.Volume.Value;
            if (chara != null)
            {
                chara.SetVoiceTransform(breathTransform);
            }
#else
            var audioSource = Illusion.Game.Utils.Voice.OncePlayChara(setting);
            if (audioSource == null)
            {
                MaleBreath.Logger.LogWarning($"Couldn't find specified voice:{bundle}:{asset}");
            }
            else
            {
                audioSource.volume = MaleBreath.Volume.Value;
                breathTransform = audioSource.transform;
                if (chara != null)
                {
                    chara.SetLipSync(audioSource);
                }
            }
#endif
            return breathTransform;
        }
        public static Transform PlayVoice(VoiceType voiceType, ChaControl chara = null, Transform voiceTransform = null)
        {
            if (chara == null && voiceTransform == null) return null;
            var hMode = GetHMode();
            if (hMode == HMode.None) return null;
            var personalityId = GetPlayerPersonality();
            if (!voiceDic.ContainsKey(personalityId)) return null;
            var voiceEntryList = voiceDic[personalityId][(int)hMode][(int)voiceType];
            if (voiceEntryList.Count == 0) return null;

            var hExp = (int)MaleBreath.PreferredVoiceExperience.Value;
            if (hExp != -1)
            {
                var sortedList = FindExperience(voiceEntryList, hExp);
                while (hExp > 0)
                {
                    if (sortedList.Count == 0 || (sortedList.Count == 1 && sortedList[0].asset.Equals(BreathComponent.instances[0].lastVoiceName)))
                    {
                        --hExp;
                        sortedList = FindExperience(voiceEntryList, hExp);
                        continue;
                    }
                    voiceEntryList = sortedList;
                    break;
                }
            }
            if (voiceEntryList.Count > 1)
            {
                voiceEntryList = voiceEntryList
                    .Where(e => !e.asset.Equals(BreathComponent.instances[0].lastVoiceName))
                    .ToList();
            }
            var voiceEntry = voiceEntryList[UnityEngine.Random.Range(0, voiceEntryList.Count)];
            var bundle = voiceEntry.bundle;
            lastLoadedAsset = voiceEntry.asset;
            MaleBreath.Logger.LogDebug($"LoadVoice:{bundle}:{lastLoadedAsset}");
            var setting = new Illusion.Game.Utils.Voice.Setting
            {
                no = personalityId,
                assetBundleName = bundle,
                assetName = lastLoadedAsset,
                pitch = chara == null ? 1f : chara.fileParam.voicePitch,
                voiceTrans = chara == null ? voiceTransform : chara.objHead.transform
            };
#if KK
            voiceTransform = Illusion.Game.Utils.Voice.OnecePlayChara(setting);
            voiceTransform.gameObject.GetComponent<AudioSource>().minDistance = MaleBreath.Volume.Value;
            if (chara != null)
            {
                chara.SetVoiceTransform(voiceTransform);
            }
#else
            var audioSource = Illusion.Game.Utils.Voice.OncePlayChara(setting);
            if (audioSource == null)
            {
                MaleBreath.Logger.LogWarning($"Couldn't find specified voice:{bundle}:{lastLoadedAsset}");
            }
            else
            {
                audioSource.volume = MaleBreath.Volume.Value;
                voiceTransform = audioSource.transform;
                if (chara != null)
                {
                    chara.SetLipSync(audioSource);
                }
            }
#endif
            return voiceTransform;
        }
        private static List<VoiceEntry> FindExperience(List<VoiceEntry> voiceEntryList, int hExp)
        {
            var sortedList = new List<VoiceEntry>();
            foreach (var entry in voiceEntryList)
            {
                if (entry.hExp == hExp || entry.hExp == -1) sortedList.Add(entry);
            }
            return sortedList;
        }
        private static HMode GetHMode()
        {
            switch (BreathComponent.hFlag.mode)
            {
                case HFlag.EMode.aibu:
                    return HMode.Aibu;
                case HFlag.EMode.houshi:
                case HFlag.EMode.houshi3P:
                case HFlag.EMode.houshi3PMMF:
                    return HMode.Houshi;
                case HFlag.EMode.sonyu:
                case HFlag.EMode.sonyu3P:
                case HFlag.EMode.sonyu3PMMF:
                    return HMode.Sonyu;
                default:
                    return HMode.None;
            }
        }
        // Structure <PersonalityId, HModes<VoiceTypes<BundleAssets as string separated by ','>>>
        private static Dictionary<int, List<List<List<VoiceEntry>>>> voiceDic = [];

        public static void Initialize()
        {
            var voiceTypeEnum = Enum.GetNames(typeof(VoiceType));
            var hModeEnum = Enum.GetNames(typeof(HMode));
            var files = Directory.GetFiles(System.IO.Path.GetDirectoryName(typeof(MaleBreath).Assembly.Location), "*.txt");
            //MaleBreath.Logger.LogDebug($"Files:Found:{files.Length}");
            foreach (var file in files)
            {
                ReadFile(file, voiceTypeEnum, hModeEnum);
            }
        }
        private static void ReadFile(string file, string[] voiceTypeEnum, string[] hModeEnum)
        {
            var numbersInName = Regex.Match(file, @"\d+").Value;
            if (numbersInName.Count() == 0 ) return;

            var personalityId = Int32.Parse(numbersInName);
            if (personalityId < 0 || personalityId > 38) return;
            MaleBreath.Logger.LogDebug($"ReadFile:{personalityId}");
            // We separate everything by lines
            var array = System.IO.File.ReadAllText(file)
                .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0) return;

            // Populate dic
            if (!voiceDic.ContainsKey(personalityId))
            {
                voiceDic.Add(personalityId, new List<List<List<VoiceEntry>>>());
                var dic = voiceDic[personalityId];
                for (var j = 0; j < hModeEnum.Length; j++)
                {
                    dic.Add(new List<List<VoiceEntry>>());
                    for (var i = 0; i < voiceTypeEnum.Length; i++)
                    {
                        dic[j].Add(new List<VoiceEntry>());
                    }
                }
            }
            // We parse each line separately. Wait for the line with the exact name from enum,
            // and start to dump consequent lines in appropriate category until we meet the next line with enum name.
            var currentHMode = -1;
            var currentVoiceType = -1;
            var personalitySubstring = personalityId + "_";
            foreach (var item in array)
            {
                var trim = item.Replace(" ", string.Empty);
                trim = trim.Replace("\t", string.Empty);

                if (trim.StartsWith("//", StringComparison.Ordinal)) continue;
                if (hModeEnum.Any(s => s.Equals(trim)))
                {
                    currentHMode = Array.IndexOf(hModeEnum, trim);
                    MaleBreath.Logger.LogDebug($"Dic:Mode:{(HMode)currentHMode}:{trim}");
                }
                else if (voiceTypeEnum.Any(s => s.Equals(trim)))
                {
                    currentVoiceType = Array.IndexOf(voiceTypeEnum, trim);
                    MaleBreath.Logger.LogDebug($"Dic:Category:{(VoiceType)currentVoiceType}:{trim}");
                }
                else
                {
                    //MaleBreath.Logger.LogDebug($"Dic:Add:{(HMode)currentHMode}:{(VoiceType)currentVoiceType}");
                    if (currentHMode == -1 || currentVoiceType == -1) continue;

                    var split = trim.Split(',');
                    if (split.Length < 2)
                    {
                        MaleBreath.Logger.LogWarning($"Invalid line in .txt file of {personalityId} personality:\n" +
                            $"{item}");
                        continue;
                    }
                    if (AddVoiceEntry(split, personalitySubstring, out var voiceEntry))
                    {
                        voiceDic[personalityId][currentHMode][currentVoiceType].Add(voiceEntry);
                    }
                }
            }
        }
        private static bool AddVoiceEntry(string[] strings, string personalitySubstring, out VoiceEntry voiceEntry)
        {
            voiceEntry = new VoiceEntry();
            var personalitySubstringCount = personalitySubstring.Count();
            for (var i = 0; i < 2; i++)
            {
                if (strings[i].Length == 0 || IsComment(strings[i])) return false;
            }
            voiceEntry.bundle = strings[0];
            voiceEntry.asset = strings[1];

            if (strings.Length > 2 && strings[2].Length > 0 && !IsComment(strings[2])
                && int.TryParse(strings[2], out var result) && result >= -1 && result < 4)
            {
                voiceEntry.hExp = result;
            }
            else if (strings[0].EndsWith("_00.unity3d", StringComparison.Ordinal) && strings[0].Contains("/h/")
                && int.TryParse(strings[1].Substring(strings[1].LastIndexOf(personalitySubstring) + personalitySubstringCount, 2), out var number)
                && number >= 0 && number < 4)
            {
                // No explicit exp stated.
                // We look for personality index in format of "27_" and grab next two numbers, in h they are always h experience.
                voiceEntry.hExp = number;
            }
            else
            {
                voiceEntry.hExp = -1;
            }
            MaleBreath.Logger.LogDebug($"{voiceEntry.bundle}:{voiceEntry.asset}:{voiceEntry.hExp}");
            return true;
        }
        private static bool IsComment(string str) => str.StartsWith("//", StringComparison.Ordinal);
        //private static readonly Regex sWhitespace = new Regex(@"\s+");
        //public static string ReplaceWhitespace(string input, string replacement)
        //{
        //    return sWhitespace.Replace(input, replacement);
        //}
       
        private static List<string> GetBreathList(BreathType type)
        {
            return type switch
            {
                BreathType.Normal => breathNormal,
                BreathType.Strained => breathStrained,
                BreathType.StrainedToNormal => breathStrainedToNormal,
                BreathType.Gasp => breathGasp,

                BreathType.ResistWeakSlow => breathResistWeakSlow,
                BreathType.ResistWeakFast => breathResistWeakFast,
                BreathType.ResistStrongSlow => breathResistStrongSlow,
                BreathType.ResistStrongFast => breathResistStrongFast,

                BreathType.LoopWeakSlow => breathLoopWeakSlow,
                BreathType.LoopWeakFast => breathLoopWeakFast,
                BreathType.LoopStrongSlow => breathLoopStrongSlow,
                BreathType.LoopStrongFast => breathLoopStrongFast,

                BreathType.LateLoopWeakSlow => breathLateLoopWeakSlow,
                BreathType.LateLoopWeakFast => breathLateLoopWeakFast,
                BreathType.LateLoopStrongSlow => breathLateLoopStrongSlow,
                BreathType.LateLoopStrongFast => breathLateLoopStrongFast,

                BreathType.KissExclamation => breathKissExclamation,
                BreathType.KissSlow => breathKissSlow,
                BreathType.KissFast => breathKissFast,

                BreathType.KissDuringLoopWeakSlow => breathKissDuringLoopWeakSlow,
                BreathType.KissDuringLoopWeakFast => breathKissDuringLoopWeakFast,
                BreathType.KissDuringLoopStrongSlow => breathKissDuringLoopStrongSlow,
                BreathType.KissDuringLoopStrongFast => breathKissDuringLoopStrongFast,

                BreathType.LickSlow => breathLickWeakSlow,
                BreathType.LickFast => breathLickWeakFast,
                //BreathType.LickStrongSlow => breathLickStrongSlow,
                //BreathType.LickStrongFast => breathLickStrongFast,

                BreathType.SuckWeakSlow => breathSuckWeakSlow,
                BreathType.SuckWeakFast => breathSuckWeakFast,
                BreathType.SuckStrongSlow => breathSuckStrongSlow,
                BreathType.SuckStrongFast => breathSuckStrongFast,
                _ => null
            };
        }
        /*
         * Voices for
         *     Insert confirmation
         *     Climax
         *     After Climax
         *     All loops (W / S+O)
         *     Late loops
         * 
         */
        private static readonly List<string> breathStrainedToNormal = new List<string>()
        {
            "h/h_ko_{0:00}_00_000",
        };
        private static readonly List<string> breathNormal = new List<string>()
        {
            "h/h_ko_{0:00}_00_001",
            "h/h_ko_{0:00}_00_002",

            "h/h_ko_{0:00}_03_001",
            "h/h_ko_{0:00}_03_002",
        };

        private static readonly List<string> breathStrained = new List<string>()
        {
            "h/h_ko_{0:00}_00_003",
            "h/h_ko_{0:00}_00_004",
            "h/h_ko_{0:00}_00_021",
            "h/h_ko_{0:00}_00_022",

            "h/h_ko_{0:00}_03_003",
            "h/h_ko_{0:00}_03_004",
            "h/h_ko_{0:00}_03_021",
            "h/h_ko_{0:00}_03_022",
            //"h/h_ko_{0:00}_03_073",
        };
        private static readonly List<string> breathKissDuringLoopWeakSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_083",
            "h/h_ko_{0:00}_00_084",

            "h/h_ko_{0:00}_02_083",
            "h/h_ko_{0:00}_02_084",

            "h/h_ko_{0:00}_03_083",
            "h/h_ko_{0:00}_03_084",
        };
        private static readonly List<string> breathKissDuringLoopWeakFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_085",
            "h/h_ko_{0:00}_00_086",

            "h/h_ko_{0:00}_02_085",
            "h/h_ko_{0:00}_02_086",

            "h/h_ko_{0:00}_03_085",
            "h/h_ko_{0:00}_03_086",

        };
        private static readonly List<string> breathKissDuringLoopStrongSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_087",
            "h/h_ko_{0:00}_00_088",

            "h/h_ko_{0:00}_02_087",
            "h/h_ko_{0:00}_02_088",

            "h/h_ko_{0:00}_03_087",
            "h/h_ko_{0:00}_03_088",
        };
        private static readonly List<string> breathKissDuringLoopStrongFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_089",
            "h/h_ko_{0:00}_00_090",

            "h/h_ko_{0:00}_02_089",
            "h/h_ko_{0:00}_02_090",

            "h/h_ko_{0:00}_03_089",
            "h/h_ko_{0:00}_03_090",
        };
        /*
         * 69 - Mouth Finish
         */
        private static readonly List<string> breathAfterClimax = new List<string>()
        {
            "h/h_ko_{0:00}_00_091",
            "h/h_ko_{0:00}_00_092",
            "h/h_ko_{0:00}_00_093",
            "h/h_ko_{0:00}_00_094",
            "h/h_ko_{0:00}_00_095",
            "h/h_ko_{0:00}_00_096",

            "h/h_ko_{0:00}_03_091",
            "h/h_ko_{0:00}_03_092",
            "h/h_ko_{0:00}_03_093",
            "h/h_ko_{0:00}_03_094",
            "h/h_ko_{0:00}_03_095",
            "h/h_ko_{0:00}_03_096",
        };

        private static readonly List<string> breathLateLoopWeakSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_065",

            "h/h_ko_{0:00}_03_065"
        };
        private static readonly List<string> breathLateLoopWeakFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_066",

            "h/h_ko_{0:00}_03_066"
        };
        private static readonly List<string> breathLateLoopStrongSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_067",

            "h/h_ko_{0:00}_03_067",
        };
        private static readonly List<string> breathLateLoopStrongFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_068",

            "h/h_ko_{0:00}_03_068",
        };
        private static readonly List<string> breathLoopWeakSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_057",
            "h/h_ko_{0:00}_00_058",

            "h/h_ko_{0:00}_03_057",
            "h/h_ko_{0:00}_03_058",
        };
        private static readonly List<string> breathLoopWeakFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_059",
            "h/h_ko_{0:00}_00_060",

            "h/h_ko_{0:00}_03_059",
            "h/h_ko_{0:00}_03_060",
        };
        private static readonly List<string> breathLoopStrongSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_061",
            "h/h_ko_{0:00}_00_062",

            "h/h_ko_{0:00}_03_061",
            "h/h_ko_{0:00}_03_062",
        };
        private static readonly List<string> breathLoopStrongFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_063",
            "h/h_ko_{0:00}_00_064",

            "h/h_ko_{0:00}_03_063",
            "h/h_ko_{0:00}_03_064",
        };
        private static readonly List<string> breathResistWeakSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_009",
            "h/h_ko_{0:00}_00_010",
            "h/h_ko_{0:00}_00_075",
            "h/h_ko_{0:00}_00_076",


            "h/h_ko_{0:00}_02_009",
            "h/h_ko_{0:00}_02_010",
            "h/h_ko_{0:00}_02_075",
            "h/h_ko_{0:00}_02_076",

            "h/h_ko_{0:00}_03_009",
            "h/h_ko_{0:00}_03_010",
            "h/h_ko_{0:00}_03_075",
            "h/h_ko_{0:00}_03_076",
        };
        private static readonly List<string> breathResistWeakFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_011",
            "h/h_ko_{0:00}_00_012",
            "h/h_ko_{0:00}_00_077",
            "h/h_ko_{0:00}_00_078",

            "h/h_ko_{0:00}_02_011",
            "h/h_ko_{0:00}_02_012",
            "h/h_ko_{0:00}_02_077",
            "h/h_ko_{0:00}_02_078",

            "h/h_ko_{0:00}_03_011",
            "h/h_ko_{0:00}_03_012",
            "h/h_ko_{0:00}_03_077",
            "h/h_ko_{0:00}_03_078",
        };
        private static readonly List<string> breathResistStrongSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_079",
            "h/h_ko_{0:00}_00_080",

            "h/h_ko_{0:00}_02_079",
            "h/h_ko_{0:00}_02_080",

            "h/h_ko_{0:00}_03_079",
            "h/h_ko_{0:00}_03_080",
        };
        private static readonly List<string> breathResistStrongFast =
        [
            "h/h_ko_{0:00}_00_081",
            "h/h_ko_{0:00}_00_082",

            "h/h_ko_{0:00}_02_081",
            "h/h_ko_{0:00}_02_082",

            "h/h_ko_{0:00}_03_081",
            "h/h_ko_{0:00}_03_082",
        ];

        private static readonly List<string> breathKissExclamation =
        [
            "h/h_ko_{0:00}_00_023_00",
            "h/h_ko_{0:00}_00_023_01",
            "h/h_ko_{0:00}_00_023_02",
            "h/h_ko_{0:00}_00_023_03",
            "h/h_ko_{0:00}_00_023_04",
            "h/h_ko_{0:00}_00_023_05",
            "h/h_ko_{0:00}_00_023_06",
            "h/h_ko_{0:00}_00_023_07",
            "h/h_ko_{0:00}_00_023_08",
            "h/h_ko_{0:00}_00_023_09",
            "h/h_ko_{0:00}_00_024_00",
            "h/h_ko_{0:00}_00_024_01",
            "h/h_ko_{0:00}_00_024_02",
            "h/h_ko_{0:00}_00_024_03",
            "h/h_ko_{0:00}_00_024_04",
            "h/h_ko_{0:00}_00_024_05",
            "h/h_ko_{0:00}_00_024_06",
            "h/h_ko_{0:00}_00_024_07",
            "h/h_ko_{0:00}_00_024_08",
            "h/h_ko_{0:00}_00_024_09",

            "h/h_ko_{0:00}_02_023_00",
            "h/h_ko_{0:00}_02_023_01",
            "h/h_ko_{0:00}_02_023_02",
            "h/h_ko_{0:00}_02_023_03",
            "h/h_ko_{0:00}_02_023_04",
            "h/h_ko_{0:00}_02_023_05",
            "h/h_ko_{0:00}_02_023_06",
            "h/h_ko_{0:00}_02_023_07",
            "h/h_ko_{0:00}_02_023_08",
            "h/h_ko_{0:00}_02_023_09",
            "h/h_ko_{0:00}_02_024_00",
            "h/h_ko_{0:00}_02_024_01",
            "h/h_ko_{0:00}_02_024_02",
            "h/h_ko_{0:00}_02_024_03",
            "h/h_ko_{0:00}_02_024_04",
            "h/h_ko_{0:00}_02_024_05",
            "h/h_ko_{0:00}_02_024_06",
            "h/h_ko_{0:00}_02_024_07",
            "h/h_ko_{0:00}_02_024_08",
            "h/h_ko_{0:00}_02_024_09",

            "h/h_ko_{0:00}_03_023_00",
            "h/h_ko_{0:00}_03_023_01",
            "h/h_ko_{0:00}_03_023_02",
            "h/h_ko_{0:00}_03_023_03",
            "h/h_ko_{0:00}_03_023_04",
            "h/h_ko_{0:00}_03_023_05",
            "h/h_ko_{0:00}_03_023_06",
            "h/h_ko_{0:00}_03_023_07",
            "h/h_ko_{0:00}_03_023_08",
            "h/h_ko_{0:00}_03_023_09",
            "h/h_ko_{0:00}_03_024_00",
            "h/h_ko_{0:00}_03_024_01",
            "h/h_ko_{0:00}_03_024_02",
            "h/h_ko_{0:00}_03_024_03",
            "h/h_ko_{0:00}_03_024_04",
            "h/h_ko_{0:00}_03_024_05",
            "h/h_ko_{0:00}_03_024_06",
            "h/h_ko_{0:00}_03_024_07",
            "h/h_ko_{0:00}_03_024_08",
            "h/h_ko_{0:00}_03_024_09",
        ];
        private static readonly List<string> breathKissSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_013",
            "h/h_ko_{0:00}_00_017",

            "h/h_ko_{0:00}_02_013",
            "h/h_ko_{0:00}_02_017",

            "h/h_ko_{0:00}_03_015",
            "h/h_ko_{0:00}_03_019",
        };
        private static readonly List<string> breathKissFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_014",
            "h/h_ko_{0:00}_00_018",

            "h/h_ko_{0:00}_02_014",
            "h/h_ko_{0:00}_02_018",

            "h/h_ko_{0:00}_03_016",
            "h/h_ko_{0:00}_03_020",
        };
        //private static readonly List<string> breathKissStrongSlow = new List<string>()
        //{
        //    "h/h_ko_{0:00}_00_017",

        //    "h/h_ko_{0:00}_02_017",

        //    "h/h_ko_{0:00}_03_019",

        //};
        //private static readonly List<string> breathKissStrongFast = new List<string>()
        //{
        //    "h/h_ko_{0:00}_00_018",

        //    "h/h_ko_{0:00}_02_018",

        //    "h/h_ko_{0:00}_03_020",
        //};
        private static readonly List<string> breathLickWeakSlow =
        [
            "h/h_ko_{0:00}_00_025",
            "h/h_ko_{0:00}_00_026",

            "h/h_ko_{0:00}_00_029",
            "h/h_ko_{0:00}_00_030",

            "h/h_ko_{0:00}_03_025",
            "h/h_ko_{0:00}_03_026",

            "h/h_ko_{0:00}_03_029",
            "h/h_ko_{0:00}_03_030",
        ];
        private static readonly List<string> breathLickWeakFast =
        [
            "h/h_ko_{0:00}_00_027",
            "h/h_ko_{0:00}_00_028",

            "h/h_ko_{0:00}_00_031",
            "h/h_ko_{0:00}_00_032",

            "h/h_ko_{0:00}_03_027",
            "h/h_ko_{0:00}_03_028",

            "h/h_ko_{0:00}_03_031",
            "h/h_ko_{0:00}_03_032",
        ];
        //private static readonly List<string> breathLickStrongSlow = new List<string>()
        //{
        //    "h/h_ko_{0:00}_00_029",
        //    "h/h_ko_{0:00}_00_030",

        //    "h/h_ko_{0:00}_03_029",
        //    "h/h_ko_{0:00}_03_030",
        //};
        //private static readonly List<string> breathLickStrongFast = new List<string>()
        //{
        //    "h/h_ko_{0:00}_00_031",
        //    "h/h_ko_{0:00}_00_032",

        //    "h/h_ko_{0:00}_03_031",
        //    "h/h_ko_{0:00}_03_032",
        //};
        private static readonly List<string> breathSuckWeakSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_033",
            "h/h_ko_{0:00}_00_037",
            "h/h_ko_{0:00}_00_038",
            "h/h_ko_{0:00}_00_045",
            "h/h_ko_{0:00}_00_046",
            "h/h_ko_{0:00}_00_053",
            "h/h_ko_{0:00}_00_054",

            "h/h_ko_{0:00}_02_045",
            "h/h_ko_{0:00}_02_046",

            "h/h_ko_{0:00}_03_033",
            "h/h_ko_{0:00}_03_037",
            "h/h_ko_{0:00}_03_038",
            "h/h_ko_{0:00}_03_045",
            "h/h_ko_{0:00}_03_046",
            "h/h_ko_{0:00}_03_053",
            "h/h_ko_{0:00}_03_054",
        };
        private static readonly List<string> breathSuckWeakFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_034",
            "h/h_ko_{0:00}_00_039",
            "h/h_ko_{0:00}_00_040",
            "h/h_ko_{0:00}_00_047",
            "h/h_ko_{0:00}_00_048",
            "h/h_ko_{0:00}_00_055",
            "h/h_ko_{0:00}_00_056",

            "h/h_ko_{0:00}_02_047",
            "h/h_ko_{0:00}_02_048",

            "h/h_ko_{0:00}_03_034",
            "h/h_ko_{0:00}_03_039",
            "h/h_ko_{0:00}_03_040",
            "h/h_ko_{0:00}_03_047",
            "h/h_ko_{0:00}_03_048",
            "h/h_ko_{0:00}_03_055",
            "h/h_ko_{0:00}_03_056",
        };
        private static readonly List<string> breathSuckStrongSlow = new List<string>()
        {
            "h/h_ko_{0:00}_00_035",
            "h/h_ko_{0:00}_00_041",
            "h/h_ko_{0:00}_00_042",
            "h/h_ko_{0:00}_00_049",
            "h/h_ko_{0:00}_00_050",

            "h/h_ko_{0:00}_02_049",
            "h/h_ko_{0:00}_02_050",

            "h/h_ko_{0:00}_03_035",
            "h/h_ko_{0:00}_03_041",
            "h/h_ko_{0:00}_03_042",
            "h/h_ko_{0:00}_03_049",
            "h/h_ko_{0:00}_03_050",
        };
        private static readonly List<string> breathSuckStrongFast = new List<string>()
        {
            "h/h_ko_{0:00}_00_036",
            "h/h_ko_{0:00}_00_043",
            "h/h_ko_{0:00}_00_044",
            "h/h_ko_{0:00}_00_051",
            "h/h_ko_{0:00}_00_052",

            "h/h_ko_{0:00}_02_051",
            "h/h_ko_{0:00}_02_052",

            "h/h_ko_{0:00}_03_036",
            "h/h_ko_{0:00}_03_043",
            "h/h_ko_{0:00}_03_044",
            "h/h_ko_{0:00}_03_051",
            "h/h_ko_{0:00}_03_052",
        };

        private static readonly List<string> breathGasp = new List<string>()
        {
            "h/h_ko_{0:00}_00_005_00",
            "h/h_ko_{0:00}_00_005_01",
            "h/h_ko_{0:00}_00_005_02",
            "h/h_ko_{0:00}_00_005_03",
            "h/h_ko_{0:00}_00_005_04",
            "h/h_ko_{0:00}_00_005_05",
            "h/h_ko_{0:00}_00_006_00",
            "h/h_ko_{0:00}_00_006_01",
            "h/h_ko_{0:00}_00_006_02",
            "h/h_ko_{0:00}_00_006_03",
            "h/h_ko_{0:00}_00_006_04",
            "h/h_ko_{0:00}_00_006_05",

            "h/h_ko_{0:00}_02_005_00",
            "h/h_ko_{0:00}_02_005_01",
            "h/h_ko_{0:00}_02_005_02",
            "h/h_ko_{0:00}_02_005_03",
            "h/h_ko_{0:00}_02_005_04",
            "h/h_ko_{0:00}_02_005_05",
            "h/h_ko_{0:00}_02_006_00",
            "h/h_ko_{0:00}_02_006_01",
            "h/h_ko_{0:00}_02_006_02",
            "h/h_ko_{0:00}_02_006_03",
            "h/h_ko_{0:00}_02_006_04",
            "h/h_ko_{0:00}_02_006_05",

            "h/h_ko_{0:00}_03_005_00",
            "h/h_ko_{0:00}_03_005_01",
            "h/h_ko_{0:00}_03_005_02",
            "h/h_ko_{0:00}_03_005_03",
            "h/h_ko_{0:00}_03_005_04",
            "h/h_ko_{0:00}_03_005_05",
            "h/h_ko_{0:00}_03_006_00",
            "h/h_ko_{0:00}_03_006_01",
            "h/h_ko_{0:00}_03_006_02",
            "h/h_ko_{0:00}_03_006_03",
            "h/h_ko_{0:00}_03_006_04",
            "h/h_ko_{0:00}_03_006_05"

        };
    }
}
