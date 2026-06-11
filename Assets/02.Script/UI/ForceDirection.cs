using UnityEngine;

namespace ProjectSpedex
{
	[AddComponentMenu("Radial Menu Framework/RMF Force Direction")]
    public class ForceDirection : MonoBehaviour {
    
        [Tooltip("현재 프로젝트에서는 Text Rotation을 코드로 보정하지 않습니다. 기존 프리팹 연결 유지를 위해 값만 남겨둡니다.")]
        public float forcedZRotation;
    }

}
