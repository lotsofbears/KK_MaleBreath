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
    [BepInPlugin(GUID, "KK_MaleBreath", Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    public class MaleBreath : BaseUnityPlugin
    {
        public const string GUID = "kk.malebreath";
        // There is a rare nullref "crash", preventing this to be 1.0
        // Haven't seen it in a while though, no clue how to catch it.
        public const string Version = "0.9.0";
        public new static PluginInfo Info;
        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<Personality> PlayerPersonality;
        public static ConfigEntry<HExp> PreferredVoiceExperience;
        public static ConfigEntry<HExp> PreferredBreathExperience;
        public static ConfigEntry<float> Volume;
        public static ConfigEntry<int> AverageVoiceCooldown;
        internal new static ManualLogSource Logger;
        public static int GetPlayerPersonality() => (int)PlayerPersonality.Value;
        private void Awake()    
        {
            Logger = base.Logger;
            Enable = Config.Bind(
                section: "",
                key: "Enable",
                defaultValue: true,
                ""
                );
            PlayerPersonality = Config.Bind(
                section: "",
                key: "Personality",
                defaultValue: Personality.Stubborn,
                ""
                );
            PreferredVoiceExperience = Config.Bind(
                section: "",
                key: "VoiceExperience",
                defaultValue: HExp.淫乱,
                ""
                );
            PreferredBreathExperience = Config.Bind(
                section: "",
                key: "BreathExperience",
                defaultValue: HExp.淫乱,
                ""
                );
            Volume = Config.Bind(
                section: "",
                key: "Volume",
                defaultValue: 0.5f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { ShowRangeAsPercent = false })
                );
            AverageVoiceCooldown = Config.Bind(
                section: "",
                key: "VoiceCooldown",
                defaultValue: 25,
                new ConfigDescription("",
                new AcceptableValueRange<int>(0, 60))
                );
            LoadVoice.Initialize();
            GameAPI.RegisterExtraBehaviour<MaleBreathController>(GUID);
#if KKS
            Harmony.CreateAndPatchAll(typeof(Patches));
#endif
        }
        public enum HExp
        {
            Any = -1,
            初めて,
            不慣れ,
            慣れ,
            淫乱
        }
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
