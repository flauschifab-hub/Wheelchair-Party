using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using Photon.Voice.Unity;

namespace Photon.Voice.Unity.Demos.DemoVoiceUI
{
    // -----------------------------
    // Mic Type & Reference
    // -----------------------------
    public enum MicType
    {
        Unity,
        Photon,
        FMOD
    }

    public struct MicRef
    {
        public readonly MicType MicType;
        public readonly DeviceInfo Device;

        public MicRef(MicType micType, DeviceInfo device)
        {
            this.MicType = micType;
            this.Device = device;
        }

        public override string ToString()
        {
            return $"Mic reference: {Device.Name}";
        }
    }

    // -----------------------------
    // Main Microphone Selector
    // -----------------------------
    public class MicrophoneSelector : VoiceComponent
    {
        public class MicrophoneSelectorEvent : UnityEvent<MicType, DeviceInfo> { }
        public MicrophoneSelectorEvent onValueChanged = new MicrophoneSelectorEvent();

        private List<MicRef> micOptions;

#pragma warning disable 649
        [SerializeField] private TMP_Dropdown micDropdown;
        [SerializeField] private Slider micLevelSlider;
        [SerializeField] private Recorder recorder;
        [SerializeField, FormerlySerializedAs("RefreshButton")] private GameObject refreshButton;
#pragma warning restore 649

        private Image fillArea;
        private Color defaultFillColor = Color.white;
        private Color speakingFillColor = Color.green;

        private IDeviceEnumerator unityMicEnum;
        private IDeviceEnumerator photonMicEnum;

#if PHOTON_VOICE_FMOD_ENABLE
        private IDeviceEnumerator fmodMicEnum;
        private System.Func<IAudioDesc> fmodInputFactory;
#endif

        protected override void Awake()
        {
            base.Awake();

            unityMicEnum = new Unity.AudioInEnumerator(Logger);
#if PHOTON_VOICE_FMOD_ENABLE
            fmodMicEnum = new Photon.Voice.FMOD.AudioInEnumerator(FMODUnity.RuntimeManager.CoreSystem, Logger);
#endif
            photonMicEnum = Platform.CreateAudioInEnumerator(Logger);
            photonMicEnum.OnReady = () =>
            {
                SetupMicDropdown();
                SelectDefaultMic();
            };

            if (refreshButton != null)
                refreshButton.GetComponentInChildren<Button>().onClick.AddListener(RefreshMicrophones);

            if (micLevelSlider != null)
                fillArea = micLevelSlider.fillRect.GetComponent<Image>();

            defaultFillColor = fillArea != null ? fillArea.color : Color.white;
        }

        private void Update()
        {
            if (recorder != null && micLevelSlider != null && fillArea != null)
            {
                micLevelSlider.value = recorder.LevelMeter.CurrentPeakAmp;
                fillArea.color = recorder.IsCurrentlyTransmitting ? speakingFillColor : defaultFillColor;
            }
        }

        private void OnEnable()
        {
            UtilityScripts.MicrophonePermission.MicrophonePermissionCallback += OnMicrophonePermissionCallback;
        }

        private void OnDisable()
        {
            UtilityScripts.MicrophonePermission.MicrophonePermissionCallback -= OnMicrophonePermissionCallback;
        }

        private void OnMicrophonePermissionCallback(bool granted)
        {
            RefreshMicrophones();
        }

        private void SetupMicDropdown()
        {
            if (micDropdown == null) return;

            micDropdown.ClearOptions();
            micOptions = new List<MicRef>();
            List<string> micStrings = new List<string>();

            // Unity default
            micOptions.Add(new MicRef(MicType.Unity, DeviceInfo.Default));
            micStrings.Add("[Unity] [Default]");

            foreach (var d in unityMicEnum)
            {
                micOptions.Add(new MicRef(MicType.Unity, d));
                micStrings.Add($"[Unity] {d}");
            }

            // Photon default
            micOptions.Add(new MicRef(MicType.Photon, DeviceInfo.Default));
            micStrings.Add("[Photon] [Default]");

            foreach (var d in photonMicEnum)
            {
                micOptions.Add(new MicRef(MicType.Photon, d));
                micStrings.Add($"[Photon] {d}");
            }

#if PHOTON_VOICE_FMOD_ENABLE
            foreach (var d in fmodMicEnum)
            {
                micOptions.Add(new MicRef(MicType.FMOD, d));
                micStrings.Add($"[FMOD] {d}");
            }
#endif

            micDropdown.AddOptions(micStrings);
            micDropdown.onValueChanged.RemoveAllListeners();
            micDropdown.onValueChanged.AddListener(_ => SwitchToSelectedMic());
        }

        private void SelectDefaultMic()
        {
            if (micOptions == null || micDropdown == null || recorder == null) return;

            for (int i = 0; i < micOptions.Count; i++)
            {
                if (micOptions[i].Device.IsDefault)
                {
                    micDropdown.value = i;
                    SwitchToSelectedMic();
                    return;
                }
            }

            // fallback to first mic
            micDropdown.value = 0;
            SwitchToSelectedMic();
        }

        public void SwitchToSelectedMic()
        {
            if (micDropdown == null || micOptions == null || recorder == null) return;

            MicRef mic = micOptions[micDropdown.value];

            switch (mic.MicType)
            {
                case MicType.Unity:
                    recorder.SourceType = Recorder.InputSourceType.Microphone;
                    recorder.MicrophoneType = Recorder.MicType.Unity;
                    recorder.MicrophoneDevice = mic.Device;
                    break;

                case MicType.Photon:
                    recorder.SourceType = Recorder.InputSourceType.Microphone;
                    recorder.MicrophoneType = Recorder.MicType.Photon;
                    recorder.MicrophoneDevice = mic.Device;
                    break;

#if PHOTON_VOICE_FMOD_ENABLE
                case MicType.FMOD:
                    recorder.SourceType = Recorder.InputSourceType.Factory;
                    recorder.InputFactory = fmodInputFactory = () =>
                        new Photon.Voice.FMOD.AudioInReader<short>(
                            FMODUnity.RuntimeManager.CoreSystem,
                            mic.Device.IsDefault ? 0 : mic.Device.IDInt,
                            (int)recorder.SamplingRate,
                            Logger
                        );
                    break;
#endif
            }

            // --- Enable transmission safely ---
            if (recorder != null && recorder.MicrophoneDevice != null)
            {
                recorder.TransmitEnabled = true; // replaces StartRecording()
            }

            onValueChanged?.Invoke(mic.MicType, mic.Device);
        }

        public void RefreshMicrophones()
        {
            unityMicEnum.Refresh();
            photonMicEnum.Refresh();
#if PHOTON_VOICE_FMOD_ENABLE
            fmodMicEnum.Refresh();
#endif
        }
    }
}
