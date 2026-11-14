using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpaceSim.UI
{
    /// <summary>
    /// Blender-style 3D viewport camera controller.
    /// Provides orbit, pan, scroll-zoom, and smooth stage-framing transitions.
    /// Works seamlessly with Unity's new Input System and legacy Input manager.
    /// </summary>
    public class BlenderOrbitCamera : MonoBehaviour
    {
        [Header("Target & Distances")]
        public Transform pivotTarget;
        public Vector3 pivotPoint = new Vector3(0, 45f, 0);
        public float distance = 70f;
        public float minDistance = 5f;
        public float maxDistance = 220f;

        [Header("Orbit Sensitivity")]
        public float xSpeed = 160f;
        public float ySpeed = 100f;
        public float yMinLimit = -30f;
        public float yMaxLimit = 85f;

        [Header("Zoom & Pan Sensitivity")]
        public float zoomSensitivity = 15f;
        public float panSpeed = 0.4f;
        public float damping = 8f;

        private float _currentX = 35f;
        private float _currentY = 20f;
        private float _targetX = 35f;
        private float _targetY = 20f;
        private float _targetDistance = 70f;
        private Vector3 _targetPivot;

        private bool _isTransitioning = false;
        private Coroutine _transitionCoroutine;

        private void Start()
        {
            _targetPivot = pivotPoint;
            _targetDistance = distance;

            Vector3 angles = transform.eulerAngles;
            _targetX = _currentX = angles.y;
            _targetY = _currentY = angles.x;

            UpdateCameraTransform(1f);
        }

        private void LateUpdate()
        {
            if (_isTransitioning) return;

            HandleInput();
            UpdateCameraTransform(Time.deltaTime * damping);
        }

        private void HandleInput()
        {
            // Do not orbit/pan if pointer is interacting with UI elements
            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue();
            float scroll = mouse.scroll.ReadValue().y;

            // Zoom (Scroll wheel)
            if (Mathf.Abs(scroll) > 0.01f && !pointerOverUI)
            {
                _targetDistance -= (scroll / 120f) * zoomSensitivity;
                _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            }

            // Orbit (Left Click Drag or Right Click Drag when not panning)
            bool leftPressed = mouse.leftButton.isPressed;
            bool middlePressed = mouse.middleButton.isPressed;
            bool rightPressed = mouse.rightButton.isPressed;

            if (leftPressed && !pointerOverUI)
            {
                _targetX += mouseDelta.x * xSpeed * 0.02f;
                _targetY -= mouseDelta.y * ySpeed * 0.02f;
                _targetY = ClampAngle(_targetY, yMinLimit, yMaxLimit);
            }
            // Pan (Middle Click Drag or Shift + Left)
            else if (middlePressed || (rightPressed && !pointerOverUI))
            {
                Vector3 right = transform.right;
                Vector3 up = transform.up;
                _targetPivot -= (right * mouseDelta.x + up * mouseDelta.y) * panSpeed * 0.05f * (_targetDistance / 50f);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !pointerOverUI)
            {
                _targetDistance -= scroll * zoomSensitivity * 10f;
                _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            }

            if (Input.GetMouseButton(0) && !pointerOverUI)
            {
                _targetX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                _targetY -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                _targetY = ClampAngle(_targetY, yMinLimit, yMaxLimit);
            }
            else if (Input.GetMouseButton(2) || (Input.GetMouseButton(1) && !pointerOverUI))
            {
                Vector3 right = transform.right;
                Vector3 up = transform.up;
                _targetPivot -= (right * Input.GetAxis("Mouse X") + up * Input.GetAxis("Mouse Y")) * panSpeed * (_targetDistance / 50f);
            }
#endif
        }

        private void UpdateCameraTransform(float t)
        {
            _currentX = Mathf.Lerp(_currentX, _targetX, t);
            _currentY = Mathf.Lerp(_currentY, _targetY, t);
            distance = Mathf.Lerp(distance, _targetDistance, t);
            pivotPoint = Vector3.Lerp(pivotPoint, _targetPivot, t);

            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + pivotPoint;

            transform.rotation = rotation;
            transform.position = position;
        }

        /// <summary>
        /// Smoothly frames the camera on a specific rocket stage / module.
        /// </summary>
        public void FocusOn(Vector3 targetCenter, float targetDist, float duration = 0.85f)
        {
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(SmoothFocusRoutine(targetCenter, targetDist, duration));
        }

        private IEnumerator SmoothFocusRoutine(Vector3 newPivot, float newDist, float duration)
        {
            _isTransitioning = true;
            Vector3 startPivot = pivotPoint;
            float startDist = distance;
            float elapsed = 0f;

            _targetPivot = newPivot;
            _targetDistance = Mathf.Clamp(newDist, minDistance, maxDistance);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                pivotPoint = Vector3.Lerp(startPivot, newPivot, progress);
                distance = Mathf.Lerp(startDist, _targetDistance, progress);

                Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
                transform.rotation = rotation;
                transform.position = rotation * new Vector3(0.0f, 0.0f, -distance) + pivotPoint;

                yield return null;
            }

            pivotPoint = newPivot;
            distance = _targetDistance;
            _isTransitioning = false;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
