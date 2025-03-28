using UnityEngine;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Neocortex
{
    public class MicrophonePermission : MonoBehaviour
    {
        [SerializeField] private GameObject[] activateOnPermission;
        [SerializeField] private GameObject[] deactivateOnPermission;

        private IEnumerator Start()
        {
            RequestMicrophonePermission();
            yield return StartCoroutine(WaitForMicrophonePermission());

            foreach (GameObject go in activateOnPermission)
            {
                go.SetActive(true);
            }
            
            foreach (GameObject go in deactivateOnPermission)
            {
                go.SetActive(false);
            }
        }

        private void RequestMicrophonePermission()
        {
            #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            #elif UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
            #endif
        }

        private IEnumerator WaitForMicrophonePermission()
        {
            #if UNITY_ANDROID
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                yield return null;
            }
            #elif UNITY_IOS
            while (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                yield return null;
            }
            #else
            yield return null;
            #endif
        }
    }
}
