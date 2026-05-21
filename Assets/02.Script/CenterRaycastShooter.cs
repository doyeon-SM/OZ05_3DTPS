using UnityEngine;
using UnityEngine.InputSystem;

namespace _01.Scenes.PhaseValidation
{
    public class CenterRaycastShooter : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField]private Camera m_cam;
        [SerializeField]private LayerMask m_hittableMask;
        [SerializeField]private float m_InteractDistance = 15.0f;
        
        [SerializeField]private PlayerInput _input;
        [SerializeField]private InputAction _fire;

        [Header("Attack Data")]
        [SerializeField]private TPS_TwoStepHitscanWeapon tpsTwoStepHitscanWeapon;
        [SerializeField] private Transform attackPoint;
        [SerializeField]private Collider[] hitColliders;
        //폐기 될 변수들  
        [SerializeField] private int Damage = 20;
        [SerializeField] private float attackOverlapRadius = 1.0f;
        
        #region Unity Functions

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _fire = _input.actions.FindAction("Attack" , true);
            
            if(m_cam == null) m_cam = Camera.main;
            if(tpsTwoStepHitscanWeapon == null)
                tpsTwoStepHitscanWeapon = gameObject.GetComponent<TPS_TwoStepHitscanWeapon>();
        }

        private void Update()
        {
            InteractCount();
        }
        private void OnEnable()
        {
            /*_fire.performed += attackAlgorithm;*/
        }
        
        private void OnDisable()
        {
            /*_fire.performed -= attackAlgorithm;*/
        }
        #endregion

        private void InteractCount()
        {
            /*Vector2 _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Ray _ray = m_cam.ScreenPointToRay(_screenCenter);

            if (Physics.Raycast(_ray, out RaycastHit hit, m_InteractDistance, m_hittableMask,
                    QueryTriggerInteraction.Ignore))
            {
                string _objectName = hit.collider.gameObject.name;
                if (_objectName.Contains("Chest"))
                {
                    Debug.Log("[E] 상자 열기");
                }
                else if (_objectName.Contains("NPC"))
                {
                    Debug.Log("[E] 대화 하기.");
                }

                Debug.DrawRay(_ray.origin, _ray.direction * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawLine(m_cam.transform.position, m_cam.transform.forward * m_InteractDistance, Color.red);
            }*/
        }

        private void OnShapeFire(InputAction.CallbackContext _)
        {
            float radius = 0.5f; // 구체 반지름
            
            Vector3 origin = transform.position;
            Vector3 Direction = transform.forward;

            if (Physics.SphereCast(origin, radius, Direction, out RaycastHit hit, m_InteractDistance, m_hittableMask))
            {
                Debug.Log($"[CenterRaycastShooter] Hit : {hit.collider.name}");
                
            }
        }

        private void attackAlgorithm(InputAction.CallbackContext _)
        {
            tpsTwoStepHitscanWeapon.Fire();
        }

        
        private void OverlapInteractable(InputAction.CallbackContext _)
        {
            if(attackPoint == null) return;
           
            Vector3 center = attackPoint.position;
            
            hitColliders = Physics.OverlapSphere(center, attackOverlapRadius,LayerMask.GetMask("Enemy"));
            foreach (Collider _collider in hitColliders)
            {
                if (_collider.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(Damage);
                    Debug.Log($"[OverlapInteractable] {_collider.name}에게 {Damage}만큼 피해를 입혔습니다.");
                }
            }
            

        }
        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            if (attackPoint != null)
            {
                Gizmos.DrawWireSphere(attackPoint.position, attackOverlapRadius);
            }
        }
        

    }
}