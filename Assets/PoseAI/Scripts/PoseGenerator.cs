using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEditor;
using System;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Video;
using System.IO;

namespace PoseAI
{
    public class PoseGenerator : EditorWindow
    {
        public NNModel nNModel;
        public GameObject humanoid;
        private Model _model;
        private IWorker _worker;
        private PoseDecoder.JointPoint[] jointPoints;
        private const int JointNum = 24;
        public int InputImageSize = 448;
        private float InputImageSizeHalf;
        public int HeatMapCol = 28;
        private float InputImageSizeF;
        private int HeatMapCol_Squared;
        private int HeatMapCol_Cube;
        private float ImageScale;
        private float[] heatMap2D;
        private float[] offset2D;
        private float[] heatMap3D;
        private float[] offset3D;
        private float unit;
        private int JointNum_Squared = JointNum * 2;
        private int JointNum_Cube = JointNum * 3;
        private int HeatMapCol_JointNum;
        private int CubeOffsetLinear;
        private int CubeOffsetSquared;
        public float highPassFilter = 0.0001f;
        public float lowPassFilter = 0.015f;
        public float smoothing = 0.05f;
        private GameObject humanoidPrefab;
        public Texture2D inputImg;
        public bool useFingerAI = true;
        public float fingerAIStrength = 0.25f;
        PoseAIPostProcessor poseAIPostProcessor;

        public void GUIGeneratePose()
        {
            EditorCoroutineUtility.StartCoroutine(GeneratePose(inputImg), this);
            humanoidPrefab = humanoid;
            TPose();
            if (humanoidPrefab.GetComponent<PoseDecoder>() == null)
                humanoidPrefab.AddComponent<PoseDecoder>();
            if (useFingerAI)
            {
                poseAIPostProcessor = CreateInstance<PoseAIPostProcessor>();
                poseAIPostProcessor.humanoid = humanoidPrefab;
                poseAIPostProcessor.Init();
            }
        }

        IEnumerator GeneratePose(Texture2D inputImg)
        {
            jointPoints = humanoidPrefab.GetComponent<PoseDecoder>().Init(humanoidPrefab);
            HeatMapCol_Squared = HeatMapCol * HeatMapCol;
            HeatMapCol_Cube = HeatMapCol * HeatMapCol * HeatMapCol;
            HeatMapCol_JointNum = HeatMapCol * JointNum;
            CubeOffsetLinear = HeatMapCol * JointNum_Cube;
            CubeOffsetSquared = HeatMapCol_Squared * JointNum_Cube;
            heatMap2D = new float[JointNum * HeatMapCol_Squared];
            offset2D = new float[JointNum * HeatMapCol_Squared * 2];
            heatMap3D = new float[JointNum * HeatMapCol_Cube];
            offset3D = new float[JointNum * HeatMapCol_Cube * 3];
            unit = 1f / (float)HeatMapCol;
            InputImageSizeF = InputImageSize;
            InputImageSizeHalf = InputImageSizeF / 2f;
            ImageScale = InputImageSize / (float)HeatMapCol;// 224f / (float)InputImageSize;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _model = ModelLoader.Load(nNModel);
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _model);
            input = new Tensor(inputImg);
            if (inputs[inputName_1] == null)
            {
                inputs[inputName_1] = input;
                inputs[inputName_2] = new Tensor(inputImg);
                inputs[inputName_3] = new Tensor(inputImg);
            }
            else
            {
                inputs[inputName_3].Dispose();

                inputs[inputName_3] = inputs[inputName_2];
                inputs[inputName_2] = inputs[inputName_1];
                inputs[inputName_1] = input;
            }

            yield return _worker.StartManualSchedule(inputs);

            // Get outputs
            for (var i = 2; i < _model.outputs.Count; i++)
            {
                b_outputs[i] = _worker.PeekOutput(_model.outputs[i]);
            }

            // Get data from outputs
            offset3D = b_outputs[2].data.Download(b_outputs[2].shape);
            heatMap3D = b_outputs[3].data.Download(b_outputs[3].shape);

            // Release outputs
            for (var i = 2; i < b_outputs.Length; i++)
            {
                b_outputs[i].Dispose();
            }
            input?.Dispose();
            _worker?.Dispose();

            PredictPose();
            humanoidPrefab.GetComponent<PoseDecoder>().PoseUpdate();
            if (useFingerAI)
            {
                poseAIPostProcessor.humanoid = humanoidPrefab;
                poseAIPostProcessor.RunPostProcessor(fingerAIStrength);
            }
        }

        private const string inputName_1 = "1";
        private const string inputName_2 = "2";
        private const string inputName_3 = "3";
        Tensor input = new Tensor();
        Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>() { { inputName_1, null }, { inputName_2, null }, { inputName_3, null }, };
        Tensor[] b_outputs = new Tensor[4];

        private void PredictPose()
        {
            for (var j = 0; j < JointNum; j++)
            {
                var maxXIndex = 0;
                var maxYIndex = 0;
                var maxZIndex = 0;
                jointPoints[j].score3D = 0.0f;
                var jj = j * HeatMapCol;
                for (var z = 0; z < HeatMapCol; z++)
                {
                    var zz = jj + z;
                    for (var y = 0; y < HeatMapCol; y++)
                    {
                        var yy = y * HeatMapCol_Squared * JointNum + zz;
                        for (var x = 0; x < HeatMapCol; x++)
                        {
                            float v = heatMap3D[yy + x * HeatMapCol_JointNum];
                            if (v > jointPoints[j].score3D)
                            {
                                jointPoints[j].score3D = v;
                                maxXIndex = x;
                                maxYIndex = y;
                                maxZIndex = z;
                            }
                        }
                    }
                }

                jointPoints[j].Now3D.x = (offset3D[maxYIndex * CubeOffsetSquared + maxXIndex * CubeOffsetLinear + j * HeatMapCol + maxZIndex] + 0.5f + (float)maxXIndex) * ImageScale - InputImageSizeHalf;
                jointPoints[j].Now3D.y = InputImageSizeHalf - (offset3D[maxYIndex * CubeOffsetSquared + maxXIndex * CubeOffsetLinear + (j + JointNum) * HeatMapCol + maxZIndex] + 0.5f + (float)maxYIndex) * ImageScale;
                jointPoints[j].Now3D.z = (offset3D[maxYIndex * CubeOffsetSquared + maxXIndex * CubeOffsetLinear + (j + JointNum_Squared) * HeatMapCol + maxZIndex] + 0.5f + (float)(maxZIndex - 14)) * ImageScale;
            }

            // Calculate hip location
            var lc = (jointPoints[PositionIndex.rThighBend.Int()].Now3D + jointPoints[PositionIndex.lThighBend.Int()].Now3D) / 2f;
            jointPoints[PositionIndex.hip.Int()].Now3D = (jointPoints[PositionIndex.abdomenUpper.Int()].Now3D + lc) / 2f;

            // Calculate neck location
            jointPoints[PositionIndex.neck.Int()].Now3D = (jointPoints[PositionIndex.rShldrBend.Int()].Now3D + jointPoints[PositionIndex.lShldrBend.Int()].Now3D) / 2f;

            // Calculate head location
            var cEar = (jointPoints[PositionIndex.rEar.Int()].Now3D + jointPoints[PositionIndex.lEar.Int()].Now3D) / 2f;
            var hv = cEar - jointPoints[PositionIndex.neck.Int()].Now3D;
            var nhv = Vector3.Normalize(hv);
            var nv = jointPoints[PositionIndex.Nose.Int()].Now3D - jointPoints[PositionIndex.neck.Int()].Now3D;
            jointPoints[PositionIndex.head.Int()].Now3D = jointPoints[PositionIndex.neck.Int()].Now3D + nhv * Vector3.Dot(nhv, nv);

            // Calculate spine location
            jointPoints[PositionIndex.spine.Int()].Now3D = jointPoints[PositionIndex.abdomenUpper.Int()].Now3D;

            // Kalman filter
            foreach (var jp in jointPoints)
            {
                KUpdate(jp);
            }


            foreach (var jp in jointPoints)
            {
                jp.PrevPos3D[0] = jp.Pos3D;
                for (var i = 1; i < jp.PrevPos3D.Length; i++)
                {
                    jp.PrevPos3D[i] = jp.PrevPos3D[i] * smoothing + jp.PrevPos3D[i - 1] * (1f - smoothing);
                }
                jp.Pos3D = jp.PrevPos3D[jp.PrevPos3D.Length - 1];
            }

        }

        /// <summary>
        /// Kalman filter
        /// </summary>
        /// <param name="measurement">joint points</param>
        void KUpdate(PoseDecoder.JointPoint measurement)
        {
            measurementUpdate(measurement);
            measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
            measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
            measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
            measurement.X = measurement.Pos3D;
        }

        void measurementUpdate(PoseDecoder.JointPoint measurement)
        {
            measurement.K.x = (measurement.P.x + highPassFilter) / (measurement.P.x + highPassFilter + lowPassFilter);
            measurement.K.y = (measurement.P.y + highPassFilter) / (measurement.P.y + highPassFilter + lowPassFilter);
            measurement.K.z = (measurement.P.z + highPassFilter) / (measurement.P.z + highPassFilter + lowPassFilter);
            measurement.P.x = lowPassFilter * (measurement.P.x + highPassFilter) / (lowPassFilter + measurement.P.x + highPassFilter);
            measurement.P.y = lowPassFilter * (measurement.P.y + highPassFilter) / (lowPassFilter + measurement.P.y + highPassFilter);
            measurement.P.z = lowPassFilter * (measurement.P.z + highPassFilter) / (lowPassFilter + measurement.P.z + highPassFilter);
        }
        public void TPose()
        {
            GameObject selected = humanoid;

            if (!selected) return; // If no object was selected, exit.
            if (!selected.TryGetComponent<Animator>(out Animator animator)) return; // If the selected object has no animator, exit.
            if (!animator.avatar) return;

            SkeletonBone[] skeletonbones = animator.avatar?.humanDescription.skeleton; // Get the list of bones in the armature.

            foreach (SkeletonBone sb in skeletonbones) // Loop through all bones in the armature.
            {
                foreach (HumanBodyBones hbb in Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (hbb != HumanBodyBones.LastBone)
                    {

                        Transform bone = animator.GetBoneTransform(hbb);
                        if (bone != null)
                        {

                            if (sb.name == bone.name) // If this bone is a normal humanoid bone (as opposed to an ear or tail bone), reset its transform.
                            {

                                // The bicycle pose happens when for some reason the transforms of an avatar's bones are incorectly saved in a state that is not the t-pose.
                                // For most of the bones this affects only their rotation, but for the hips, the position is affected as well.
                                // As the scale should be untouched, and the user may have altered these intentionally, we should leave them alone.

                                if (hbb == HumanBodyBones.Hips) bone.localPosition = sb.position;
                                bone.localRotation = sb.rotation;
                                //bone.localScale = sb.scale;

                                // An alternative to setting the values above would be to revert each bone to its prefab state like so:
                                // RevertObjectOverride(boneT.gameObject, InteractionMode.UserAction); // InteractionMode.UserAction should save the changes to the undo history.

                                // Though this may only work if the object actually is a prefab, and it would overwrite any user changes to scale or position, and who knows what else.

                                break; // We found a humanbodybone that matches, so we need not check the rest against this skeleton bone.

                            }

                        }
                    }
                }

            }

        }
    }
}
