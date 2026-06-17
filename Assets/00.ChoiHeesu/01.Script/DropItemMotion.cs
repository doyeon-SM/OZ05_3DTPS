using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class DropItemMotion : MonoBehaviour
    {
        private const float MinimumMoveDuration = 0.01f;

        [Header("Target")]
        [SerializeField] private Transform meshTarget;
        [SerializeField] private bool useLocalPosition = true;

        [Header("Position")]
        [SerializeField] private float minYPosition;
        [SerializeField] private float maxYPosition = 0.25f;
        [SerializeField, Tooltip("Seconds from Min Y to Max Y.")]
        private float verticalMoveDuration = 1f;

        [Header("Rotate")]
        [SerializeField] private float yRotationDegreesPerSecond = 90f;

        private Vector3 baseLocalPosition;
        private Vector3 baseWorldPosition;
        private float motionStartTime;

        private Transform CurrentTarget => meshTarget != null ? meshTarget : transform;

        private void Awake()
        {
            CacheBasePosition();
        }

        private void OnEnable()
        {
            CacheBasePosition();
            motionStartTime = Time.time;
        }

        private void Reset()
        {
            meshTarget = transform.childCount > 0 ? transform.GetChild(0) : transform;
            CacheBasePosition();
        }

        private void OnValidate()
        {
            verticalMoveDuration = Mathf.Max(verticalMoveDuration, MinimumMoveDuration);
        }

        private void Update()
        {
            Transform target = CurrentTarget;
            if (target == null)
                return;

            UpdatePosition(target);
            UpdateRotation(target);
        }

        public void SetMeshTarget(Transform target)
        {
            meshTarget = target;
            CacheBasePosition();
        }

        private void CacheBasePosition()
        {
            Transform target = CurrentTarget;
            if (target == null)
                return;

            baseLocalPosition = target.localPosition;
            baseWorldPosition = target.position;
        }

        private void UpdatePosition(Transform target)
        {
            float lowerY = Mathf.Min(minYPosition, maxYPosition);
            float upperY = Mathf.Max(minYPosition, maxYPosition);
            float moveDuration = Mathf.Max(verticalMoveDuration, MinimumMoveDuration);
            float elapsedTime = Time.time - motionStartTime;
            float positionRate = Mathf.PingPong(elapsedTime / moveDuration, 1f);
            float nextY = Mathf.Lerp(lowerY, upperY, positionRate);

            if (useLocalPosition)
            {
                Vector3 nextPosition = baseLocalPosition;
                nextPosition.y = nextY;
                target.localPosition = nextPosition;
                return;
            }

            Vector3 nextWorldPosition = baseWorldPosition;
            nextWorldPosition.y = nextY;
            target.position = nextWorldPosition;
        }

        private void UpdateRotation(Transform target)
        {
            if (Mathf.Approximately(yRotationDegreesPerSecond, 0f))
                return;

            target.Rotate(0f, yRotationDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
