using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using System.Threading;

namespace PoseAI
{
    public class ConstraintPoseCanvasController : MonoBehaviour
    {
        public Button poseEditorButton;
        PoseAI poseAI;
        GameObject leftFootTarget, rightFootTarget, rightHandTarget, leftHandTarget;
        Vector3 leftFootInitPosition, rightFootInitPosition, rightHandInitPosition, leftHandInitPosition;
        int flip = -1;

        void Start()
        {
            poseEditorButton.onClick.AddListener(OpenPoseEditorAndExit);
            poseAI = ScriptableObject.CreateInstance<PoseAI>();
            poseAI.AutoSetupCharacter();
            poseAI.UIhips = true;
            poseAI.snapSpeed = 0.01f;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            StartCoroutine(HeadAction());
            StartCoroutine(RotateHipsTimer(5));
        }

        void OpenPoseEditorAndExit()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            UnityEditor.EditorApplication.isPlaying = false;
            SceneView sceneView = SceneView.lastActiveSceneView;
            Debug.Log("Please open the PoseAI Editor window (Window > PoseAI) to access the full range of features designed specifically for running in the editor.");
            if (sceneView != null)
            {
                sceneView.Focus();
            }
            else
            {
                Debug.LogWarning("Please switch to the scene view.");
            }

        }
        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.Focus();
                }
                else
                {
                    Debug.LogWarning("Please switch to the scene view.");
                }
                EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;

            }
        }
        void OnDisable()
        {
            if (poseAI != null)
            {
                DestroyImmediate(poseAI);
                poseAI = null;
            }

        }
        void LateUpdate()
        {
            poseAI.skeletonTransformsList.transforms[0].transform.Rotate(Vector3.up * flip, Time.deltaTime * 2);

            if (leftFootTarget != null)
            {
                leftFootTarget.transform.position = Vector3.Lerp(leftFootTarget.transform.position, leftFootInitPosition + new Vector3(0, 0.2f + Mathf.Cos(Time.time) * 0.2f, 0), Time.deltaTime);
            }
            if (rightFootTarget != null)
            {
                rightFootTarget.transform.position = Vector3.Lerp(rightFootTarget.transform.position, rightFootInitPosition + new Vector3(0, 0.2f + Mathf.Sin(Time.time) * 0.2f, 0), Time.deltaTime);
            }
            if (rightHandTarget != null)
            {
                rightHandTarget.transform.position = Vector3.Lerp(rightHandTarget.transform.position, rightHandInitPosition + new Vector3(Mathf.Cos(Time.time) * 0.1f, Mathf.Sin(Time.time) * 0.1f, 0), Time.deltaTime);
            }
            if (leftHandTarget != null)
            {
                leftHandTarget.transform.position = Vector3.Lerp(leftHandTarget.transform.position, leftHandInitPosition + new Vector3(Mathf.Cos(Time.time) * 0.1f, Mathf.Sin(Time.time) * 0.1f, 0), Time.deltaTime);
            }

            if (poseAI.headTarget != null)
            {
                poseAI.skeletonTransformsList.transforms[32].transform.LookAt(poseAI.headTarget.transform);
                poseAI.headTarget.transform.position = new Vector3(Mathf.Cos(Time.time) * 1f, 1 + Mathf.Sin(Time.time) * 1f, 2);
            }



            List<float> rotations = new List<float>();
            for (int i = 0; i < poseAI.constraintTransformsList.transforms.Count; i++)
            {
                rotations.Add(poseAI.constraintTransformsList.transforms[i].rotation.x);
                rotations.Add(poseAI.constraintTransformsList.transforms[i].rotation.y);
                rotations.Add(poseAI.constraintTransformsList.transforms[i].rotation.z);
                rotations.Add(poseAI.constraintTransformsList.transforms[i].rotation.w);
            }
            poseAI.RunInference(rotations.ToArray(), poseAI.SelectModelBasedOnName());


        }
        void FixedUpdate()
        {
            for (int i = 0; i < poseAI.constraintCount; i++)
            {
                poseAI.ResolveIK(poseAI.Tips[i], poseAI.Targets[i], poseAI.Poles[i]);
            }
        }
        IEnumerator HeadAction()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIhead = true;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            StartCoroutine(LeftLegAction());
        }
        IEnumerator LeftLegAction()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIleftLeg = true;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            leftFootInitPosition = leftFootTarget.transform.position;
            StartCoroutine(RightLegAction());
        }
        IEnumerator RightLegAction()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIrightLeg = true;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            rightFootTarget = GameObject.Find("RightFoot_Target");
            rightFootInitPosition = rightFootTarget.transform.position;
            StartCoroutine(RightArmAction());
        }
        IEnumerator RightArmAction()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIrightArm = true;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            rightFootTarget = GameObject.Find("RightFoot_Target");
            rightHandTarget = GameObject.Find("RightHand_Target");
            rightHandInitPosition = rightHandTarget.transform.position;
            StartCoroutine(LeftArmAction());
        }
        IEnumerator LeftArmAction()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIleftArm = true;
            poseAI.UIrightLeg = false;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            rightFootTarget = GameObject.Find("RightFoot_Target");
            rightHandTarget = GameObject.Find("RightHand_Target");
            leftHandTarget = GameObject.Find("LeftHand_Target");
            leftHandInitPosition = leftHandTarget.transform.position;
            StartCoroutine(SwitchOff());
        }
        IEnumerator SwitchOff()
        {
            yield return new WaitForSecondsRealtime(2);
            poseAI.UIleftArm = false;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            rightFootTarget = GameObject.Find("RightFoot_Target");
            rightHandTarget = GameObject.Find("RightHand_Target");

            yield return new WaitForSecondsRealtime(2);
            poseAI.UIrightArm = false;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");
            rightFootTarget = GameObject.Find("RightFoot_Target");

            yield return new WaitForSecondsRealtime(2);
            poseAI.UIrightLeg = false;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;
            leftFootTarget = GameObject.Find("LeftFoot_Target");

            yield return new WaitForSecondsRealtime(2);
            poseAI.UIleftLeg = false;
            poseAI.UpdateConstraints();
            poseAI.delayColorFrames = 100;
            EditorApplication.update += poseAI.UpdateMaterialColors;

            StartCoroutine(LeftLegAction());

        }
        IEnumerator RotateHipsTimer(int time)
        {
            yield return new WaitForSecondsRealtime(time);
            flip *= -1;
            StartCoroutine(RotateHipsTimer(10));
        }
    }
}
