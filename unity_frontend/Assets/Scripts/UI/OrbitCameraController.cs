using UnityEngine;

namespace TensorHawks.UI
{
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Target & Positioning")]
        public Transform rocketTarget;
        public Vector3 offset = new Vector3(-15f, 10f, -15f);
        public float smoothSpeed = 5f;

        private void LateUpdate()
        {
            if (rocketTarget == null) return;

            Vector3 desiredPosition = rocketTarget.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            transform.LookAt(rocketTarget);
        }
    }
}
