using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the 3D space flight sequence from LEO to destination planets.
/// Handles dynamic rocket mesh selection (Saturn V, SLS, Falcon 9),
/// 3-phase Cubic Bezier orbital transit, and velocity-matched orbital entry.
/// </summary>
public class InterplanetaryJourney : MonoBehaviour
{
    [Header("Core References")]
    public Transform earth;
    public Transform spacecraft;
    public OrbitCameraController cameraController;
    public ParticleSystem thrusterParticles;

    [Header("Destination Celestial Bodies")]
    public GameObject[] destinationObjects; // Mars, Jupiter, Saturn, Moon

    [Header("Rocket 3D Models")]
    public GameObject[] rocketObjects; // Saturn V, space_launch_system_sls, spacex_falcon_9
    public string selectedRocketName = "Saturn V";

    [Header("Standalone Test Mode Settings")]
    public bool enableTestLaunchOnStart = false;
    public string testDestination = "Mars";

    [Header("Camera & View Distance Settings")]
    public float cameraFarClipPlane = 50000f;
    public float cameraNearClipPlane = 0.1f;

    [Header("Transit Arc Parameters (Motion 1 Speed Controls)")]
    public float leoAltitude = 25f;
    public float targetOrbitBuffer = 30f;
    public float solarSystemArcHeight = 150f;
    public float minTransitDuration = 5f;    // Fast transit lower bound
    public float maxTransitDuration = 15f;   // Fast transit upper bound
    public float transitSpeedFactor = 60f;   // Speed multiplier (Higher = Faster)

    [Header("Orbital Motion Parameters")]
    public float orbitSpeedDegrees = 25f;
    public float orbitTiltAngle = 15f;

    [Header("Thruster Intensity Settings")]
    [Tooltip("Particle emission & velocity multiplier during transit burn (1.0 = 100%)")]
    public float transitThrusterIntensity = 1.0f;

    [Tooltip("Particle emission & velocity multiplier during continuous orbit cruise burn (0.3 = 30%)")]
    public float orbitThrusterIntensity = 0.3f;

    public Transform activeDestination;
    private bool isOrbiting = false;
    private float currentOrbitAngle = 0f;
    private float finalOrbitRadius = 40f;
    private Quaternion orbitPlaneRotation;

    private float baseEmissionRate = 50f;
    private float baseStartSpeed = 10f;
    private bool baseSettingsCaptured = false;

    void Start()
    {
        ConfigureCameraViewDistance();
        CaptureBaseThrusterSettings();

        if (enableTestLaunchOnStart)
        {
            Debug.Log("<color=yellow>⚠️ Standalone Test Mode: Auto-starting flight to " + testDestination + "!</color>");
            StartFlightSequence(testDestination, selectedRocketName, true, 3800f, 4200f, 500f, 100f);
        }
    }

    void Update()
    {
        if (isOrbiting && activeDestination != null && spacecraft != null)
        {
            ExecuteContinuousOrbit();
        }
    }

    private void CaptureBaseThrusterSettings()
    {
        if (thrusterParticles == null)
        {
            GameObject flameObj = FindObjectInSceneByName("ThrusterFlame") ?? FindObjectInSceneByName("Thruster");
            if (flameObj != null) thrusterParticles = flameObj.GetComponent<ParticleSystem>();
        }

        if (thrusterParticles != null && !baseSettingsCaptured)
        {
            var emission = thrusterParticles.emission;
            var main = thrusterParticles.main;
            baseEmissionRate = emission.rateOverTime.constant;
            baseStartSpeed = main.startSpeed.constant;
            if (baseEmissionRate <= 0f) baseEmissionRate = 50f;
            if (baseStartSpeed <= 0f) baseStartSpeed = 10f;
            baseSettingsCaptured = true;
        }
    }

    private List<Renderer> GetValidRocketRenderers(Transform targetTransform)
    {
        List<Renderer> validList = new List<Renderer>();
        if (targetTransform == null) return validList;

        Renderer[] allRnds = targetTransform.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRnds)
        {
            if (r == null || !r.enabled) continue;
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

            string n = r.gameObject.name.ToLower();
            if (n.Contains("thruster") || n.Contains("flame") || n.Contains("particle")) continue;

            validList.Add(r);
        }
        return validList;
    }

    private Vector3 GetSpacecraftEngineNozzleWorldPos()
    {
        if (spacecraft == null) return Vector3.zero;

        List<Renderer> rnds = GetValidRocketRenderers(spacecraft);
        if (rnds.Count > 0)
        {
            Bounds b = rnds[0].bounds;
            for (int i = 1; i < rnds.Count; i++)
            {
                b.Encapsulate(rnds[i].bounds);
            }
            return new Vector3(b.center.x, b.min.y, b.center.z);
        }
        return spacecraft.position;
    }

    private Vector3 GetSpacecraftCenter()
    {
        if (spacecraft == null) return Vector3.zero;
        List<Renderer> rnds = GetValidRocketRenderers(spacecraft);
        if (rnds.Count > 0)
        {
            Bounds b = rnds[0].bounds;
            for (int i = 1; i < rnds.Count; i++)
            {
                b.Encapsulate(rnds[i].bounds);
            }
            return b.center;
        }
        return spacecraft.position;
    }

    private float GetSpacecraftLength()
    {
        if (spacecraft == null) return 5f;
        List<Renderer> rnds = GetValidRocketRenderers(spacecraft);
        if (rnds.Count > 0)
        {
            Bounds b = rnds[0].bounds;
            for (int i = 1; i < rnds.Count; i++)
            {
                b.Encapsulate(rnds[i].bounds);
            }
            return b.size.y;
        }
        return 5f;
    }

    private float GetCombinedBodyRadius(Transform bodyTransform)
    {
        if (bodyTransform == null) return 10f;
        List<Renderer> rnds = GetValidRocketRenderers(bodyTransform);
        if (rnds.Count > 0)
        {
            Bounds b = rnds[0].bounds;
            for (int i = 1; i < rnds.Count; i++)
            {
                b.Encapsulate(rnds[i].bounds);
            }
            return Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
        }
        return 10f;
    }

    private void AttachThrusterToSpacecraft()
    {
        if (thrusterParticles == null)
        {
            GameObject flameObj = FindObjectInSceneByName("ThrusterFlame") ?? FindObjectInSceneByName("Thruster");
            if (flameObj != null) thrusterParticles = flameObj.GetComponent<ParticleSystem>();
        }

        if (thrusterParticles != null && spacecraft != null)
        {
            thrusterParticles.transform.SetParent(spacecraft, true);
            thrusterParticles.transform.localScale = Vector3.one;

            Vector3 nozzleWorldPos = GetSpacecraftEngineNozzleWorldPos();
            thrusterParticles.transform.position = nozzleWorldPos;

            thrusterParticles.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            thrusterParticles.gameObject.SetActive(true);
            if (!thrusterParticles.isPlaying) thrusterParticles.Play();
        }
    }

    private void SetThrusterIntensity(float multiplier)
    {
        AttachThrusterToSpacecraft();
        if (thrusterParticles == null) return;
        CaptureBaseThrusterSettings();

        if (!thrusterParticles.isPlaying) thrusterParticles.Play();

        var emission = thrusterParticles.emission;
        emission.rateOverTime = baseEmissionRate * multiplier;

        var main = thrusterParticles.main;
        main.startSpeed = baseStartSpeed * multiplier;
    }

    private void ConfigureCameraViewDistance()
    {
        if (Camera.main != null)
        {
            Camera.main.nearClipPlane = cameraNearClipPlane;
            Camera.main.farClipPlane = cameraFarClipPlane;
        }
    }

    // --- FLIGHT SEQUENCE PUBLIC ENTRY POINTS ---

    // 6-Argument Overload (Uses default selectedRocketName)
    public void StartFlightSequence(string targetDestination, bool isSuccess, float reqDeltaV, float achievedDeltaV, float payloadKg, float fuelLeftKg)
    {
        StartFlightSequence(targetDestination, selectedRocketName, isSuccess, reqDeltaV, achievedDeltaV, payloadKg, fuelLeftKg);
    }

    // 7-Argument Overload (Explicit Rocket Selection)
    public void StartFlightSequence(string targetDestination, string rocketName, bool isSuccess, float reqDeltaV, float achievedDeltaV, float payloadKg, float fuelLeftKg)
    {
        enableTestLaunchOnStart = false;

        if (!string.IsNullOrEmpty(rocketName))
        {
            selectedRocketName = rocketName;
        }

        Debug.Log("<color=cyan>🚀 FLIGHT SEQUENCE STARTED TO: " + targetDestination + " WITH ROCKET: " + selectedRocketName + "</color>");

        StopAllCoroutines();

        isOrbiting = false;
        ConfigureCameraViewDistance();
        ActivateDestinationObject(targetDestination);
        ActivateRocketObject(selectedRocketName);

        if (activeDestination != null && spacecraft != null)
        {
            StartCoroutine(Execute3PhaseJourney(targetDestination, isSuccess, reqDeltaV, achievedDeltaV, payloadKg, fuelLeftKg));
        }
        else
        {
            Debug.LogError("❌ [InterplanetaryJourney] Destination object or Spacecraft reference missing!");
        }
    }

    private void ActivateRocketObject(string rocketName)
    {
        string target = string.IsNullOrEmpty(rocketName) ? "saturn" : rocketName.ToLower().Trim();

        // Dynamically find rocket GameObjects in scene
        GameObject saturnObj = FindObjectInSceneByName("Saturn V") ?? FindObjectInSceneByName("Saturn_V");
        GameObject slsObj = FindObjectInSceneByName("space_launch_system_sls") ?? FindObjectInSceneByName("SLS Block 1") ?? FindObjectInSceneByName("SLS");
        GameObject falconObj = FindObjectInSceneByName("spacex_falcon_9") ?? FindObjectInSceneByName("spacex_falcon_9_v4") ?? FindObjectInSceneByName("Falcon 9");

        // Disable all first
        if (saturnObj != null) saturnObj.SetActive(false);
        if (slsObj != null) slsObj.SetActive(false);
        if (falconObj != null) falconObj.SetActive(false);

        GameObject chosenRocket = null;

        if (target.Contains("falcon") || target.Contains("rocket_3"))
        {
            chosenRocket = falconObj;
        }
        else if (target.Contains("sls") || target.Contains("rocket_2"))
        {
            chosenRocket = slsObj;
        }
        else if (target.Contains("saturn") || target.Contains("rocket_1"))
        {
            chosenRocket = saturnObj;
        }

        // Failsafe fallback
        if (chosenRocket == null)
        {
            chosenRocket = saturnObj ?? slsObj ?? falconObj;
        }

        if (chosenRocket != null)
        {
            chosenRocket.SetActive(true);
            spacecraft = chosenRocket.transform;

            if (chosenRocket.transform.parent != null)
            {
                chosenRocket.transform.parent.gameObject.SetActive(true);
            }

            Debug.Log("<color=green>✅ ACTIVATED 3D ROCKET MODEL: " + chosenRocket.name + "</color>");
        }
        else if (spacecraft != null)
        {
            spacecraft.gameObject.SetActive(true);
        }

        AttachThrusterToSpacecraft();
    }

    private GameObject FindObjectInSceneByName(string nameToFind)
    {
        if (string.IsNullOrEmpty(nameToFind)) return null;

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in allTransforms)
        {
            if (t == null) continue;
            string cleanName = t.name.ToLower().Replace(" ", "_");
            string cleanTarget = nameToFind.ToLower().Replace(" ", "_");

            if (cleanName.Equals(cleanTarget) || cleanName.Contains(cleanTarget) || cleanTarget.Contains(cleanName))
            {
                return t.gameObject;
            }
        }
        return null;
    }

    private void SetRocketOrientation(Vector3 flightDir)
    {
        if (spacecraft == null || flightDir == Vector3.zero) return;

        Vector3 radialUp = activeDestination != null
            ? (spacecraft.position - activeDestination.position).normalized
            : Vector3.up;

        if (radialUp == Vector3.zero || radialUp == flightDir) radialUp = Vector3.up;

        Vector3 right = Vector3.Cross(radialUp, flightDir).normalized;
        if (right == Vector3.zero) right = Vector3.right;

        Vector3 forward = Vector3.Cross(flightDir, right).normalized;

        spacecraft.rotation = Quaternion.LookRotation(forward, flightDir);
    }

    private void SetupImg52CameraPOV(Vector3 leoLaunchPos, Vector3 trajectoryDir)
    {
        if (spacecraft == null) return;

        spacecraft.gameObject.SetActive(true);
        spacecraft.position = leoLaunchPos;

        if (trajectoryDir != Vector3.zero)
        {
            SetRocketOrientation(trajectoryDir);
        }

        Vector3 focusPoint = GetSpacecraftCenter();

        if (Camera.main != null)
        {
            ConfigureCameraViewDistance();
            Vector3 camOffset = -trajectoryDir * 25f + Vector3.up * 8f - Vector3.right * 10f;
            Camera.main.transform.position = focusPoint + camOffset;
            Camera.main.transform.LookAt(focusPoint + trajectoryDir * 10f);
        }

        if (cameraController != null && activeDestination != null)
        {
            cameraController.SetDualTarget(spacecraft, activeDestination);
        }
    }

    private IEnumerator Execute3PhaseJourney(string targetName, bool isSuccess, float reqDeltaV, float achievedDeltaV, float payloadKg, float fuelLeftKg)
    {
        if (earth == null || activeDestination == null || spacecraft == null)
        {
            Debug.LogError("❌ [InterplanetaryJourney] Missing critical inspector references!");
            yield break;
        }

        spacecraft.gameObject.SetActive(true);
        AttachThrusterToSpacecraft();

        float earthRadius = GetCombinedBodyRadius(earth);
        float targetRadius = GetCombinedBodyRadius(activeDestination);
        float fullRocketLength = GetSpacecraftLength();

        Vector3 trajectoryDir = (activeDestination.position - earth.position).normalized;
        if (trajectoryDir == Vector3.zero) trajectoryDir = Vector3.forward;

        float totalDepartureClearance = earthRadius + fullRocketLength + leoAltitude;
        float totalArrivalClearance = targetRadius + fullRocketLength + targetOrbitBuffer;

        Vector3 startPos = earth.position + trajectoryDir * totalDepartureClearance;

        finalOrbitRadius = totalArrivalClearance;
        orbitPlaneRotation = Quaternion.Euler(orbitTiltAngle, 0f, 0f);

        Vector3 approachVector = (activeDestination.position - startPos).normalized;
        Vector3 localApproach = Quaternion.Inverse(orbitPlaneRotation) * (-approachVector);
        currentOrbitAngle = Mathf.Atan2(localApproach.z, localApproach.x) * Mathf.Rad2Deg;

        float radEntry = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 initialOrbitOffset = orbitPlaneRotation * new Vector3(Mathf.Cos(radEntry), 0f, Mathf.Sin(radEntry)) * finalOrbitRadius;

        Vector3 p3_endPos = activeDestination.position + initialOrbitOffset;
        Vector3 p0_startPos = startPos;

        float rawDistance = Vector3.Distance(p0_startPos, p3_endPos);
        float transitDuration = Mathf.Clamp(rawDistance / transitSpeedFactor, minTransitDuration, maxTransitDuration);

        Vector3 targetTangent = orbitPlaneRotation * new Vector3(-Mathf.Sin(radEntry), 0f, Mathf.Cos(radEntry)).normalized;
        float orbitSpeedRad = orbitSpeedDegrees * Mathf.Deg2Rad;
        float vOrbit = orbitSpeedRad * finalOrbitRadius;

        float effectiveArcHeight = Mathf.Min(solarSystemArcHeight, rawDistance * 0.25f);
        if (vOrbit > 0.001f)
        {
            float maxSafeDuration = Mathf.Max((3f * rawDistance * 0.4f) / vOrbit, 0.5f);
            transitDuration = Mathf.Min(transitDuration, maxSafeDuration);
        }

        Vector3 midPoint = (p0_startPos + p3_endPos) * 0.5f;
        Vector3 perpendicularDir = Vector3.Cross(trajectoryDir, Vector3.up).normalized;
        if (perpendicularDir == Vector3.zero) perpendicularDir = Vector3.right;
        Vector3 p1_highArc = midPoint + (Vector3.up * effectiveArcHeight) + (perpendicularDir * (effectiveArcHeight * 0.4f));

        float approachLength = (vOrbit * transitDuration) / 3.0f;
        Vector3 p2_tangentApproach = p3_endPos - (targetTangent * approachLength);

        // --- PHASE 1: LEO LAUNCH SETUP ---
        SetupImg52CameraPOV(startPos, trajectoryDir);
        SetThrusterIntensity(transitThrusterIntensity);

        // --- PHASE 2: CUBIC TANGENT BEZIER TRANSIT ---
        float elapsedTime = 0f;
        Vector3 previousPos = p0_startPos;

        while (elapsedTime < transitDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitDuration;

            Vector3 currentPos = Mathf.Pow(1f - t, 3) * p0_startPos +
                                 3f * Mathf.Pow(1f - t, 2) * t * p1_highArc +
                                 3f * (1f - t) * Mathf.Pow(t, 2) * p2_tangentApproach +
                                 Mathf.Pow(t, 3) * p3_endPos;

            spacecraft.position = currentPos;

            Vector3 flightVelocity = (currentPos - previousPos).normalized;
            if (flightVelocity != Vector3.zero)
            {
                SetRocketOrientation(flightVelocity);
            }

            if (t >= 0.70f)
            {
                float blend = (t - 0.70f) / 0.30f;
                float currentIntensity = Mathf.Lerp(transitThrusterIntensity, orbitThrusterIntensity, blend);
                SetThrusterIntensity(currentIntensity);
            }
            else
            {
                SetThrusterIntensity(transitThrusterIntensity);
            }

            previousPos = currentPos;
            yield return null;
        }

        // --- PHASE 3: SEAMLESS CONTINUOUS ORBIT ---
        spacecraft.position = p3_endPos;
        SetThrusterIntensity(orbitThrusterIntensity);
        isOrbiting = true;

        Debug.Log("<color=green>✅ PERFECT VELOCITY-MATCHED ORBITAL ENTRY AT " + targetName + "!</color>");
    }

    private void ExecuteContinuousOrbit()
    {
        currentOrbitAngle += orbitSpeedDegrees * Time.deltaTime;
        if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;

        float rad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 localOffset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * finalOrbitRadius;

        Vector3 targetOrbitPos = activeDestination.position + (orbitPlaneRotation * localOffset);
        spacecraft.position = targetOrbitPos;

        Vector3 tangentDir = orbitPlaneRotation * new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
        if (tangentDir != Vector3.zero)
        {
            SetRocketOrientation(tangentDir);
        }

        if (cameraController != null)
        {
            cameraController.target = spacecraft;
            if (cameraController.secondaryTarget != activeDestination)
            {
                cameraController.SetDualTarget(spacecraft, activeDestination);
            }
        }
    }

    private void ActivateDestinationObject(string targetName)
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (destinationObjects != null && destinationObjects.Length > 0)
        {
            activeDestination = null;

            foreach (GameObject obj in destinationObjects)
            {
                if (obj == null) continue;
                obj.SetActive(true);

                string nameLower = obj.name.ToLower();
                string targetLower = targetName.ToLower();

                if (nameLower.Contains(targetLower) || targetLower.Contains(nameLower))
                {
                    activeDestination = obj.transform;
                }
            }
        }

        if (activeDestination == null)
        {
            foreach (Transform t in allTransforms)
            {
                if (t == null) continue;
                string cleanName = t.name.ToLower();
                string cleanTarget = targetName.ToLower().Replace(" orbit", "").Trim();

                if (cleanName.Contains(cleanTarget) && !cleanName.Contains("button") && !cleanName.Contains("card"))
                {
                    activeDestination = t;
                    t.gameObject.SetActive(true);
                    break;
                }
            }
        }

        if (activeDestination == null && earth != null)
        {
            activeDestination = earth;
        }

        if (activeDestination != null)
        {
            activeDestination.gameObject.SetActive(true);
        }

        if (cameraController != null && activeDestination != null)
        {
            cameraController.SetDualTarget(spacecraft, activeDestination);
        }
    }
}
