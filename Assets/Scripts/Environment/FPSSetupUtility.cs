using UnityEngine;
using SpaceGame.Player;

namespace SpaceGame.Environment
{
    public static class FPSSetupUtility
    {
        public static GameObject SetupPlayerAndArena()
        {
            // 1. Check or create Player
            FPSController existingPlayer = Object.FindFirstObjectByType<FPSController>();
            GameObject playerObj;

            if (existingPlayer != null)
            {
                playerObj = existingPlayer.gameObject;
                if (playerObj.GetComponent<ObjectCarryingSystem>() == null)
                {
                    playerObj.AddComponent<ObjectCarryingSystem>();
                }
            }
            else
            {
                playerObj = new GameObject("FPS_Player");
                playerObj.transform.position = new Vector3(0f, 1f, 0f);

                CharacterController cc = playerObj.AddComponent<CharacterController>();
                cc.height = 2.0f;
                cc.radius = 0.45f;
                cc.center = new Vector3(0f, 1.0f, 0f);
                cc.stepOffset = 0.35f;

                FPSController controller = playerObj.AddComponent<FPSController>();
                ObjectCarryingSystem carrySystem = playerObj.AddComponent<ObjectCarryingSystem>();

                // Setup Camera under Player
                Camera mainCam = Camera.main;
                GameObject camHolder = new GameObject("CameraHolder");
                camHolder.transform.SetParent(playerObj.transform);
                camHolder.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                camHolder.transform.localRotation = Quaternion.identity;

                if (mainCam != null)
                {
                    mainCam.transform.SetParent(camHolder.transform);
                    mainCam.transform.localPosition = Vector3.zero;
                    mainCam.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.transform.SetParent(camHolder.transform);
                    camObj.transform.localPosition = Vector3.zero;
                    camObj.transform.localRotation = Quaternion.identity;
                    Camera cam = camObj.AddComponent<Camera>();
                    cam.tag = "MainCamera";
                    camObj.AddComponent<AudioListener>();
                }
            }

            // 2. Check or create Test Arena
            GameObject arena = GameObject.Find("FPS_TestArena");
            if (arena == null)
            {
                arena = CreateArena();
            }
            else
            {
                // Ensure physics carryables exist under the existing arena
                EnsureCarryableProps(arena.transform);
            }

            return playerObj;
        }

        private static GameObject CreateArena()
        {
            GameObject arenaParent = new GameObject("FPS_TestArena");
            arenaParent.transform.position = Vector3.zero;

            // Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            Material floorMat = new Material(litShader);
            floorMat.name = "Arena_Floor_Mat";
            floorMat.color = new Color(0.12f, 0.14f, 0.18f); // Dark space hull metal

            Material accentMat = new Material(litShader);
            accentMat.name = "Arena_Accent_Mat";
            accentMat.color = new Color(0.15f, 0.65f, 0.85f); // Neon cyan

            Material jumpMat = new Material(litShader);
            jumpMat.name = "Arena_Jump_Mat";
            jumpMat.color = new Color(0.95f, 0.55f, 0.15f); // Orange hazard

            // Main Floor: 50x50 area
            CreateBlock("Floor", arenaParent.transform, new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 50f), floorMat);

            // Perimeter walls
            CreateBlock("Wall_North", arenaParent.transform, new Vector3(0f, 1.5f, 25f), new Vector3(50f, 3f, 1f), floorMat);
            CreateBlock("Wall_South", arenaParent.transform, new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 1f), floorMat);
            CreateBlock("Wall_East",  arenaParent.transform, new Vector3(25f, 1.5f, 0f), new Vector3(1f, 3f, 50f), floorMat);
            CreateBlock("Wall_West",  arenaParent.transform, new Vector3(-25f, 1.5f, 0f), new Vector3(1f, 3f, 50f), floorMat);

            // Jump blocks and platforms to test movement and jumping
            CreateBlock("Step_Low", arenaParent.transform, new Vector3(5f, 0.35f, 6f), new Vector3(3f, 0.7f, 3f), jumpMat);
            CreateBlock("Step_Mid", arenaParent.transform, new Vector3(5f, 0.85f, 10f), new Vector3(3f, 1.7f, 3f), jumpMat);
            CreateBlock("Step_High", arenaParent.transform, new Vector3(5f, 1.35f, 14f), new Vector3(3f, 2.7f, 3f), jumpMat);

            // Observation Tower / High Platform
            CreateBlock("Platform_Base", arenaParent.transform, new Vector3(-8f, 1.5f, 8f), new Vector3(6f, 3f, 6f), accentMat);
            CreateBlock("Ramp_Run", arenaParent.transform, new Vector3(-8f, 0.75f, 2f), new Vector3(2.5f, 0.4f, 4f), floorMat);

            // Sci-fi Pillars for perspective reference
            CreateBlock("Pillar_1", arenaParent.transform, new Vector3(-12f, 3f, -12f), new Vector3(1.5f, 6f, 1.5f), accentMat);
            CreateBlock("Pillar_2", arenaParent.transform, new Vector3(12f, 3f, -12f), new Vector3(1.5f, 6f, 1.5f), accentMat);
            CreateBlock("Pillar_3", arenaParent.transform, new Vector3(-12f, 3f, 12f), new Vector3(1.5f, 6f, 1.5f), accentMat);
            CreateBlock("Pillar_4", arenaParent.transform, new Vector3(12f, 3f, 12f), new Vector3(1.5f, 6f, 1.5f), accentMat);

            EnsureCarryableProps(arenaParent.transform);

            return arenaParent;
        }

        public static void EnsureCarryableProps(Transform arenaParent)
        {
            if (arenaParent == null) return;

            if (arenaParent.Find("PhysicsCrate_1") != null) return; // Already exists

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material crateMat = new Material(litShader) { name = "Crate_Mat", color = new Color(0.85f, 0.65f, 0.25f) };
            Material barrelMat = new Material(litShader) { name = "Barrel_Mat", color = new Color(0.25f, 0.75f, 0.45f) };

            CreateCarryableCrate("PhysicsCrate_1", arenaParent, new Vector3(2f, 0.5f, 3f), new Vector3(0.8f, 0.8f, 0.8f), 12f, crateMat);
            CreateCarryableCrate("PhysicsCrate_2", arenaParent, new Vector3(2f, 1.3f, 3f), new Vector3(0.8f, 0.8f, 0.8f), 12f, crateMat);
            CreateCarryableCrate("PhysicsCrate_3", arenaParent, new Vector3(3.2f, 0.5f, 3.2f), new Vector3(0.9f, 0.9f, 0.9f), 15f, crateMat);
            CreateCarryableBarrel("PhysicsBarrel_1", arenaParent, new Vector3(-3f, 0.6f, 3.5f), new Vector3(0.7f, 1.1f, 0.7f), 18f, barrelMat);
            CreateCarryableBarrel("PhysicsBarrel_2", arenaParent, new Vector3(-4f, 0.6f, 4f), new Vector3(0.7f, 1.1f, 0.7f), 18f, barrelMat);
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;

            if (mat != null)
            {
                Renderer r = cube.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = mat;
                }
            }

            return cube;
        }

        private static GameObject CreateCarryableCrate(string name, Transform parent, Vector3 localPos, Vector3 localScale, float mass, Material mat)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = name;
            crate.transform.SetParent(parent);
            crate.transform.localPosition = localPos;
            crate.transform.localScale = localScale;

            if (mat != null)
            {
                Renderer r = crate.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            Rigidbody rb = crate.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            crate.AddComponent<CarryableObject>();
            return crate;
        }

        private static GameObject CreateCarryableBarrel(string name, Transform parent, Vector3 localPos, Vector3 localScale, float mass, Material mat)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name;
            barrel.transform.SetParent(parent);
            barrel.transform.localPosition = localPos;
            barrel.transform.localScale = localScale;

            if (mat != null)
            {
                Renderer r = barrel.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            Rigidbody rb = barrel.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            barrel.AddComponent<CarryableObject>();
            return barrel;
        }
    }
}
