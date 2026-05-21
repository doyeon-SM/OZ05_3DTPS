using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public struct AimResult
    {
        public Ray ray;
        public bool didHit;
        public Vector3 point;
        public RaycastHit hit;
    }
    
    public struct ShotResult
    {
    public Vector3 origin;
    public Vector3 direction;
    public float distance;
    public bool didHit;
    public RaycastHit hit;
    }

}