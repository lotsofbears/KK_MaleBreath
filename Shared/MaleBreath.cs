using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KK_MaleBreath
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]

#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif


    public class MaleBreath : BaseUnityPlugin
    {
        public const string GUID = "kk.malebreath";
        public const string Name = "KK_MaleBreath";
        // There is a rare nullref "crash", preventing this to be 1.0
        // Haven't seen it in a while though, no clue how to catch it.
        public const string Version = "0.9.0";


        // public new static PluginInfo Info;
        public static ConfigEntry<EnableState> Enable;
        public static ConfigEntry<Personality> PlayerPersonality;
        public static ConfigEntry<HExp> PreferredVoiceExperience;
        public static ConfigEntry<HExp> PreferredBreathExperience;
        public static ConfigEntry<float> VolumeBreath;
        public static ConfigEntry<float> VolumeVoice;
        public static ConfigEntry<int> AverageVoiceCooldown;
        public static ConfigEntry<bool> RunInAibu;


        internal new static ManualLogSource Logger;

        // Hook for KK(S)_VR to get desirable personality (It can play male voices on controller touch). 
        public static int GetPlayerPersonality() => (int)PlayerPersonality.Value;
        // Hook for KK(S)_VR's PoV.
        public static void OnPov(bool active, ChaControl chara)
        {
            MaleBreathController.OnPov(active, chara);
        }
        private void Awake()    
        {
            Logger = base.Logger;
            Enable = Config.Bind(
                section: "",
                key: "Enable",
                defaultValue: EnableState.OnlyInVr,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { Order = 20 })
                );


            RunInAibu = Config.Bind(
                section: "",
                key: "Run in caress",
                defaultValue: false,
                new ConfigDescription("Add breath to the camera",
                null,
                new ConfigurationManagerAttributes { Order = 19 })
                );


            PlayerPersonality = Config.Bind(
                section: "",
                key: "Personality",
                defaultValue: Personality.Stubborn,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { Order = 15 })
                );


            PreferredBreathExperience = Config.Bind(
                section: "",
                key: "BreathExperience",
                defaultValue: HExp.淫乱,
                new ConfigDescription("Prefer if available, fallback at lower one if not",
                null,
                new ConfigurationManagerAttributes { Order = 14 })
                );


            PreferredVoiceExperience = Config.Bind(
                section: "",
                key: "VoiceExperience",
                defaultValue: HExp.淫乱,
                new ConfigDescription("Prefer if available, fallback at lower one if not\nRequires manually set-up voices in .csv file",
                null,
                new ConfigurationManagerAttributes { Order = 13 })
                );


            VolumeBreath = Config.Bind(
                section: "",
                key: "Volume breath",
                defaultValue: 0.2f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 10, ShowRangeAsPercent = false })
                );


            VolumeVoice = Config.Bind(
                section: "",
                key: "Volume voice",
                defaultValue: 0.7f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 10, ShowRangeAsPercent = false })
                );


            AverageVoiceCooldown = Config.Bind(
                section: "",
                key: "VoiceCooldown",
                defaultValue: 25,
                new ConfigDescription("Requires manually set-up voices in .csv file",
                new AcceptableValueRange<int>(0, 60),
                new ConfigurationManagerAttributes { Order = 9 })
                );


            LoadGameVoice.Initialize();
            GameAPI.RegisterExtraBehaviour<MaleBreathController>(GUID);
        }
        public enum EnableState
        {
            Disable,
            OnlyInVr,
            Always,
        }

        public enum HExp
        {
            Any = -1,
            初めて,
            不慣れ,
            慣れ,
            淫乱
        }
        // Is there an in-game enum for personalities ?
        public enum Personality
        {
            Sexy,
            Ojousama,
            Snobby,
            Kouhai,
            Mysterious,
            Weirdo,
            YamatoNadeshiko,
            Tomboy,
            Pure,
            Simple,
            Delusional,
            Motherly,
            BigSisterly,
            Gyaru,
            Delinquent,
            Wild,
            Wannabe,
            Reluctant,
            Jinxed,
            Bookish,
            Timid,
            TypicalSchoolgirl,
            Trendy,
            Otaku,
            Yandere,
            Lazy,
            Quiet,
            Stubborn,
            OldFashioned,
            Humble,
            Friendly,
            Willful,
            Honest,
            Glamorous,
            Returnee,
            Slangy,
            Sadistic,
            Emotionless,
            Perfectionist
        }
    }
}
