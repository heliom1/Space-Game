#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpaceGame.Environment;
using SpaceGame.Player;

namespace SpaceGame.Editor
{
    public static class FPSSetupMenu
    {
        [MenuItem("Space Game/Setup FPS Player & Test Arena", false, 1)]
        public static void SetupPlayerAndArenaInScene()
        {
            GameObject player = FPSSetupUtility.SetupPlayerAndArena();

            if (player != null)
            {
                Selection.activeGameObject = player;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("<color=#00e5ff><b>[Space Game]</b> FPS Player & Test Arena successfully set up in the scene! Press <b>Play</b> to test WASD and mouse look.</color>");
            }
        }

        [MenuItem("Space Game/Spawn Carryable Props (Crates & Barrels)", false, 3)]
        public static void SpawnPropsInScene()
        {
            GameObject arena = GameObject.Find("FPS_TestArena");
            if (arena == null)
            {
                FPSSetupUtility.SetupPlayerAndArena();
                arena = GameObject.Find("FPS_TestArena");
            }
            else
            {
                FPSSetupUtility.EnsureCarryableProps(arena.transform);
            }

            // Also ensure player has ObjectCarryingSystem
            FPSController player = Object.FindFirstObjectByType<FPSController>();
            if (player != null && player.GetComponent<ObjectCarryingSystem>() == null)
            {
                player.gameObject.AddComponent<ObjectCarryingSystem>();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#00e5ff><b>[Space Game]</b> Physics carryable crates & barrels spawned right in front of the player! Look at them and press [F] to pick up.</color>");
        }
    }
}
#endif
