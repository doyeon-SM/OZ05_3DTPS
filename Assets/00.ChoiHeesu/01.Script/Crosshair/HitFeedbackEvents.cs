using System;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    public struct HitFeedbackEventData
    {
        public HitFeedbackEventData(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
        {
            HitCollider = hitCollider;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            HitLayer = hitCollider != null ? hitCollider.gameObject.layer : -1;
        }

        public Collider HitCollider { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public int HitLayer { get; }

        public bool IsInLayerMask(LayerMask layerMask)
        {
            return HitLayer >= 0 && (layerMask.value & (1 << HitLayer)) != 0;
        }
    }

    public static class HitFeedbackEvents
    {
        public static event Action<HitFeedbackEventData> Hit;

        public static void RaiseHit(RaycastHit hit)
        {
            if (hit.collider == null)
                return;

            Hit?.Invoke(new HitFeedbackEventData(hit.collider, hit.point, hit.normal));
        }
    }
}
