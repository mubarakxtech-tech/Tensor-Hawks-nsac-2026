using UnityEngine;
using UnityEngine.UI;

public class TelemetryDrawerController : MonoBehaviour
{
    [Header("Drawer Animation")]
    public RectTransform drawerPanel;
    public float slideSpeed = 8f;
    private Vector2 closedPos;
    private Vector2 openPos;
    private bool isOpen = false;

    [Header("UI Text Displays")]
    public Text statusTitleText;
    public Text deltaVText;
    public Text payloadText;
    public Text fuelRemainingText;
    public Text flightLogText;

    void Start()
    {
        if (drawerPanel != null)
        {
            closedPos = new Vector2(drawerPanel.rect.width, 0);
            openPos = Vector2.zero;
            drawerPanel.anchoredPosition = closedPos;
        }
    }

    void Update()
    {
        if (drawerPanel == null) return;
        Vector2 targetPos = isOpen ? openPos : closedPos;
        drawerPanel.anchoredPosition = Vector2.Lerp(drawerPanel.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    public void DisplaySimulationResults(bool isSuccess, string destination, float reqDeltaV, float achievedDeltaV, float payloadKg, float fuelLeftKg, string logDetails)
    {
        if (statusTitleText != null)
        {
            statusTitleText.text = isSuccess ? $"<color=#00FFCC>MISSION SUCCESS — {destination.ToUpper()} ORBIT ACHIEVED</color>"
                                            : $"<color=#FF3366>MISSION FAILED — INSIGHT INSUFFICIENT DELTA-V</color>";
        }

        if (deltaVText != null)
            deltaVText.text = $"Delta-V Achieved: {achievedDeltaV:F0} m/s (Req: {reqDeltaV:F0} m/s)";

        if (payloadText != null)
            payloadText.text = $"Payload Delivered: {payloadKg:F0} kg";

        if (fuelRemainingText != null)
            fuelRemainingText.text = $"Fuel Remaining: {fuelLeftKg:F0} kg";

        if (flightLogText != null)
            flightLogText.text = logDetails;

        isOpen = true;
    }

    public void CloseDrawer()
    {
        isOpen = false;
    }
}
