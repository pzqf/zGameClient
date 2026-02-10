using UnityEngine;

namespace GameClient.Game
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float distance = 5f;
        public float height = 2f;
        public float smoothSpeed = 0.125f;
        public float rotationSpeed = 5f;
        public float minVerticalAngle = -20f;
        public float maxVerticalAngle = 60f;
        public float minDistance = 2f;
        public float maxDistance = 15f;
        public float scrollSpeed = 2f;

        private float _currentRotationX = 0f;
        private float _currentRotationY = 0f;
        private float _currentDistance;
        private bool _isRotating = false;

        private void Start()
        {
            FindPlayerTarget();
            _currentRotationY = transform.eulerAngles.y;
            _currentDistance = distance;
        }

        private void FindPlayerTarget()
        {
            if (target != null) return;

            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                target = playerController.transform;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _isRotating = true;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _isRotating = false;
            }

            if (_isRotating)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                _currentRotationY += mouseX * rotationSpeed;
                _currentRotationX -= mouseY * rotationSpeed;
                _currentRotationX = Mathf.Clamp(_currentRotationX, minVerticalAngle, maxVerticalAngle);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= scroll * scrollSpeed;
                _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayerTarget();
                return;
            }

            Quaternion rotation = Quaternion.Euler(_currentRotationX, _currentRotationY, 0);
            Vector3 desiredPosition = target.position - rotation * Vector3.forward * _currentDistance + Vector3.up * height;
            
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoothSpeed);
        }

        private void OnEnable()
        {
            Debug.Log($"CameraFollow enabled on {gameObject.name}, target: {target?.name ?? "null"}");
        }

        private void OnDisable()
        {
            Debug.Log($"CameraFollow disabled on {gameObject.name}");
        }
    }
}
