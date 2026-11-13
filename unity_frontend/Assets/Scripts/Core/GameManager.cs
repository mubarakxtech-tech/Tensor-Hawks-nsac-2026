using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
        public GameObject settingsPanel;       // Settings Overlay

        [Header("Hangar & Settings Controls")]
        public Slider fuelSlider;
        public Slider payloadSlider;
        public Slider volumeSlider;
        public TMP_Dropdown qualityDropdown;
        public TMP_Text deltaVRequirementText;

        [Header("AutoBuilder UI Text Bindings")]
        public TMP_Text destinationInfoText;
        public TMP_Text selectedRocketText;
        public TMP_Text fuelText;
        public TMP_Text payloadText;

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
            AutoDiscoverUIReferences();
            InitializeAudioAndSettings();
            BindSliderEvents();
            ShowIntroScreen();
        }

        private void OnEnable()
        {
            BindVolumeSlider();
            BindSliderEvents();
        }

        private void BindSliderEvents()
        {
            if (fuelSlider != null)
            {
                fuelSlider.onValueChanged.RemoveAllListeners();
                fuelSlider.onValueChanged.AddListener(OnFuelSliderChanged);
                OnFuelSliderChanged(fuelSlider.value);
            }

            if (payloadSlider != null)
            {
                payloadSlider.onValueChanged.RemoveAllListeners();
                payloadSlider.onValueChanged.AddListener(OnPayloadSliderChanged);
                OnPayloadSliderChanged(payloadSlider.value);
            }
        }

        public void OnFuelSliderChanged(float val)
        {
            if (fuelText != null)
            {
                fuelText.text = $"LIQUID FUEL MASS: {val:N0} KG";
            }
        }

        public void OnPayloadSliderChanged(float val)
        {
            if (payloadText != null)
            {
                payloadText.text = $"PAYLOAD MASS: {val:N0} KG";
            }
        }

        private void AutoDiscoverUIReferences()
        {
            if (introPanel == null) introPanel = GameObject.Find("IntroPanel");
            if (destinationPanel == null) destinationPanel = GameObject.Find("DestinationPanel");
            if (hangarPanel == null) hangarPanel = GameObject.Find("HangarPanel");
            if (orbitHudPanel == null) orbitHudPanel = GameObject.Find("OrbitHudPanel");
            if (telemetryDrawer == null) telemetryDrawer = GameObject.Find("TelemetryDrawer");
            if (settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");

            if (volumeSlider == null)
            {
                Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Slider s in sliders)
                {
                    if (s.name.ToLower().Contains("volume"))
                    {
                        volumeSlider = s;
                        break;
                    }
                }
            }

            if (qualityDropdown == null)
            {
                TMP_Dropdown[] dropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (TMP_Dropdown d in dropdowns)
                {
                    if (d.name.ToLower().Contains("quality"))
                    {
                        qualityDropdown = d;
                        break;
                    }
                }
            }

            if (webApiClient == null) webApiClient = FindFirstObjectByType<WebApiClient>();
        }

        public void InitializeAudioAndSettings()
        {
            if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            AudioListener.volume = savedVolume;

            BindVolumeSlider();

            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.RemoveAllListeners();
                qualityDropdown.onValueChanged.AddListener(SetQuality);
            }
        }

        private void BindVolumeSlider()
        {
            if (volumeSlider == null) AutoDiscoverUIReferences();

            if (volumeSlider != null)
            {
                float currentVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
                volumeSlider.value = currentVol;
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }

        public void SetVolume(float volume)
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.Save();

            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source != null) source.volume = volume;
            }
        }

        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        public void ToggleSettingsPanel(bool isOpen)
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(isOpen);
                if (isOpen) BindVolumeSlider();
            }
        }

        public void ToggleSettingsPanel()
        {
            if (settingsPanel != null)
            {
                bool newState = !settingsPanel.activeSelf;
                settingsPanel.SetActive(newState);
                if (newState) BindVolumeSlider();
            }
        }

        // --- SCREEN TRANSITIONS ---
        public void ShowIntroScreen()
        {
            SetPanelStates(intro: true, dest: false, hangar: false, orbit: false, telem: false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        public void ShowDestinationScreen()
        {
            SetPanelStates(intro: false, dest: true, hangar: false, orbit: false, telem: false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        public void ShowHangarScreen()
        {
            SetPanelStates(intro: false, dest: false, hangar: true, orbit: false, telem: false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        public void SelectDestination(string destinationName)
        {
            currentRequest.target_destination = destinationName;

            if (destinationInfoText != null)
            {
                string reqDeltaV = "3,800 M/S";
                if (destinationName.ToLower().Contains("mars")) reqDeltaV = "6,200 M/S";
                else if (destinationName.ToLower().Contains("jupiter")) reqDeltaV = "12,500 M/S";
                else if (destinationName.ToLower().Contains("saturn")) reqDeltaV = "15,000 M/S";

                destinationInfoText.text = $"TARGET: {destinationName.ToUpper()} | REQUIRED DELTA-V: {reqDeltaV}";
            }

            ShowHangarScreen();
        }

        public void SelectRocket(string rocketName)
        {
            currentRequest.selected_rocket = rocketName;

            if (selectedRocketText != null)
            {
                selectedRocketText.text = $"SELECTED LAUNCH VEHICLE: {rocketName.ToUpper()}";
            }
        }

        public void OnRunSimulationClicked()
        {
            currentRequest.total_fuel_mass_kg = fuelSlider != null ? fuelSlider.value : 45000f;
            currentRequest.payload_mass_kg = payloadSlider != null ? payloadSlider.value : 5000f;

            if (string.IsNullOrEmpty(currentRequest.selected_rocket))
            {
                currentRequest.selected_rocket = "Saturn V";
            }

            // Transition cleanly to 3D Orbit Viewport - Hide menu overlays & settings
            SetPanelStates(intro: false, dest: false, hangar: false, orbit: true, telem: false);
            if (settingsPanel) settingsPanel.SetActive(false);

            // Execute 3D Flight sequence with dynamic destination and rocket selection
            InterplanetaryJourney journey = FindFirstObjectByType<InterplanetaryJourney>();
            if (journey != null)
            {
                string target = !string.IsNullOrEmpty(currentRequest?.target_destination)
                    ? currentRequest.target_destination
                    : (!string.IsNullOrEmpty(journey.testDestination) ? journey.testDestination : "Mars");

                string selectedRocket = !string.IsNullOrEmpty(currentRequest?.selected_rocket)
                    ? currentRequest.selected_rocket
                    : "Saturn V";

                // Pass BOTH target destination AND selected rocket model (7 arguments)
                journey.StartFlightSequence(
                    target,
                    selectedRocket,
                    true,
                    3800f,
                    4200f,
                    currentRequest != null ? currentRequest.payload_mass_kg : 500f,
                    currentRequest != null ? currentRequest.total_fuel_mass_kg : 100f
                );
            }

            if (webApiClient != null)
            {
                webApiClient.SendSimulationData(
                    currentRequest,
                    onSuccess: OnSimulationSuccess,
                    onError: OnSimulationError
                );
            }
        }

        private void OnSimulationSuccess(SimulationResponse response)
        {
            if (telemetryDrawer != null) telemetryDrawer.SetActive(true);

            if (response != null)
            {
                if (statusBadgeText != null)
                    statusBadgeText.text = response.mission_status == "PASSING" ? "MISSION SUCCESS" : "MISSION FAILED";
                if (scoreText != null)
                    scoreText.text = $"{response.science_score} PTS";
                if (telemetryLogsText != null && response.mission_log != null)
                    telemetryLogsText.text = "• " + string.Join("\n• ", response.mission_log);
            }
        }

        private void OnSimulationError(string errorMessage)
        {
            Debug.LogWarning($"[GameManager] {errorMessage} - Continuing in Autonomous Offline Mode.");
            if (telemetryDrawer != null) telemetryDrawer.SetActive(false);
        }

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
