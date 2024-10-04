using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static KK_MaleBreathVR.LoadVoice;
using Random = UnityEngine.Random;
using System.Collections;
using static UnityEngine.Experimental.Director.FrameData;

namespace KK_MaleBreathVR
{
    internal class BreathComponent : MonoBehaviour
    {
        private Transform _voiceTransform;
        private ChaControl _chara;
        private BreathType _currentBreath;
        private VoiceType _currentVoice;
        internal string _lastVoiceName;
        private bool _inTransition;
        private bool _clickPending;
        private static Manager.Scene _scene;
        private bool _ownVoiceActive;
        private bool _partnerVoiceActive;
        internal static HFlag _hFlag;
        internal static List<ChaControl> _lstFemale;
        private int _voiceCooldown;
        private bool[] _encroachingVoices;
        private bool _waitForTrigger;
        internal static bool IsIdleInside(string animName) => animName.EndsWith("InsertIdle", StringComparison.Ordinal);
        internal static bool IsInsert(string animName) => animName.EndsWith("Insert", StringComparison.Ordinal);
        internal static bool IsIdleOutside(string animName) => animName.Equals("Idle");
        internal static bool IsActionLoop(string animName) => 
               animName.EndsWith("WLoop", StringComparison.Ordinal)
            || animName.EndsWith("SLoop", StringComparison.Ordinal)
            || animName.EndsWith("OLoop", StringComparison.Ordinal);
        internal static bool IsWeakLoop(string animName) => animName.EndsWith("WLoop", StringComparison.Ordinal);
        internal static bool IsStrongLoop(string animName) => animName.EndsWith("SLoop", StringComparison.Ordinal);
        internal static bool IsOrgasmLoop(string animName) => animName.EndsWith("OLoop", StringComparison.Ordinal);
        internal static bool IsKissAnim(string animName) => animName.StartsWith("K_", StringComparison.Ordinal);
        internal static bool IsOwnClimax(string animName) => animName.EndsWith("M_IN_Start", StringComparison.Ordinal) 
            || animName.EndsWith("M_OUT_Start", StringComparison.Ordinal);
        internal static bool IsPartnerClimax(string animName) => animName.EndsWith("WF_IN_Start", StringComparison.Ordinal) 
            || animName.EndsWith("SF_IN_Start", StringComparison.Ordinal);
        internal static bool IsBothClimax(string animName) => animName.EndsWith("WS_IN_Start", StringComparison.Ordinal)
            || animName.EndsWith("SS_IN_Start", StringComparison.Ordinal);
        internal static bool IsAfterClimaxInside(string animName) => animName.EndsWith("IN_A", StringComparison.Ordinal);
        internal static bool IsAfterClimaxOutside(string animName) => animName.EndsWith("OUT_A", StringComparison.Ordinal);
        internal static bool IsAfterClimaxInMouth(string animName) => animName.StartsWith("Oral", StringComparison.Ordinal);
        internal static bool IsFast => _hFlag.speedCalc > 0.5f;
        internal static bool IsPartnerVoiceActive
        {
            get
            {
                foreach (var chara in _lstFemale)
                {
                    if (chara.asVoice != null && !chara.asVoice.name.StartsWith("h_ko_", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private HFlag.TimeWait GetTimeWait()
        {
            if (_hFlag == null) return null;
            switch (_hFlag.mode)
            {
                case HFlag.EMode.aibu:
                    return _hFlag.voice.timeAibu;
                case HFlag.EMode.houshi:
                    return _hFlag.voice.timeHoushi;
                case HFlag.EMode.sonyu:
                    return _hFlag.voice.timeSonyu;
                //case HFlag.EMode.masturbation:
                //    return _hFlag.timeMasturbation.timeIdle - _hFlag.timeMasturbation.timeIdleCalc;
                //case HFlag.EMode.lesbian:
                //    return hFlag.timeLesbian.timeIdle - hFlag.timeLesbian.timeIdleCalc;
                default:
                    return null;
            }
        }
        private bool IsPartnerVoiceRecent()
        {
            var timeWait = GetTimeWait();
            if (timeWait == null) return true;
            return timeWait.timeIdle - timeWait.timeIdleCalc > timeWait.timeIdle * 0.5f;
        }
        private bool IsOwnVoiceRecent() => _voiceCooldown > MaleBreath.AverageVoiceCooldown.Value * 0.5f;
        private void PostponeVoice()
        {
            var timeWait = GetTimeWait();
            if (timeWait == null) return;
            if (timeWait.timeIdle - timeWait.timeIdleCalc < 2f)
            {
                _encroachingVoices[0] = true;
                timeWait.timeIdleCalc = timeWait.timeIdle - 2f;
            }
        }
        private void PlayPartnerVoice()
        {
            // Immediate response feels unnatural. 
            var timeWait = GetTimeWait();
            if (timeWait == null) return;
            timeWait.timeIdleCalc = timeWait.timeIdle - (0.25f + Random.value * 0.5f);
        }

        private void Start()
        {
            _chara = GetComponent<ChaControl>();
            _scene = Manager.Scene.Instance;
            _encroachingVoices = new bool[_lstFemale.Count];
            StartCoroutine(OncePerSecondCo());
        }
        private IEnumerator OncePerSecondCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (_scene.IsExit) continue;
                if (!_ownVoiceActive)
                {
                    if (IsPartnerVoiceActive) _partnerVoiceActive = true;
                    else if (_voiceCooldown-- < 0 && SetVoice()) continue;
                    SetBreath();
                }
                else
                {
                    PostponeVoice();
                }
            }
        }

        private void Update()
        {
            if (_waitForTrigger && CatchTrigger())
            {
                DoVoice();
                _waitForTrigger = false;
            }
            if (_voiceTransform == null && !_scene.IsExit)
            {
                if (RecoverTransform()) return;
                if (_partnerVoiceActive && !IsPartnerVoiceActive)
                {
                    _partnerVoiceActive = false;
                    MaleBreath.Logger.LogDebug($"Set:OwnResponse:{IsOwnVoiceRecent()}");
                    if (!IsOwnVoiceRecent() && SetVoice()) return;
                }

                if (_inTransition)
                {
                    _inTransition = false;
                    SetBreath();
                }
                else
                {
                    DoBreath();
                }
            }
        }
        private bool RecoverTransform()
        {
            if (_chara.asVoice != null)
            {
                _voiceTransform = _chara.asVoice.transform;
                return true;
            }
            return false;
        }
        private void SetLastVoice()
        {
            _lastVoiceName = lastLoadedAsset;
        }
        private void SetVoiceCooldown()
        {
            var avg = MaleBreath.AverageVoiceCooldown.Value;
            _ownVoiceActive = true;
            _voiceCooldown = Random.Range(Mathf.Abs(avg - 5), avg + 6);
            SetLastVoice();
        }
        private void DoClick()
        {
            if (_clickPending)
            {
                _clickPending = false;
                MaleBreathController.Click();
            }
        }
        private void DeactivateVoice()
        {
            if (_ownVoiceActive)
            {
                _ownVoiceActive = false;
                for (var i = 0; i < _encroachingVoices.Length; i++)
                {
                    if (_encroachingVoices[i])
                    {
                        _encroachingVoices[i] = false;
                        PlayPartnerVoice();
                        return;
                    }
                }
                if (Random.value < 0.67f && !IsPartnerVoiceRecent()) PlayPartnerVoice();
                MaleBreath.Logger.LogDebug($"Set:PartnerResponse:{IsPartnerVoiceRecent()}");
            }
        }
        private void DoBreath()
        {
            DoClick();
            _voiceTransform = _chara.objTop.activeSelf && _chara.visibleAll ? PlayBreath(_currentBreath, chara: _chara) 
                : PlayBreath(_currentBreath, breathTransform: this.transform); // VR.Camera.transform);
            DeactivateVoice();
        }
        private bool DoVoice()
        {
            DoClick();
            var transform = _chara.objTop.activeSelf && _chara.visibleAll ? PlayVoice(_currentVoice, chara: _chara)
                : PlayVoice(_currentVoice, voiceTransform: this.transform); // VR.Camera.transform);
            if (transform != null)
            {
                _voiceTransform = transform;
                SetVoiceCooldown();
                return true;
            }
            return false;
        }
        /// <returns>False if no voice was found.</returns>
        public bool SetTriggerVoice(VoiceType voiceType)
        {
            MaleBreath.Logger.LogDebug($"SetSpecialVoice:{voiceType}");
            _currentVoice = voiceType;
            var result = DoVoice();
            _clickPending = true;
            return result;
        }
        private bool SetVoice()
        {
            var animName = _hFlag.nowAnimStateName;
            var voiceType = VoiceType.Idle;
            switch (_hFlag.mode)
            {
                //case HFlag.EMode.aibu:
                //    if (IsKissAnim(animName)) ManageKiss();
                //    break;
                //case HFlag.EMode.houshi:
                //case HFlag.EMode.houshi3P:
                //case HFlag.EMode.houshi3PMMF:
                //    if (IsWeakLoop(animName))
                //    {
                //        breathType = IsFast ? BreathType.ResistWeakFast : BreathType.ResistWeakSlow;
                //    }
                //    else if (IsStrongLoop(animName) || IsOrgasmLoop(animName))
                //    {
                //        breathType = IsFast ? BreathType.ResistStrongFast : BreathType.ResistStrongSlow;
                //    }
                //    else if (IsEndInMouth(animName) || IsEndOutside(animName))
                //    {
                //        breathType = BreathType.Strained;
                //    }
                //    break;
                case HFlag.EMode.sonyu:
                case HFlag.EMode.sonyu3P:
                case HFlag.EMode.sonyu3PMMF:
                    if (IsIdleInside(animName))
                    {
                        voiceType = _hFlag.isAnalPlay ? VoiceType.InsertIdleAnal : VoiceType.InsertIdle;
                    }
                    else if (IsActionLoop(animName))
                    {
                        // Grab SensH ceiling.
                        if (_hFlag.gaugeMale > 70f && _hFlag.gaugeFemale > 70f)
                        {
                            voiceType = IsFast ? VoiceType.LateLoopBothFast : VoiceType.LateLoopBothSlow;
                        }
                        else if (_hFlag.gaugeMale > 70f)
                        {
                            voiceType = IsFast ? VoiceType.LateLoopSelfFast : VoiceType.LateLoopSelfSlow;
                        }
                        else if (_hFlag.gaugeFemale > 70f)
                        {
                            voiceType = IsFast ? VoiceType.LateLoopPartnerFast : VoiceType.LateLoopPartnerSlow;
                        }
                        else
                        {
                            voiceType = IsFast ? VoiceType.LoopFast : VoiceType.LoopSlow;
                        }
                    }
                    else if (IsAfterClimaxInside(animName) || IsAfterClimaxOutside(animName))
                    {
                        voiceType = VoiceType.AfterClimaxSelf;
                    }
                    break;
            }
            _currentVoice = voiceType;
            return DoVoice();
        }
        private bool CatchTrigger()
        {
            var animName = _hFlag.nowAnimStateName;
            switch (_hFlag.mode)
            {
                //case HFlag.EMode.aibu:
                //case HFlag.EMode.houshi:
                //case HFlag.EMode.houshi3P:
                //case HFlag.EMode.houshi3PMMF:
                case HFlag.EMode.sonyu:
                case HFlag.EMode.sonyu3P:
                case HFlag.EMode.sonyu3PMMF:
                    var finish = _hFlag.finish;
                    if (finish != HFlag.FinishKind.none)
                    {
                        switch (finish)
                        {
                            case HFlag.FinishKind.inside:
                            case HFlag.FinishKind.outside:
                                if (IsOwnClimax(animName))
                                {
                                    _currentVoice = VoiceType.ClimaxSelf;
                                    return true;
                                }
                                break;
                            //case HFlag.FinishKind.orgW:
                            //case HFlag.FinishKind.orgS:
                            //    if (IsPartnerClimax(animName)) voiceType = VoiceType.Climax
                            case HFlag.FinishKind.sameW:
                            case HFlag.FinishKind.sameS:
                                if (IsBothClimax(animName))
                                {
                                    _currentVoice = VoiceType.ClimaxBoth;
                                    return true;
                                }
                                break;
                        }
                    }
                    break;
            }
            return false;
        }
        private void SetBreath()
        {
            var animName = _hFlag.nowAnimStateName;
            var breathType = BreathType.Normal;
            switch (_hFlag.mode)
            {
                case HFlag.EMode.aibu:
                    if (IsKissAnim(animName)) ManageKiss();
                    break;
                case HFlag.EMode.houshi:
                case HFlag.EMode.houshi3P:
                case HFlag.EMode.houshi3PMMF:
                    if (IsWeakLoop(animName))
                    {
                        breathType = IsFast ? BreathType.ResistWeakFast : BreathType.ResistWeakSlow;
                    }
                    else if (IsStrongLoop(animName) || IsOrgasmLoop(animName))
                    {
                        breathType = IsFast ? BreathType.ResistStrongFast : BreathType.ResistStrongSlow;
                    }
                    else if (IsAfterClimaxInMouth(animName) || IsAfterClimaxOutside(animName))
                    {
                        breathType = BreathType.Strained;
                    }
                    break;
                case HFlag.EMode.sonyu:
                case HFlag.EMode.sonyu3P:
                case HFlag.EMode.sonyu3PMMF:
                    if (IsWeakLoop(animName))
                    {
                        // Grab SensH ceiling.
                        if (_hFlag.gaugeMale > 70f)
                        {
                            breathType = IsFast ? BreathType.LateLoopWeakFast : BreathType.LateLoopWeakSlow;
                        }
                        else
                        {
                            breathType = IsFast ? BreathType.LoopWeakFast : BreathType.LoopWeakSlow;
                        }
                    }
                    else if (IsStrongLoop(animName) || IsOrgasmLoop(animName))
                    {
                        if (_hFlag.gaugeMale > 70f)
                        {
                            breathType = IsFast ? BreathType.LateLoopStrongFast : BreathType.LateLoopStrongSlow;
                        }
                        else
                        {
                            breathType = IsFast ? BreathType.LoopStrongFast : BreathType.LoopStrongSlow;
                        }
                    }
                    else if (IsAfterClimaxInside(animName) || IsAfterClimaxOutside(animName))
                    {
                        breathType = BreathType.Strained;
                    }
                    break;
            }
            if (breathType == _currentBreath) return;
            if (_inTransition)
            {
                MaleBreath.Logger.LogDebug($"Transition:cur - {_currentBreath}:tar - {breathType}");
                if (breathType > _currentBreath)
                {
                    MaleBreath.Logger.LogDebug($"Transition:OverrideWithHigherVoice");
                    _currentBreath = breathType;
                    _inTransition = false;
                    DoBreath();
                }
                return;
            }
            ApplyTransition(ref breathType);
            _currentBreath = breathType;
            DoBreath();
        }
        private void ApplyTransition(ref BreathType targetType)
        {
            MaleBreath.Logger.LogDebug($"Transition:Attempt:from:{_currentBreath}:to:{targetType}");
            var original = targetType;
            if (targetType == BreathType.Normal)
            {
                if (_currentBreath > BreathType.Strained)
                {
                    targetType = BreathType.Strained;
                }
                else if (_currentBreath == BreathType.Strained)
                {
                    targetType = BreathType.StrainedToNormal;
                }
            }
            if (targetType != original)
            {
                MaleBreath.Logger.LogDebug($"Transition:Enter:from:{_currentBreath}:to:{targetType}");
                _inTransition = true;
                //return true;
            }
            //return false;
        }
        private void ManageKiss()
        {

        }
    }
}
