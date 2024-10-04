using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI;
using KKAPI.MainGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KK_MaleBreathVR
{
    [BepInPlugin(GUID, "KK_MaleBreath", Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    public class MaleBreath : BaseUnityPlugin
    {
        public const string GUID = "kk.malebreath";
        public const string Version = "0.1";
        public new static PluginInfo Info;
        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<Personality> PlayerPersonality;
        public static ConfigEntry<HExp> PreferredVoiceExperience;
        public static ConfigEntry<HExp> PreferredBreathExperience;
        public static ConfigEntry<float> VoiceVolume;
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
                key: "PlayerPersonality",
                defaultValue: Personality.Tomboy,
                ""
                );
            PreferredVoiceExperience = Config.Bind(
                section: "",
                key: "PreferredVoiceExperience",
                defaultValue: HExp.淫乱,
                ""
                );
            PreferredBreathExperience = Config.Bind(
                section: "",
                key: "PreferredBreathExperience",
                defaultValue: HExp.淫乱,
                ""
                );
            VoiceVolume = Config.Bind(
                section: "",
                key: "Volume",
                defaultValue: 0.5f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0f, 1f))
                );
            AverageVoiceCooldown = Config.Bind(
                section: "",
                key: "AverageVoiceCooldown",
                defaultValue: 25,
                new ConfigDescription("",
                new AcceptableValueRange<int>(0, 60))
                );
            LoadVoice.Initialize();
            GameAPI.RegisterExtraBehaviour<MaleBreathController>(GUID);
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
