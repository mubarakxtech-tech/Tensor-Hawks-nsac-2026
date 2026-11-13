#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TensorHawks.Core;

public class TensorHawksAutoBuilder : EditorWindow
{
    [MenuItem("Tensor Hawks/Build Screens 2 & 3 Now")]
    public static void BuildScreens()
    {
        // 1. Ensure Canvas & EventSystem exist
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // 2. Ensure GameManager exists
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }

        // Clean up old generated panels if re-running
        Transform oldDest = canvas.transform.Find("DestinationPanel");
        if (oldDest != null) DestroyImmediate(oldDest.gameObject);
        Transform oldHangar = canvas.transform.Find("HangarPanel");
        if (oldHangar != null) DestroyImmediate(oldHangar.gameObject);

        Color darkBg = new Color(0.05f, 0.12f, 0.22f, 0.96f);
        Color cardBg = new Color(0.08f, 0.20f, 0.32f, 1f);
        Color cyanAccent = new Color(0.0f, 0.85f, 0.95f, 1f);

        // ==========================================
        // BUILD SCREEN 2: DESTINATION PANEL
        // ==========================================
        GameObject destPanel = CreatePanel("DestinationPanel", canvas.transform, darkBg);
        gm.destinationPanel = destPanel;

        CreateTMPText("Header", destPanel.transform, "CHOOSE YOUR DESTINATION", 44, cyanAccent, new Vector2(0, 420), new Vector2(1000, 70));

        // Horizontal Row for 4 Planets
        GameObject planetRow = new GameObject("PlanetRow", typeof(RectTransform));
        planetRow.transform.SetParent(destPanel.transform, false);
        RectTransform planetRowRT = planetRow.GetComponent<RectTransform>();
        planetRowRT.anchoredPosition = new Vector2(0, 80);
        planetRowRT.sizeDelta = new Vector2(1500, 420);
        HorizontalLayoutGroup planetHLG = planetRow.AddComponent<HorizontalLayoutGroup>();
        planetHLG.spacing = 30;
        planetHLG.childAlignment = TextAnchor.MiddleCenter;
        planetHLG.childForceExpandWidth = true;
        planetHLG.childForceExpandHeight = true;

        string[] planets = { "Moon Orbit", "Mars Orbit", "Jupiter Orbit", "Saturn Orbit" };
        string[] spriteFiles = { "Assets/Sprites/Moon.jpg", "Assets/Sprites/Mars.jpg", "Assets/Sprites/Jupiter.jpg", "Assets/Sprites/Saturn.jpg" };
        string[] specs = { "Delta-V: 3,800 m/s", "Delta-V: 6,200 m/s", "Delta-V: 12,500 m/s", "Delta-V: 15,000 m/s" };

        for (int i = 0; i < planets.Length; i++)
        {
            string pName = planets[i];
            Button btn = CreateCardButton(pName.Replace(" ", "") + "_Btn", planetRow.transform, pName.ToUpper(), specs[i], spriteFiles[i], cardBg, cyanAccent);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(btn.onClick, gm.SelectDestination, pName);
        }

        // Readout Text
        TMP_Text destInfo = CreateTMPText("DestinationInfoText", destPanel.transform, "TARGET: MOON ORBIT | REQUIRED DELTA-V: 3,800 M/S", 30, Color.white, new Vector2(0, -210), new Vector2(1200, 60));
        gm.destinationInfoText = destInfo;

        // Confirm Button -> Goes to Screen 3
        Button confirmBtn = CreateActionButton("ConfirmDestinationBtn", destPanel.transform, "CONFIRM DESTINATION", cyanAccent, Color.black, new Vector2(0, -340), new Vector2(450, 75));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmBtn.onClick, gm.ShowHangarScreen);

        // ==========================================
        // BUILD SCREEN 3: ROCKET HANGAR PANEL
        // ==========================================
        GameObject hangarPanel = CreatePanel("HangarPanel", canvas.transform, darkBg);
        gm.hangarPanel = hangarPanel;

        CreateTMPText("HangarHeader", hangarPanel.transform, "SELECT LAUNCH VEHICLE", 44, cyanAccent, new Vector2(0, 430), new Vector2(1000, 70));

        // Horizontal Row for 3 Rockets
        GameObject rocketRow = new GameObject("RocketCardGrid", typeof(RectTransform));
        rocketRow.transform.SetParent(hangarPanel.transform, false);
        RectTransform rocketRowRT = rocketRow.GetComponent<RectTransform>();
        rocketRowRT.anchoredPosition = new Vector2(0, 140);
        rocketRowRT.sizeDelta = new Vector2(1300, 360);
        HorizontalLayoutGroup rocketHLG = rocketRow.AddComponent<HorizontalLayoutGroup>();
        rocketHLG.spacing = 40;
        rocketHLG.childAlignment = TextAnchor.MiddleCenter;
        rocketHLG.childForceExpandWidth = true;
        rocketHLG.childForceExpandHeight = true;

        string[] rockets = { "Saturn V", "SLS Block 1", "Falcon 9" };
        string[] rocketSprites = { "Assets/Sprites/Rocket_1.jpg", "Assets/Sprites/Rocket_2.jpg", "Assets/Sprites/Rocket_3.jpg" };
        string[] rocketSpecs = { "Heavy Lift | 34.5 MN", "Deep Space | 39.1 MN", "Medium Lift | 7.6 MN" };

        for (int i = 0; i < rockets.Length; i++)
        {
            string rName = rockets[i];
            Button rBtn = CreateCardButton(rName.Replace(" ", "") + "_Card", rocketRow.transform, rName.ToUpper(), rocketSpecs[i], rocketSprites[i], cardBg, cyanAccent);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(rBtn.onClick, gm.SelectRocket, rName);
        }

        // Selected Rocket Readout
        TMP_Text rocketReadout = CreateTMPText("SelectedRocketText", hangarPanel.transform, "SELECTED LAUNCH VEHICLE: SATURN V", 26, cyanAccent, new Vector2(0, -80), new Vector2(900, 45));
        gm.selectedRocketText = rocketReadout;

        // Fuel Slider
        gm.fuelText = CreateTMPText("FuelText", hangarPanel.transform, "LIQUID FUEL MASS: 50,000 KG", 24, Color.white, new Vector2(-280, -150), new Vector2(450, 40));
        gm.fuelSlider = CreateSlider("FuelSlider", hangarPanel.transform, 0, 100000, 50000, new Vector2(220, -150), cyanAccent);

        // Payload Slider
        gm.payloadText = CreateTMPText("PayloadText", hangarPanel.transform, "PAYLOAD MASS: 5,000 KG", 24, Color.white, new Vector2(-280, -230), new Vector2(450, 40));
        gm.payloadSlider = CreateSlider("PayloadSlider", hangarPanel.transform, 1000, 20000, 5000, new Vector2(220, -230), cyanAccent);

        // Back & Launch Buttons
        Button backBtn = CreateActionButton("BackToDestBtn", hangarPanel.transform, "< BACK", cardBg, Color.white, new Vector2(-260, -350), new Vector2(260, 75));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(backBtn.onClick, gm.ShowDestinationScreen);

        Button launchBtn = CreateActionButton("LaunchButton", hangarPanel.transform, "PREPARE FOR LAUNCH", cyanAccent, Color.black, new Vector2(140, -350), new Vector2(460, 75));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(launchBtn.onClick, gm.OnRunSimulationClicked);

        // Start with Screen 2 visible and Screen 3 hidden
        destPanel.SetActive(true);
        hangarPanel.SetActive(false);

        EditorUtility.SetDirty(gm);
        Debug.Log("✅ Tensor Hawks Screen 2 & Screen 3 built and wired successfully! Press Play to test.");
    }

    static GameObject CreatePanel(string name, Transform parent, Color bg)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = bg;
        return panel;
    }

    static TMP_Text CreateTMPText(string name, Transform parent, string content, int fontSize, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    static Button CreateCardButton(string name, Transform parent, string title, string subtitle, string spritePath, Color bg, Color accent)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = bg;
        Button btn = card.GetComponent<Button>();

        // Image preview
        GameObject imgObj = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(card.transform, false);
        RectTransform imgRT = imgObj.GetComponent<RectTransform>();
        imgRT.anchoredPosition = new Vector2(0, 45);
        imgRT.sizeDelta = new Vector2(220, 200);

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        if (tex != null)
        {
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            imgObj.GetComponent<Image>().sprite = sp;
        }

        CreateTMPText("Title", card.transform, title, 24, accent, new Vector2(0, -90), new Vector2(300, 40));
        CreateTMPText("Subtitle", card.transform, subtitle, 18, Color.white, new Vector2(0, -130), new Vector2(300, 35));
        return btn;
    }

    static Button CreateActionButton(string name, Transform parent, string label, Color bg, Color textColor, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        btnObj.GetComponent<Image>().color = bg;
        CreateTMPText("Label", btnObj.transform, label, 26, textColor, Vector2.zero, size);
        return btnObj.GetComponent<Button>();
    }

    static Slider CreateSlider(string name, Transform parent, float min, float max, float defaultVal, Vector2 pos, Color fillCol)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(500, 30);

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = fillCol;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = Vector2.zero;
        haRT.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRT = handle.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(28, 42);
        handle.GetComponent<Image>().color = Color.white;

        Slider s = sliderObj.GetComponent<Slider>();
        s.fillRect = fillRT;
        s.handleRect = hRT;
        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = true;
        s.value = defaultVal;
        return s;
    }
}
#endif
