using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Generates a smooth, beginner-friendly 2D Speedrun Platformer Level.
    /// Balanced elevation with gentle low-wall jumps, spring pads, crumbling bridges,
    /// speed pads, and checkpoints.
    /// </summary>
    public class PlaygroundBuilder2D : MonoBehaviour
    {
        [Header("Options")]
        [SerializeField] private bool buildOnStart = true;

        [Header("Colors")]
        [SerializeField] private Color groundColor = new Color(0.15f, 0.18f, 0.25f);
        [SerializeField] private Color platformColor = new Color(0.15f, 0.65f, 0.55f);
        [SerializeField] private Color doorColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color movingColor = new Color(0.3f, 0.5f, 0.9f);
        [SerializeField] private Color wallColor = new Color(0.35f, 0.3f, 0.25f);
        [SerializeField] private Color springColor = new Color(0.7f, 0.2f, 0.85f);
        [SerializeField] private Color crumbleColor = new Color(0.95f, 0.5f, 0.15f);
        [SerializeField] private Color boostColor = new Color(0.0f, 0.85f, 0.95f);
        [SerializeField] private Color playerColor = new Color(0.0f, 0.8f, 1.0f);
        [SerializeField] private Color visorColor = new Color(0.05f, 0.1f, 0.15f);
        [SerializeField] private Color goalColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color checkpointColor = new Color(0.2f, 0.8f, 0.9f);

        private void Start()
        {
            if (buildOnStart) BuildLevel();
        }

        [ContextMenu("Build Challenge Level")]
        public void BuildLevel()
        {
            // Clean old level
            var old = GameObject.Find("--- LEVEL ---");
            if (old != null) DestroyImmediate(old);
            var oldPlayer = GameObject.Find("Player2D");
            if (oldPlayer != null) DestroyImmediate(oldPlayer);
            var oldCanvas = GameObject.Find("WinCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);
            var oldHUD = GameObject.Find("SpeedrunHUDCanvas");
            if (oldHUD != null) DestroyImmediate(oldHUD);
            var oldES = GameObject.Find("EventSystem");
            if (oldES != null) DestroyImmediate(oldES);

            GameObject root = new GameObject("--- LEVEL ---");

            // Materials
            Material matGround = MakeMat(groundColor);
            Material matPlat = MakeMat(platformColor);
            Material matDoor = MakeMat(doorColor);
            Material matMoving = MakeMat(movingColor);
            Material matWall = MakeMat(wallColor);
            Material matSpring = MakeMat(springColor);
            Material matCrumble = MakeMat(crumbleColor);
            Material matBoost = MakeMat(boostColor);
            Material matPlayer = MakeMat(playerColor);
            Material matVisor = MakeMat(visorColor);
            Material matGoal = MakeMat(goalColor);
            Material matCP = MakeMat(checkpointColor);

            // ═══════════════════════════════════════════
            // SECTION 1: Starting Zone + Checkpoint 1
            // ═══════════════════════════════════════════
            MakeBlock(root, "Ground_Start", matGround, new Vector3(-2f, -0.5f, 0), new Vector3(14f, 1f, 2f));
            MakeCheckpoint(root, "CP_Start", new Vector3(-2f, 0.5f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 2: Wall-to-Wall Climbing Shaft
            // ═══════════════════════════════════════════
            MakeBlock(root, "WallClimb_Ground", matGround, new Vector3(8f, -0.5f, 0), new Vector3(6f, 1f, 2f));

            // Two parallel walls spaced 4.5m apart for zig-zag wall jumping
            MakeBlock(root, "WallClimb_LeftWall", matWall, new Vector3(10f, 4.5f, 0), new Vector3(1.2f, 9f, 2f));
            MakeBlock(root, "WallClimb_RightWall", matWall, new Vector3(14.5f, 4.5f, 0), new Vector3(1.2f, 9f, 2f));

            // Upper landing platform at the top of the shaft
            MakeBlock(root, "Plat_Section2_Landing", matGround, new Vector3(21f, 8.5f, 0), new Vector3(10f, 1f, 2f));
            MakeCheckpoint(root, "CP_Section2", new Vector3(20f, 9.5f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 3: Timing Door Gate 1
            // ═══════════════════════════════════════════
            MakeBlock(root, "Wall_Door1_Top", matWall, new Vector3(25f, 13.5f, 0), new Vector3(1.2f, 5f, 2f));
            var door1 = MakeBlock(root, "TimingDoor_1", matDoor, new Vector3(25f, 10.2f, 0), new Vector3(1.2f, 2.5f, 2f));
            var td1 = door1.AddComponent<TimingDoor>();
            td1.OpenTime = 1.8f;
            td1.CloseTime = 1.8f;
            td1.StartDelay = 0f;

            MakeBlock(root, "Plat_AfterDoor1", matGround, new Vector3(31f, 8.5f, 0), new Vector3(8f, 1f, 2f));

            // ═══════════════════════════════════════════
            // SECTION 4: Spring Launch Pad
            // ═══════════════════════════════════════════
            MakeSpringPad(root, "SpringPad_1", matSpring, new Vector3(32.5f, 9.3f, 0), new Vector3(2.5f, 0.6f, 2f), 16f);

            MakeBlock(root, "Plat_SpringTarget", matPlat, new Vector3(40f, 13.5f, 0), new Vector3(7f, 1f, 2f));
            MakeCheckpoint(root, "CP_Section4", new Vector3(40f, 14.5f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 5: Crumbling Bridge Chasm
            // ═══════════════════════════════════════════
            MakeCrumblePad(root, "Crumble_1", matCrumble, new Vector3(47f, 13.0f, 0), new Vector3(2.5f, 0.6f, 2f));
            MakeCrumblePad(root, "Crumble_2", matCrumble, new Vector3(52f, 12.5f, 0), new Vector3(2.5f, 0.6f, 2f));
            MakeCrumblePad(root, "Crumble_3", matCrumble, new Vector3(57f, 12.0f, 0), new Vector3(2.5f, 0.6f, 2f));

            MakeBlock(root, "Plat_Section6_Start", matGround, new Vector3(64f, 11.0f, 0), new Vector3(7f, 1f, 2f));
            MakeCheckpoint(root, "CP_Section5", new Vector3(64f, 12.0f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 6: Speed Sprint Track
            // ═══════════════════════════════════════════
            MakeSpeedPad(root, "SpeedBoost_1", matBoost, new Vector3(65.5f, 11.8f, 0), new Vector3(3.5f, 0.6f, 2f), 24f);

            MakeBlock(root, "Plat_SpeedLanding", matGround, new Vector3(82f, 11.0f, 0), new Vector3(8f, 1f, 2f));
            MakeCheckpoint(root, "CP_Section6", new Vector3(82f, 12.0f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 7: Moving Platform Gap
            // ═══════════════════════════════════════════
            var movPlat1 = MakeBlock(root, "MovingPlat_1", matMoving, new Vector3(90f, 11.0f, 0), new Vector3(3.5f, 0.6f, 2f));
            var mp1 = movPlat1.AddComponent<MovingPlatform>();
            mp1.MoveDirection = Vector2.right;
            mp1.MoveDistance = 7f;
            mp1.MoveSpeed = 2.5f;

            MakeBlock(root, "Plat_Section8", matGround, new Vector3(104f, 11.0f, 0), new Vector3(10f, 1f, 2f));

            // ═══════════════════════════════════════════
            // SECTION 8: Double Door Gauntlet
            // ═══════════════════════════════════════════
            MakeBlock(root, "Wall_Door2_Top", matWall, new Vector3(101f, 16f, 0), new Vector3(1.2f, 5f, 2f));
            var door2 = MakeBlock(root, "TimingDoor_2", matDoor, new Vector3(101f, 12.7f, 0), new Vector3(1.2f, 2.5f, 2f));
            var td2 = door2.AddComponent<TimingDoor>();
            td2.OpenTime = 1.5f;
            td2.CloseTime = 1.8f;
            td2.StartDelay = 0f;

            MakeBlock(root, "Wall_Door3_Top", matWall, new Vector3(107f, 16f, 0), new Vector3(1.2f, 5f, 2f));
            var door3 = MakeBlock(root, "TimingDoor_3", matDoor, new Vector3(107f, 12.7f, 0), new Vector3(1.2f, 2.5f, 2f));
            var td3 = door3.AddComponent<TimingDoor>();
            td3.OpenTime = 1.4f;
            td3.CloseTime = 1.6f;
            td3.StartDelay = 0.7f;

            MakeCheckpoint(root, "CP_Section8", new Vector3(108f, 12.0f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 9: Vertical Moving Platforms
            // ═══════════════════════════════════════════
            var movPlat2 = MakeBlock(root, "VertPlat_1", matMoving, new Vector3(115f, 3f, 0), new Vector3(3.5f, 0.6f, 2f));
            var mp2 = movPlat2.AddComponent<MovingPlatform>();
            mp2.MoveDirection = Vector2.up;
            mp2.MoveDistance = 4f;
            mp2.MoveSpeed = 2f;

            MakeBlock(root, "Plat_Section10", matGround, new Vector3(127f, 4f, 0), new Vector3(14f, 1f, 2f));
            MakeCheckpoint(root, "CP_Section10", new Vector3(123f, 5.0f, 0), matCP);

            // ═══════════════════════════════════════════
            // SECTION 10: Slide Tunnel
            // ═══════════════════════════════════════════
            MakeBlock(root, "LowCeiling", matWall, new Vector3(127f, 5.8f, 0), new Vector3(6f, 0.4f, 2f));

            MakeBlock(root, "Wall_Door4_Top", matWall, new Vector3(132f, 9f, 0), new Vector3(1.2f, 5f, 2f));
            var door4 = MakeBlock(root, "TimingDoor_4", matDoor, new Vector3(132f, 5.7f, 0), new Vector3(1.2f, 2.5f, 2f));
            var td4 = door4.AddComponent<TimingDoor>();
            td4.OpenTime = 1.7f;
            td4.CloseTime = 1.5f;
            td4.StartDelay = 0.2f;

            // ═══════════════════════════════════════════
            // SECTION 11: Final Air-Dash & Timing Door
            // ═══════════════════════════════════════════
            MakeBlock(root, "TinyPlat_AirDashTarget", matPlat, new Vector3(140f, 4f, 0), new Vector3(3f, 0.6f, 2f));

            MakeBlock(root, "Ground_PreGoal", matGround, new Vector3(150f, 4f, 0), new Vector3(8f, 1f, 2f));
            MakeCheckpoint(root, "CP_FinalDoor", new Vector3(148f, 5.0f, 0), matCP);

            MakeBlock(root, "Wall_DoorFinal_Top", matWall, new Vector3(151f, 9f, 0), new Vector3(1.2f, 5f, 2f));
            var doorFinal = MakeBlock(root, "TimingDoor_Final", matDoor, new Vector3(151f, 5.7f, 0), new Vector3(1.2f, 2.5f, 2f));
            var tdf = doorFinal.AddComponent<TimingDoor>();
            tdf.OpenTime = 1.8f;
            tdf.CloseTime = 1.8f;
            tdf.StartDelay = 0f;

            // ═══════════════════════════════════════════
            // SECTION 12: Goal Platform & Managers
            // ═══════════════════════════════════════════
            MakeBlock(root, "Ground_Goal", matGround, new Vector3(161f, 4f, 0), new Vector3(8f, 1f, 2f));
            var goalFlag = MakeBlock(root, "GoalFlag", matGoal, new Vector3(161f, 6.5f, 0), new Vector3(0.5f, 4f, 2f));
            MakeBlock(root, "GoalBanner", matGoal, new Vector3(161.8f, 8f, 0), new Vector3(1.5f, 0.8f, 2f));

            goalFlag.AddComponent<GoalTrigger2D>();

            // Managers
            root.AddComponent<WinManager2D>();
            root.AddComponent<SpeedrunTimer2D>();

            // ═══════════════════════════════════════════
            // PLAYER
            // ═══════════════════════════════════════════
            GameObject player = new GameObject("Player2D");
            player.tag = "Player";
            player.transform.position = new Vector3(-2f, 1f, 0f);

            var rb2d = player.AddComponent<Rigidbody2D>();
            rb2d.freezeRotation = true;
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var cap = player.AddComponent<CapsuleCollider2D>();
            cap.size = new Vector2(0.8f, 1.8f);
            cap.offset = new Vector2(0f, 0.9f);
            cap.direction = CapsuleDirection2D.Vertical;

            GameObject vis = new GameObject("VisualModel");
            vis.transform.SetParent(player.transform);
            vis.transform.localPosition = Vector3.zero;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(vis.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().material = matPlayer;

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(vis.transform);
            visor.transform.localPosition = new Vector3(0.32f, 1.35f, 0f);
            visor.transform.localScale = new Vector3(0.25f, 0.22f, 0.55f);
            DestroyImmediate(visor.GetComponent<Collider>());
            visor.GetComponent<Renderer>().material = matVisor;

            player.AddComponent<PlayerController2D>();
            player.AddComponent<PlayerVFX2D>();

            // ═══════════════════════════════════════════
            // CAMERA
            // ═══════════════════════════════════════════
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 7.0f;
            cam.transform.position = new Vector3(-2f, 3f, -10f);

            var camCtrl = cam.GetComponent<SmoothCamera2D>();
            if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<SmoothCamera2D>();
            camCtrl.SetTarget(player.transform);

            // ═══════════════════════════════════════════
            // LIGHTING
            // ═══════════════════════════════════════════
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun == null)
            {
                var sunObj = new GameObject("Sun");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(40f, -20f, 0f);
                sun.color = new Color(1f, 0.95f, 0.88f);
                sun.intensity = 1.2f;
            }

            Debug.Log("<color=green><b>[LEVEL GENERATED] Smooth, Beginner-Friendly Platformer Level Ready!</b></color>");
        }

        // ── Helpers ──

        private GameObject MakeBlock(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = mat;

            DestroyImmediate(obj.GetComponent<BoxCollider>());
            obj.AddComponent<BoxCollider2D>();

            return obj;
        }

        private GameObject MakeSpringPad(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale, float force)
        {
            var obj = MakeBlock(parent, name, mat, pos, scale);
            var sp = obj.AddComponent<SpringPad>();
            return obj;
        }

        private GameObject MakeCrumblePad(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale)
        {
            var obj = MakeBlock(parent, name, mat, pos, scale);
            obj.AddComponent<CrumblingPlatform>();
            return obj;
        }

        private GameObject MakeSpeedPad(GameObject parent, string name, Material mat, Vector3 pos, Vector3 scale, float speed)
        {
            var obj = MakeBlock(parent, name, mat, pos, scale);
            var sb = obj.AddComponent<SpeedBoostPad>();
            return obj;
        }

        private GameObject MakeCheckpoint(GameObject parent, string name, Vector3 pos, Material mat)
        {
            var flagObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagObj.name = name;
            flagObj.transform.SetParent(parent.transform);
            flagObj.transform.position = pos + new Vector3(0f, 1f, 0f);
            flagObj.transform.localScale = new Vector3(0.2f, 1f, 0.2f);
            flagObj.GetComponent<Renderer>().material = mat;

            DestroyImmediate(flagObj.GetComponent<CapsuleCollider>());
            var triggerBox = flagObj.AddComponent<BoxCollider2D>();
            triggerBox.size = new Vector2(2.5f, 3.0f);
            triggerBox.isTrigger = true;

            flagObj.AddComponent<Checkpoint2D>();

            return flagObj;
        }

        private Material MakeMat(Color color)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            mat.color = color;
            return mat;
        }
    }
}
