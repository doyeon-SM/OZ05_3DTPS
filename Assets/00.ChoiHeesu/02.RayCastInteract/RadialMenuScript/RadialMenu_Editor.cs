using UnityEngine;
using System.Collections;
using UnityEditor;

namespace ProjectSpedex
{
    [CustomEditor(typeof(RadialMenu))]
    public class NewBehaviourScript : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();


            RadialMenu rm = (RadialMenu)target;

            GUIContent visualize = new GUIContent("Visualize Arrangement", "Press this to preview what the radial menu will look like ingame.");
            GUIContent reset = new GUIContent("Reset Arrangement", "Press this to reset all elements to a 0 rotation for easy editing.");

            if (!Application.isPlaying) {
                if (GUILayout.Button(visualize)) {

                    arrangeElementsInEditor(rm, false);

                }

                if (GUILayout.Button(reset)) {

                    arrangeElementsInEditor(rm, true);

                }

            }

        }




        public void arrangeElementsInEditor(RadialMenu rm, bool reset) {

        if (reset) {


            for (int i = 0; i < rm.elements.Count; i++) {
                if (rm.elements[i] == null) {
                    Debug.LogError("Radial Menu: element " + i.ToString() + " in the radial menu " + rm.gameObject.name + " is null!");
                    continue;
                }
                RectTransform elemRt = rm.elements[i].GetComponent<RectTransform>();
                elemRt.localRotation = Quaternion.Euler(0, 0, 0);
                ResetButtonRotation(rm.elements[i]);

            }

            return;
        }

        if (!rm.rotateElementsByAngle)
            return;


        for (int i = 0; i < rm.elements.Count; i++) {
            if (rm.elements[i] == null) {
                Debug.LogError("Radial Menu: element " + i.ToString() + " in the radial menu " + rm.gameObject.name + " is null!");
                continue;
            }
            RectTransform elemRt = rm.elements[i].GetComponent<RectTransform>();
            elemRt.localRotation = Quaternion.Euler(0, 0, -((360f / (float)rm.elements.Count) * i) - rm.globalOffset);

        }


        }

        private void ResetButtonRotation(RadialMenuElement element) {

            if (element.button == null)
                return;

            RectTransform buttonRectTransform = element.button.GetComponent<RectTransform>();
            if (buttonRectTransform != null)
                buttonRectTransform.localRotation = Quaternion.identity;

        }

    }
}
