using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponController : MonoBehaviour
    {
        [Header("총기 관련 스크립트")]
        //todo Test용 SO
        [SerializeField]private WeaponData Testdata;
        [SerializeField]private WeaponRuntime weaponRuntime;                        // 런 타임중 무기 관리 스크립트
        [SerializeField] private GameObject gunSocket;              // 총이 추가(혹은 풀링) 될 때 추가하게될 Parents Object.
        [Header("현재 총기의 데이터 저장용 변수")]
        [SerializeField] private bool isfire;
        [SerializeField] private float shotDelayTime;  //무기 변경시 RPM을 받아와서 해당 딜레이 타임 1번 지정
        [SerializeField] private float saveTime = -999f;   //격발 시 딜레이 시간 저장
        // 호출시 S.O상에 데이터를 넘겨주는 변수.
        public float RPM => weaponRuntime != null ? weaponRuntime.data.RPM : 0;
        public bool AutoFire =>  weaponRuntime != null && weaponRuntime.data.AutoFire;
        public int Damage => weaponRuntime != null ? weaponRuntime.data.Damage : 0;
        //상태 변화 감지를 위한 bool값
        public bool VariableChange;
        #region Unity Life Cycle

        private void Awake()
        {
            //todo : Test용
            weaponRuntime = new WeaponRuntime(Testdata);
            
            AwakeSetting();
        }

        #endregion

        #region Script Methods

        private void AwakeSetting()
        {
            RPMCalculate(); // RPM으로 딜레이 시간 계산.
        }

        public bool isShootable()
        {
            if (nullCheck())
            {
                Debug.Log("WeaponDamage.nullCheck");
                return false;
            }
            if (!weaponRuntime.HasAmmo()) //남은 총알이 있는지 확인
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void FireAction()
        {
            weaponRuntime.ConsumeAmmo();        //총알 소모
            Debug.Log($"현재 남은 총알 {weaponRuntime.currentAmmo} / {weaponRuntime.data.MagazineSize}");
        }
        private void Reload()
        {
            Debug.Log("재장전 합니다..");
            //재장전 애니메이션 출력
            //사격 통제
            //runtimeData에 Reload
            weaponRuntime.Reload();
        }
        private void RPMCalculate() //RPM을 받아서 초당 딜레이 타임으로 변환 계산합니다 ( 60sec / RPM ) 
        {
            if(weaponRuntime.data.RPM <= 0) return;
            
            shotDelayTime = 60.0f / weaponRuntime.data.RPM;
        }

        public bool IsFire()
        {
            if ( Time.time <= saveTime + shotDelayTime)
            {
                //RPM에 따른 딜레이 시간을 조절합니다.
                return false;
            }
            //격발 된 시간 저장.
            saveTime = Time.time;
            return true;
        }

        private bool nullCheck()
        {
            //방어코드용 메서드
            if (weaponRuntime == null)
            {
                Debug.LogError("weaponRuntime == null");
                return true;
            }

            if (weaponRuntime.data == null)
            {
                Debug.LogError("weaponRuntime.data == null");
                return true;
            }
            
            return false;
        }
        #endregion
        
    }
}