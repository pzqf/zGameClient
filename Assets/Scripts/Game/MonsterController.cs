using UnityEngine;
using UnityEngine.AI;

namespace GameClient.Game
{
    public class MonsterController : MonoBehaviour
    {
        public long monsterId;
        public string monsterName;
        public float maxHealth = 100;
        public float currentHealth = 100;
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackCooldown = 1f;

        private NavMeshAgent _agent;
        private float _lastAttackTime;
        private GameObject _target;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (_target != null)
            {
                float distance = Vector3.Distance(transform.position, _target.transform.position);

                if (distance <= attackRange)
                {
                    _agent.isStopped = true;
                    TryAttack();
                }
                else
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(_target.transform.position);
                }
            }
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
        }

        public void ClearTarget()
        {
            _target = null;
            _agent.isStopped = true;
        }

        private void TryAttack()
        {
            if (Time.time - _lastAttackTime >= attackCooldown)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }

        private void Attack()
        {
            if (_target != null)
            {
                var player = _target.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log($"Monster {monsterName} attacks player for {attackDamage} damage");
                }
            }
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"Monster {monsterName} died");
            _agent.isStopped = true;
            enabled = false;
        }

        public void SetPosition(Vector3 position)
        {
            if (_agent != null && _agent.enabled)
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
