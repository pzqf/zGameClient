using UnityEngine;
using UnityEngine.AI;

namespace GameClient.Game
{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;
        public Camera followCamera;

        private NavMeshAgent _agent;
        private Vector3 _targetPosition;
        private bool _isMoving;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.speed = moveSpeed;
            }
        }

        private void Start()
        {
            if (followCamera == null)
            {
                followCamera = Camera.main ?? FindObjectOfType<Camera>();
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    MoveToPosition(hit.point);
                }
            }

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 moveDir = GetCameraBasedDirection(horizontal, vertical);
                Move(moveDir);
            }
        }

        private Vector3 GetCameraBasedDirection(float horizontal, float vertical)
        {
            if (followCamera == null)
            {
                return new Vector3(horizontal, 0, vertical).normalized;
            }

            Vector3 cameraForward = followCamera.transform.forward;
            Vector3 cameraRight = followCamera.transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDir = (cameraForward * vertical + cameraRight * horizontal).normalized;
            return moveDir;
        }

        private void Move(Vector3 direction)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }

            transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            _isMoving = true;
        }

        private void MoveToPosition(Vector3 position)
        {
            _targetPosition = position;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(position);
                _agent.isStopped = false;
            }
            else
            {
                Vector3 direction = (position - transform.position).normalized;
                direction.y = 0;
                Move(direction);
            }

            _isMoving = true;
        }

        public void StopMoving()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
            _isMoving = false;
        }

        public bool IsMoving()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                return !_agent.isStopped && _agent.velocity.magnitude > 0.1f;
            }
            return _isMoving;
        }

        public void SetPosition(Vector3 position)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }
    }
}
