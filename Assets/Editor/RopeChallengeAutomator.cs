using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class RopeChallengeAutomator : EditorWindow
{
    [MenuItem("Tools/Ghost-Route/Automate Rope Challenge Setup")]
    public static void AutomateSetup()
    {
        // 1. Find or CREATE the Platforms
        GameObject entry = GameObject.Find("Platform_Entry");
        GameObject exit = GameObject.Find("Platform_Exit");

        if (entry == null || exit == null)
        {
            Debug.Log("Platforms not found. Generating default chasm platforms...");
            CreateDefaultPlatforms(out entry, out exit);
        }

        // 2. Create the Rope System
        GameObject ropeSystem = new GameObject("Rope_System_Core");
        Undo.RegisterCreatedObjectUndo(ropeSystem, "Auto Setup Rope");
        
        var verlet = ropeSystem.AddComponent<VerletRope>();
        var controller = ropeSystem.AddComponent<RopeCrossingController>();
        
        // 3. Configure the Rope
        Vector3 startPos = new Vector3(entry.transform.position.x, 0.2f, entry.transform.position.z + (entry.transform.localScale.z / 2));
        Vector3 endPos = new Vector3(exit.transform.position.x, 0.2f, exit.transform.position.z - (exit.transform.localScale.z / 2));
        
        verlet.nodeCount = 25;
        verlet.segmentLength = Vector3.Distance(startPos, endPos) / 24f;
        
        var lr = ropeSystem.GetComponent<LineRenderer>();
        lr.startWidth = 0.2f; 
        lr.endWidth = 0.2f;
        lr.textureMode = LineTextureMode.Tile; // Enable tiling
        lr.numCornerVertices = 4; // Round corners
        lr.numCapVertices = 4;    // Round caps
        
        Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sewer/Rope_Mat.mat");
        if (ropeMat == null) ropeMat = new Material(Shader.Find("Standard"));
        
        // Adjust tiling so it looks like braided rope
        ropeMat.mainTextureScale = new Vector2(10, 1); 
        lr.sharedMaterial = ropeMat;

        verlet.DeployRope(startPos, endPos);

        // 4. Setup the Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var move = player.GetComponent<PlayerRopeMovement>();
            if (move == null) move = player.AddComponent<PlayerRopeMovement>();
            move.verletRope = verlet;
            move.controller = controller;
        }

        // 5. Setup the Trigger
        GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trigger.name = "Rope_Start_Trigger";
        trigger.transform.position = startPos - new Vector3(0, 0, 0.5f);
        trigger.transform.localScale = new Vector3(2, 2, 2);
        trigger.GetComponent<BoxCollider>().isTrigger = true;
        trigger.GetComponent<MeshRenderer>().enabled = false;
        
        var trgScript = trigger.AddComponent<RopeCrossingTrigger>();
        trgScript.controller = controller;
        
        GameObject snap = new GameObject("Player_Snap_Point");
        snap.transform.SetParent(trigger.transform);
        snap.transform.position = startPos;
        trgScript.playerAttachPoint = snap.transform;

        // 6. Setup basic UI
        CreateBasicUI(controller);

        Debug.Log("Rope Challenge 100% Automated. Platforms, Player, Rope, and UI are linked.");
    }

    static void CreateDefaultPlatforms(out GameObject entry, out GameObject exit)
    {
        float platformWidth = 4f;
        float platformLength = 10f;
        float chasmGap = 6f;

        GameObject root = new GameObject("Sewer_Platforms_Structure");

        entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        entry.name = "Platform_Entry";
        entry.transform.SetParent(root.transform);
        entry.transform.position = new Vector3(0, 0, -platformLength / 2);
        entry.transform.localScale = new Vector3(platformWidth, 0.2f, platformLength);

        exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "Platform_Exit";
        exit.transform.SetParent(root.transform);
        exit.transform.position = new Vector3(0, 0, chasmGap + (platformLength / 2));
        exit.transform.localScale = new Vector3(platformWidth, 0.2f, platformLength);
        
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sewer/SewerFloor_Mat.mat");
        if (floorMat != null)
        {
            entry.GetComponent<Renderer>().material = floorMat;
            exit.GetComponent<Renderer>().material = floorMat;
        }
    }

    static void CreateBasicUI(RopeCrossingController controller)
    {
        if (GameObject.Find("Rope_UI_Canvas") != null) return;

        GameObject canvasObj = new GameObject("Rope_UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Prompt_Panel");
        panel.transform.SetParent(canvasObj.transform);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(0, -200);
        panelRect.sizeDelta = new Vector2(100, 100);

        GameObject textObj = new GameObject("Prompt_Text");
        textObj.transform.SetParent(panel.transform);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = ""; // Empty until prompt appears
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72;
        text.color = Color.white;
        
        // Ensure panel is at the bottom
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.2f);
        rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.anchoredPosition = Vector3.zero;

        var handler = canvasObj.AddComponent<RopeUIHandler>();
        handler.controller = controller;
        handler.uiPanel = panel;
        handler.promptText = text;
    }
}
