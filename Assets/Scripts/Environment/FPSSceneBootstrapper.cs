using UnityEngine;
using SpaceGame.Player;

namespace SpaceGame.Environment
{
    /// <summary>
    /// Ensures that hitting 'Play' in Unity immediately provides a playable FPS test arena
    /// even if the user hasn't manually set up GameObjects in the scene hierarchy yet.
    /// </summary>
    public static class FPSSceneBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            FPSController player = Object.FindFirstObjectByType<FPSController>();
            if (player == null)
            {
                Debug.Log("<color=#00e5ff><b>[Space Game]</b> Auto-bootstrapping FPS Player & Test Arena for Play Mode!</color>");
                FPSSetupUtility.SetupPlayerAndArena();
            }
        }
    }
}
