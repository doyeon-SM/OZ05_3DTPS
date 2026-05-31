using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RMF_Demo : MonoBehaviour {

    public ParticleSystem ps;
    public RMF_RadialMenu rm;
	//초기화에 사용됩니다.
	void Start () {
	
	}

    //매 프레임 한 번씩 호출됩니다.
    void Update() {


        if (WasSKeyPressed() && rm != null && rm.useLazySelection) {

            rm.useSelectionFollower = !rm.useSelectionFollower;
            if (rm.selectionFollowerContainer != null)
                rm.selectionFollowerContainer.gameObject.SetActive(rm.useSelectionFollower);


        }


        if (WasAlpha1Pressed()) {
            SceneManager.LoadScene(0);
        }

        if (WasAlpha2Pressed()) {
            SceneManager.LoadScene(1);
        }


    }

    public void emitButton(int count) {

        if (ps == null)
            return;

        ps.Emit(count);



    }

    private bool WasSKeyPressed() {

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.S);
#else
        return false;
#endif

    }

    private bool WasAlpha1Pressed() {

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha1);
#else
        return false;
#endif

    }

    private bool WasAlpha2Pressed() {

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha2);
#else
        return false;
#endif

    }


}
