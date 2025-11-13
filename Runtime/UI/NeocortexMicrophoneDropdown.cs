using UnityEngine;
using UnityEngine.UI;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Microphone Dropdown", 0)]
    public class NeocortexMicrophoneDropdown : Dropdown
    {
        protected override void Awake()
        {
            base.Awake();
            
            options.Clear();
            
            foreach (string device in NeocortexMicrophone.devices)
            {
                options.Add(new OptionData(device));
            }

            SetExistingValue();
            
            RefreshShownValue();
            
            onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(int index)
        {
            PlayerPrefs.SetInt(AudioReceiver.MIC_INDEX_KEY, index);
        }

        private void SetExistingValue()
        {
            int savedIndex = PlayerPrefs.GetInt(AudioReceiver.MIC_INDEX_KEY, 0);

            value = 0;
            if (savedIndex >= 0 && savedIndex < options.Count)
            {
                value = savedIndex;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
