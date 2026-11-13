using UnityEngine;

/// <summary>
/// Multi-Mode Camera Controller for Space Simulation (Retained Modes Toggle).
/// Modes (Keys 1-2):
/// Mode 1 (Key 1): Free Mouse Orbit Cam - Hero side-profile default with 100% 360° mouse drag & expanded 350u max distance zoom.
/// Mode 2 (Key 2): Stationary Destination Overview - Fixed, non-rotating planetary camera framing active destination planet & incoming spacecraft.
/// </summary>
public class OrbitCameraController : MonoBehaviour
{
    public Transform target;            // Primary target (spacecraft)
    public Transform secondaryTarget;   // Secondary target (destination celestial body)

    [Header("Camera Mode Settings")]
    [Tooltip("1 = Free Mouse Orbit, 2 = Planet Overview")]
    public int cameraMode = 1;          // Default to Free Mouse Orbit Cam

    [Header("Free Mouse Orbit (Mode 1) Settings")]
    public float orbitDistance = 60f;   // Default distance
    public float minOrbitDistance = 30f;
    public float maxOrbitDistance = 1500f; // Expanded max distance prevents any clipping
    public float xSpeed = 120f;
    public float ySpeed = 120f;
    public float yMinLimit = -85f;
    public float yMaxLimit = 85f;

    [Header("Planet Overview (Mode 2) Settings")]
    public float planetCamDistanceMultiplier = 3.0f;

    [Header("Optical Lens (FOV) Zoom Settings")]
    public float defaultFOV = 60f;
    public float minFOV = 10f;
    public float maxFOV = 85f;
    public float fovZoomSensitivity = 15f;

    private float currentX = -60f;       // Hero side-profile angle
    private float currentY = 15f;
    private float targetFOV = 60f;
    private Camera cam;

    // Fixed stationary vector for Mode 2 so camera does NOT auto-rotate during destination orbit
    private Vector3 mode2FixedCamPos;
    private bool mode2PosInitialized = false;

    void Start()
    {


        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            cam.fieldOfView = defaultFOV;
            targetFOV = defaultFOV;
        }

        currentX = -60f;
        currentY = 15f;

        AutoFindTargets();
    }

    public void AutoFindTargets()
    {
        if (target == null)
        {
            GameObject vehicle = GameObject.Find("Vehicle");
            if (vehicle != null)
            {
                if (vehicle.transform.childCount > 0)
                {
                    foreach (Transform child in vehicle.transform)
                    {
                        if (child.gameObject.activeSelf)
                        {
                            target = child;
                            break;
                        }
                    }
                }
                if (target == null) target = vehicle.transform;
            }
        }
    }

    public void SetDualTarget(Transform primary, Transform secondary)
    {
        target = primary;
        secondaryTarget = secondary;
        mode2PosInitialized = false; // Trigger recalibration for Mode 2 stationary position
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCameraMode(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCameraMode(2);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            AutoFindTargets();
            if (target == null) return;
        }

        // Optical Scroll Wheel Zoom & Distance Adjust
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            if (cameraMode == 1)
            {
                orbitDistance = Mathf.Clamp(orbitDistance - scroll * fovZoomSensitivity * 20f, minOrbitDistance, maxOrbitDistance);
            }
            else
            {
                targetFOV = Mathf.Clamp(targetFOV - scroll * fovZoomSensitivity * 10f, minFOV, maxFOV);
            }
        }

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 5f);
        }

        switch (cameraMode)
        {
            case 1: // Mode 1: Free Mouse Orbit Cam
                ExecuteMouseOrbitCam();
                break;
            case 2: // Mode 2: Stationary Destination Planet Overview
                ExecutePlanetOverviewCam();
                break;
            default:
                ExecuteMouseOrbitCam();
                break;
        }
    }

    public void SetCameraMode(int mode)
    {
        cameraMode = mode;
        if (mode == 1)
        {
            currentX = -60f;
            currentY = 15f;
        }
        else if (mode == 2)
        {
            mode2PosInitialized = false; // Reset stationary position calculation for Planet Overview
        }
        Debug.Log("<color=cyan>🎥 Camera Mode Switched To Mode " + mode + "</color>");
    }

    private void ExecuteMouseOrbitCam()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * xSpeed * 0.04f;
            currentY -= Input.GetAxis("Mouse Y") * ySpeed * 0.04f;
            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
        }

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -orbitDistance);

        Vector3 desiredPos = rotation * negDistance + target.position;
        transform.position = desiredPos;
        transform.LookAt(target.position);
    }

    private void ExecutePlanetOverviewCam()
    {
        Transform planet = GetActiveDestinationPlanet();
        if (planet == null) return;

        float planetRadius = GetPlanetRadius(planet);
        float viewDistance = Mathf.Max(planetRadius * planetCamDistanceMultiplier, 120f);

        // Calculate a fixed, stationary world position ONCE when entering Mode 2 so camera does NOT auto-rotate
        if (!mode2PosInitialized)
        {
            Vector3 initDir = (spacecraftPosition() - planet.position).normalized;
            if (initDir == Vector3.zero) initDir = Vector3.back;
            mode2FixedCamPos = planet.position - (initDir * viewDistance) + (Vector3.up * (viewDistance * 0.3f));
            mode2PosInitialized = true;
        }

        transform.position = Vector3.Lerp(transform.position, mode2FixedCamPos, Time.deltaTime * 3f);
        transform.LookAt(planet.position);
    }

    private Transform GetActiveDestinationPlanet()
    {
        // Priority 1: Check InterplanetaryJourney active flight destination FIRST!
        InterplanetaryJourney journey = FindFirstObjectByType<InterplanetaryJourney>();
        if (journey != null && journey.activeDestination != null)
        {
            secondaryTarget = journey.activeDestination;
            return secondaryTarget;
        }

        // Priority 2: Fall back to Inspector-assigned secondaryTarget
        if (secondaryTarget != null && secondaryTarget.gameObject.activeInHierarchy)
        {
            return secondaryTarget;
        }

        GameObject planets = GameObject.Find("Planets");
        if (planets != null)
        {
            foreach (Transform child in planets.transform)
            {
                if (child.gameObject.activeInHierarchy && !child.name.ToLower().Contains("earth"))
                {
                    secondaryTarget = child;
                    return child;
                }
            }
        }
        return secondaryTarget;
    }

    private Vector3 spacecraftPosition()
    {
        return (target != null) ? target.position : Vector3.zero;
    }

    private float GetPlanetRadius(Transform planet)
    {
        if (planet == null) return 20f;
        Renderer[] rnds = planet.GetComponentsInChildren<Renderer>();
        if (rnds != null && rnds.Length > 0)
        {
            Bounds b = rnds[0].bounds;
            for (int i = 1; i < rnds.Length; i++) b.Encapsulate(rnds[i].bounds);
            return Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
        }
        return 20f;
    }
}