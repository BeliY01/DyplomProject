using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AutomaticTutorialMaker
{
    public class JoystickTipController : MonoBehaviour
    {
        #region Variables
        private TutorialSceneReferences sceneReferences; // Scene component references

        [Header("Main triggers section (LT/RT)")]
        [SerializeField] private Transform triggers;
        [SerializeField] private Image lt;
        [SerializeField] private Image rt;
        [SerializeField] private Image lb;
        [SerializeField] private Image rb;

        [Header("Face buttons section")]
        [SerializeField] private Transform buttons;
        [SerializeField] private Image xButton;
        [SerializeField] private Image bButton;
        [SerializeField] private Image yButton;
        [SerializeField] private Image aButton;

        [Header("D-pad section")]
        [SerializeField] private Image leftCross;
        [SerializeField] private Image rightCross;
        [SerializeField] private Image upCross;
        [SerializeField] private Image downCross;

        [Header("Mini buttons section")]
        [SerializeField] private Transform minibuttons;
        [SerializeField] private Image back;
        [SerializeField] private Image start;

        [Header("Stick visualization and animation controls")]
        [SerializeField] private Transform sticks;
        [SerializeField] private Transform stickLeftHolder;
        [SerializeField] private Transform stickRightHolder;
        [SerializeField] private Image stickLeft;
        [SerializeField] private Image stickRight;
        [SerializeField] private Image stickLeftDown;
        [SerializeField] private Image stickRightDown;

        [Header("Tip Animation")]
        private float timeElapsed;
        private bool isStickLeftActive;
        private bool isStickRightActive;
        [SerializeField] private float amplitude = 30f;
        [SerializeField] private float frequency = 2f;
        [SerializeField] private float damping = 0.1f;
        #endregion

        #region Animation
        // Creates smooth rocking animation for better visual feedback
        private void Update()
        {
            if (isStickLeftActive)
            {
                AnimateStick(stickLeftHolder.transform);
            }
            if (isStickRightActive)
            {
                AnimateStick(stickRightHolder.transform);
            }
        }

        private void AnimateStick(Transform stickTransform)
        {
            timeElapsed += Time.deltaTime;
            float targetAngle = amplitude * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
            float currentAngle = stickTransform.localEulerAngles.z;
            float dampedAngle = Mathf.LerpAngle(currentAngle, targetAngle, damping * Time.deltaTime);
            stickTransform.localRotation = Quaternion.Euler(stickTransform.localEulerAngles.x, stickTransform.localEulerAngles.y, dampedAngle);
        }

#endregion
        #region Initialization
        // Matches button index to predefined UI elements and activates them
        public void GetTipType(int caseButton, TutorialSceneReferences references)
        {
            sceneReferences = references;

            ResetJoystickTip();

            if (caseButton == sceneReferences.inputController.buttonMappings[14].buttonIndex)
            {
                aButton.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[15].buttonIndex)
            {
                bButton.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[16].buttonIndex)
            {
                xButton.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[17].buttonIndex)
            {
                yButton.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[18].buttonIndex)
            {
                triggers.gameObject.SetActive(true);
                lb.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[19].buttonIndex)
            {
                triggers.gameObject.SetActive(true);
                rb.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[20].buttonIndex)
            {
                minibuttons.gameObject.SetActive(true);
                back.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[21].buttonIndex)
            {
                minibuttons.gameObject.SetActive(true);
                start.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[22].buttonIndex)
            {
                buttons.gameObject.SetActive(false);
                sticks.gameObject.SetActive(true);
                stickLeft.gameObject.SetActive(true);
                stickLeftDown.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[23].buttonIndex)
            {
                buttons.gameObject.SetActive(false);
                sticks.gameObject.SetActive(true);
                stickRight.gameObject.SetActive(true);
                stickRightDown.gameObject.SetActive(true);
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[27].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[0].buttonIndex
            )
            {
                rightCross.gameObject.SetActive(true);
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[26].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[1].buttonIndex
            )
            {
                leftCross.gameObject.SetActive(true);
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[24].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[2].buttonIndex
            )
            {
                upCross.gameObject.SetActive(true);
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[25].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[3].buttonIndex
            )
            {
                downCross.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[4].buttonIndex)
            {
                triggers.gameObject.SetActive(true);
                rt.gameObject.SetActive(true);
            }
            else if (caseButton == sceneReferences.inputController.buttonMappings[5].buttonIndex)
            {
                triggers.gameObject.SetActive(true);
                lt.gameObject.SetActive(true);
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[6].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[7].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[8].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[9].buttonIndex
            )
            {
                buttons.gameObject.SetActive(false);
                sticks.gameObject.SetActive(true);
                stickLeft.gameObject.SetActive(true);
                isStickLeftActive = true;
            }
            else if (
                caseButton == sceneReferences.inputController.buttonMappings[10].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[11].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[12].buttonIndex ||
                caseButton == sceneReferences.inputController.buttonMappings[13].buttonIndex
            )
            {
                buttons.gameObject.SetActive(false);
                sticks.gameObject.SetActive(true);
                stickRight.gameObject.SetActive(true);
                isStickRightActive = true;
            }
            else
            {
                Debug.LogWarning($"[ATM] Unknown button index: {caseButton}");
            }
        }

        private void ResetJoystickTip()
        {
            triggers.gameObject.SetActive(false);
            buttons.gameObject.SetActive(true);
            minibuttons.gameObject.SetActive(false);
            sticks.gameObject.SetActive(false);
            lt.gameObject.SetActive(false);
            rt.gameObject.SetActive(false);
            lb.gameObject.SetActive(false);
            rb.gameObject.SetActive(false);
            back.gameObject.SetActive(false);
            start.gameObject.SetActive(false);
            leftCross.gameObject.SetActive(false);
            rightCross.gameObject.SetActive(false);
            upCross.gameObject.SetActive(false);
            downCross.gameObject.SetActive(false);
            xButton.gameObject.SetActive(false);
            bButton.gameObject.SetActive(false);
            yButton.gameObject.SetActive(false);
            aButton.gameObject.SetActive(false);
            stickLeft.gameObject.SetActive(false);
            stickRight.gameObject.SetActive(false);
            stickRightDown.gameObject.SetActive(false);
            stickLeftDown.gameObject.SetActive(false);
        }
        #endregion
    }
}