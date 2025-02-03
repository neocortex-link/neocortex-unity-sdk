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
            
            RefreshShownValue();
        }
    }
}
