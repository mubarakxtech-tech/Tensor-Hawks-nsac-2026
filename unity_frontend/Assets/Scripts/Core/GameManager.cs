using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TensorHawks.Data;

namespace TensorHawks.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Panels (Canvas Screens)")]
        public GameObject introPanel;          // Screen 1: Main Menu
        public GameObject destinationPanel;    // Screen 2: Choose Destination
        public GameObject hangarPanel;         // Screen 3: Rocket Hangar Setup
        public GameObject orbitHudPanel;       // Screen 4: 3D Orbit Viewport HUD
        public GameObject telemetryDrawer;     // Screen 5: Telemetry Report

        [Header("Hangar Controls")]
        public Slider fuelSlider;
        public Slider payloadSlider;
        public TMP_Text deltaVRequirementText;

        [Header("Settings Controls")]
        public Slider volumeSlider;
        public TMP_Dropdown qualityDropdown;

        [Header("Telemetry UI Elements")]
        public TMP_Text statusBadgeText;
        public TMP_Text scoreText;
        public TMP_Text telemetryLogsText;

        [Header("Dependencies")]
        public WebApiClient webApiClient;

        private SimulationRequest currentRequest = new SimulationRequest();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ShowIntroScreen();

            // Initialize Settings values if assigned
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.AddListener(SetQuality);
            }
        }

        // --- SCREEN TRANSITIONS ---
        public void ShowIntroScreen()
        {
            SetPanelStates(intro: true, dest: false, hangar: false, orbit: false, telem: false);
        }

        public void ShowDestinationScreen()
        {
            SetPanelStates(intro: false, dest: true, hangar: false, orbit: false, telem: false);
        }

        public void SelectDestination(string destinationName)
        {
            currentRequest.target_destination = destinationName;
            SetPanelStates(intro: false, dest: false, hangar: true, orbit: false, telem: false);
        }

        public void SelectRocket(string rocketName)
        {
            currentRequest.selected_rocket = rocketName;
        }

        // --- SYSTEM SETTINGS CONTROLS ---
        public void SetVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        // --- SIMULATION EXECUTION ---
        public void OnRunSimulationClicked()
        {
            currentRequest.total_fuel_mass_kg = fuelSlider != null ? fuelSlider.value : 45000f;
            currentRequest.payload_mass_kg = payloadSlider != null ? payloadSlider.value : 5000f;

            // Transition to Screen 4 (3D Orbit) while request executes
            SetPanelStates(intro: false, dest: false, hangar: false, orbit: true, telem: false);

            if (webApiClient != null)
            {
                webApiClient.SendSimulationData(
                    currentRequest,
                    onSuccess: OnSimulationSuccess,
                    onError: OnSimulationError
                );
            }
            else
            {
                OnSimulationError("WebApiClient reference missing on GameManager.");
            }
        }

        private void OnSimulationSuccess(SimulationResponse response)
        {
            // Open Screen 5 (Telemetry Drawer) overlaying Screen 4
            telemetryDrawer.SetActive(true);

            if (statusBadgeText != null)
                statusBadgeText.text = response.mission_status == "PASSING" ? "MISSION SUCCESS" : "MISSION FAILED";
            if (scoreText != null)
                scoreText.text = $"{response.science_score} PTS";
            if (telemetryLogsText != null)
                telemetryLogsText.text = string.Join("\n• ", response.mission_log);
        }

        private void OnSimulationError(string errorMessage)
        {
            Debug.LogError($"[GameManager] {errorMessage}");
            if (telemetryDrawer != null) telemetryDrawer.SetActive(true);
            if (statusBadgeText != null) statusBadgeText.text = "OFFLINE TEST MODE";
        }

        // --- APPLICATION EXIT ---
        public void QuitGame()
        {
            Debug.Log("Quitting Game...");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPanelStates(bool intro, bool dest, bool hangar, bool orbit, bool telem)
        {
            if (introPanel) introPanel.SetActive(intro);
            if (destinationPanel) destinationPanel.SetActive(dest);
            if (hangarPanel) hangarPanel.SetActive(hangar);
            if (orbitHudPanel) orbitHudPanel.SetActive(orbit);
            if (telemetryDrawer) telemetryDrawer.SetActive(telem);
        }
    }
}
