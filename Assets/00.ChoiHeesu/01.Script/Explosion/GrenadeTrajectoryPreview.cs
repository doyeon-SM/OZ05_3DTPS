using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace _00.ChoiHeesu._01.Script.Explosion
{
    [DisallowMultipleComponent]
    public class GrenadeTrajectoryPreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private Rigidbody grenadePrefabRigidbody;
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Layer Masks")]
        [SerializeField] private LayerMask aimLayerMask = ~0;
        [SerializeField] private LayerMask trajectoryCollisionMask = ~0;

        [Header("Aim")]
        [SerializeField] private float rayDistance = 100f;
        [SerializeField] private float fallbackDistance = 20f;

        [Header("Trajectory")]
        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float upwardModifier = 0.35f;
        [SerializeField] private int pointCount = 30;
        [SerializeField] private float timeStep = 0.06f;

        private Vector3[] points;
        private GrenadeThrowData currentThrowData;
        private bool hasCurrentThrowData;

        public bool HasCurrentThrowData => hasCurrentThrowData;

        private void Awake()
        {
            CacheReferences();
            ConfigureLineRenderer();
            EnsurePointBuffer();
            Hide();
        }

        private void Reset()
        {
            CacheReferences();
            ConfigureLineRenderer();
        }

        public void SetThrowSettings(float nextThrowForce, float nextUpwardModifier)
        {
            throwForce = Mathf.Max(nextThrowForce, 0f);
            upwardModifier = Mathf.Max(nextUpwardModifier, 0f);
        }

        public bool TryUpdatePreview(out GrenadeThrowData throwData)
        {
            throwData = default;

            if (!TryCalculateThrowData(out throwData))
            {
                Hide();
                return false;
            }

            currentThrowData = throwData;
            hasCurrentThrowData = true;
            DrawTrajectory(throwData);
            return true;
        }

        public bool TryCalculateThrowData(out GrenadeThrowData throwData)
        {
            CacheReferences();
            EnsurePointBuffer();

            return GrenadeThrowCalculator.TryCalculateThrowData(
                aimCamera,
                throwPoint,
                aimLayerMask,
                rayDistance,
                fallbackDistance,
                throwForce,
                upwardModifier,
                GetGrenadeMass(),
                GetPointerScreenPosition(),
                out throwData);
        }

        public bool TryGetCurrentThrowData(out GrenadeThrowData throwData)
        {
            throwData = currentThrowData;
            return hasCurrentThrowData;
        }

        public void Hide()
        {
            hasCurrentThrowData = false;

            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        private void DrawTrajectory(GrenadeThrowData throwData)
        {
            if (lineRenderer == null || throwPoint == null)
                return;

            ConfigureLineRenderer();

            Vector3 startPosition = throwPoint.position;
            points[0] = startPosition;

            int actualPointCount = 1;
            Vector3 previousPoint = startPosition;

            for (int i = 1; i < pointCount; i++)
            {
                float time = i * timeStep;
                Vector3 currentPoint = startPosition +
                                       throwData.InitialVelocity * time +
                                       0.5f * Physics.gravity * time * time;

                if (Physics.Linecast(previousPoint, currentPoint, out RaycastHit hit, trajectoryCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    points[actualPointCount] = hit.point;
                    actualPointCount++;
                    break;
                }

                points[actualPointCount] = currentPoint;
                actualPointCount++;
                previousPoint = currentPoint;
            }

            lineRenderer.positionCount = actualPointCount;
            for (int i = 0; i < actualPointCount; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
        }

        private void CacheReferences()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;

            if (lineRenderer == null)
                TryGetComponent(out lineRenderer);
        }

        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.useWorldSpace = true;
        }

        private void EnsurePointBuffer()
        {
            pointCount = Mathf.Max(pointCount, 2);

            if (points == null || points.Length != pointCount)
                points = new Vector3[pointCount];
        }

        private float GetGrenadeMass()
        {
            return grenadePrefabRigidbody != null ? Mathf.Max(grenadePrefabRigidbody.mass, 0.0001f) : 1f;
        }

        private Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
            return Input.mousePosition;
#endif
        }

        private void OnValidate()
        {
            rayDistance = Mathf.Max(rayDistance, 0.01f);
            fallbackDistance = Mathf.Max(fallbackDistance, 0.01f);
            throwForce = Mathf.Max(throwForce, 0f);
            upwardModifier = Mathf.Max(upwardModifier, 0f);
            pointCount = Mathf.Max(pointCount, 2);
            timeStep = Mathf.Max(timeStep, 0.001f);
        }
    }
}
