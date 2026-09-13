using UnityEngine;

namespace Platformer3D
{
    /// <summary>
    /// Senior-level Playground Scene Generator.
    /// Automatically builds a complete 3D platformer level with ramps, slide slopes,
    /// jump obstacles, low clearance tunnels, lighting, and player setup!
    /// </summary>
    public class PlaygroundBuilder : MonoBehaviour
    {
        [Header("Generator Options")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Color groundColor = new Color(0.2f, 0.25f, 0.35f);
        [SerializeField] private Color rampColor = new Color(0.9f, 0.45f, 0.2f);
        [SerializeField] private Color platformColor = new Color(0.2f, 0.75f, 0.6f);
        [SerializeField] private Color playerColor = new Color(0.1f, 0.8f, 1.0f);
        [SerializeField] private Color visorColor = new Color(0.05f, 0.1f, 0.15f);

        private void Start()
        {
            if (buildOnStart)
            {
                GeneratePlayground();
            }
        }

        [ContextMenu("Generate Playground Level")]
        public void GeneratePlayground()
        {
            // Create Materials
            Material matGround = CreateSimpleMaterial("Mat_Ground", groundColor);
            Material matRamp = CreateSimpleMaterial("Mat_Ramp", rampColor);
            Material matPlatform = CreateSimpleMaterial("Mat_Platform", platformColor);
            Material matPlayer = CreateSimpleMaterial("Mat_Player", playerColor);
            Material matVisor = CreateSimpleMaterial("Mat_Visor", visorColor);

            GameObject environmentParent = GameObject.Find("--- ENVIRONMENT ---");
            if (environmentParent != null)
            {
                DestroyImmediate(environmentParent);
            }
            environmentParent = new GameObject("--- ENVIRONMENT ---");

            // 1. Main Ground Arena
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Main";
            ground.transform.SetParent(environmentParent.transform);
            ground.transform.position = new Vector3(0, -0.5f, 0); // Top of ground cube is at Y = 0.0m
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            ground.GetComponent<Renderer>().material = matGround;

            // 2. Slide Ramps (Mild & Steep)
            // Mild Slope (18 degrees)
            GameObject rampMild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampMild.name = "Ramp_Mild_18Deg";
            rampMild.transform.SetParent(environmentParent.transform);
            rampMild.transform.position = new Vector3(-15f, 3.5f, 20f);
            rampMild.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            rampMild.transform.localScale = new Vector3(10f, 0.5f, 26f);
            rampMild.GetComponent<Renderer>().material = matRamp;

            // Steep Slope (32 degrees)
            GameObject rampSteep = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampSteep.name = "Ramp_Steep_32Deg";
            rampSteep.transform.SetParent(environmentParent.transform);
            rampSteep.transform.position = new Vector3(-3f, 7f, 20f);
            rampSteep.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            rampSteep.transform.localScale = new Vector3(10f, 0.5f, 28f);
            rampSteep.GetComponent<Renderer>().material = matRamp;

            // Starting high platform for ramps
            GameObject rampTopPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampTopPlat.name = "Platform_RampStart";
            rampTopPlat.transform.SetParent(environmentParent.transform);
            rampTopPlat.transform.position = new Vector3(-9f, 14f, 32f);
            rampTopPlat.transform.localScale = new Vector3(22f, 1f, 8f);
            rampTopPlat.GetComponent<Renderer>().material = matPlatform;

            // 3. Jumping Platforms Course
            for (int i = 0; i < 5; i++)
            {
                GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plat.name = $"Jump_Platform_{i + 1}";
                plat.transform.SetParent(environmentParent.transform);
                float posX = 15f + (i * 5.0f);
                float posY = 1.5f + (i * 2.0f);
                float posZ = -10f + (i * 4.0f);
                plat.transform.position = new Vector3(posX, posY, posZ);
                plat.transform.localScale = new Vector3(4.0f, 0.8f, 4.0f);
                plat.GetComponent<Renderer>().material = matPlatform;
            }

            // High reward platform
            GameObject highPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highPlat.name = "Platform_HighReward";
            highPlat.transform.SetParent(environmentParent.transform);
            highPlat.transform.position = new Vector3(40f, 12f, 10f);
            highPlat.transform.localScale = new Vector3(9f, 1f, 9f);
            highPlat.GetComponent<Renderer>().material = matPlatform;

            // 4. Low Clearance Slide Tunnel
            GameObject tunnelRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelRoof.name = "Tunnel_OverheadRoof";
            tunnelRoof.transform.SetParent(environmentParent.transform);
            tunnelRoof.transform.position = new Vector3(0f, 1.35f, -20f); // 1.35m height forces sliding!
            tunnelRoof.transform.localScale = new Vector3(10f, 0.4f, 16f);
            tunnelRoof.GetComponent<Renderer>().material = matRamp;

            GameObject pillarLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarLeft.transform.SetParent(environmentParent.transform);
            pillarLeft.transform.position = new Vector3(-5.2f, 0.6f, -20f);
            pillarLeft.transform.localScale = new Vector3(0.5f, 1.2f, 16f);
            pillarLeft.GetComponent<Renderer>().material = matRamp;

            GameObject pillarRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarRight.transform.SetParent(environmentParent.transform);
            pillarRight.transform.position = new Vector3(5.2f, 0.6f, -20f);
            pillarRight.transform.localScale = new Vector3(0.5f, 1.2f, 16f);
            pillarRight.GetComponent<Renderer>().material = matRamp;

            // 5. Build Player Character
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
            }
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0f, 0.1f, 0f); // Rest cleanly at Y = 0.0m

            CharacterController charController = playerObj.GetComponent<CharacterController>();
            if (charController == null) charController = playerObj.AddComponent<CharacterController>();
            charController.height = 1.8f;
            charController.center = new Vector3(0, 0.9f, 0);
            charController.radius = 0.4f;
            charController.stepOffset = 0.3f;
            charController.minMoveDistance = 0.001f;

            // Visual Model Container
            Transform visual = playerObj.transform.Find("VisualModel");
            if (visual == null)
            {
                GameObject visObj = new GameObject("VisualModel");
                visObj.transform.SetParent(playerObj.transform);
                visObj.transform.localPosition = Vector3.zero;
                visual = visObj.transform;

                // Body Capsule
                GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bodyObj.name = "BodyCapsule";
                bodyObj.transform.SetParent(visual);
                bodyObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                bodyObj.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                DestroyImmediate(bodyObj.GetComponent<Collider>());
                bodyObj.GetComponent<Renderer>().material = matPlayer;

                // Visor / Eyes
                GameObject visorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visorObj.name = "Visor";
                visorObj.transform.SetParent(visual);
                visorObj.transform.localPosition = new Vector3(0f, 1.35f, 0.32f);
                visorObj.transform.localScale = new Vector3(0.55f, 0.22f, 0.25f);
                DestroyImmediate(visorObj.GetComponent<Collider>());
                visorObj.GetComponent<Renderer>().material = matVisor;
            }

            // Attach Player Components
            PlayerController playerComp = playerObj.GetComponent<PlayerController>();
            if (playerComp == null) playerComp = playerObj.AddComponent<PlayerController>();

            PlayerVFX vfxComp = playerObj.GetComponent<PlayerVFX>();
            if (vfxComp == null) vfxComp = playerObj.AddComponent<PlayerVFX>();

            // 6. Camera Setup
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0f, 3f, -6f);
            mainCam.transform.LookAt(playerObj.transform.position + Vector3.up * 1.4f);

            SmoothCameraController camController = mainCam.GetComponent<SmoothCameraController>();
            if (camController == null) camController = mainCam.gameObject.AddComponent<SmoothCameraController>();
            camController.SetTarget(playerObj.transform);

            // 7. Lighting
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional) { sun = l; break; }
                }
            }
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun.color = new Color(1.0f, 0.95f, 0.85f);
                sun.intensity = 1.2f;
            }

            Debug.Log("<color=green><b>[Platformer 3D] Playground Level generated successfully! Player is ready for movement!</b></color>");
        }

        private Material CreateSimpleMaterial(string matName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.name = matName;
            mat.color = color;
            return mat;
        }
    }
}
