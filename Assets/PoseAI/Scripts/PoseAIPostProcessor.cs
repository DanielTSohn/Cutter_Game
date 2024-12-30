using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Barracuda;
namespace PoseAI
{
    public class PoseAIPostProcessor : EditorWindow
    {
        public GameObject humanoid;
        private IWorker worker;
        float[] predictedValues;
        public GameObject characterPrefabInstance;
        public TransformList skeletonTransformsList = new TransformList();
        public TransformList constraintTransformsList = new TransformList();
        public NNModel[] neuralNetworkModels;

        public void Init()
        {
            PopulateSkeletonTransforms(humanoid);
            PopulateNNModels();
        }
        public void RunPostProcessor(float strength)
        {
            UpdateConstraints();
            List<float> rotations = new List<float>();

            for (int i = 0; i < constraintTransformsList.transforms.Count; i++)
            {
                rotations.Add(constraintTransformsList.transforms[i].rotation.x);
                rotations.Add(constraintTransformsList.transforms[i].rotation.y);
                rotations.Add(constraintTransformsList.transforms[i].rotation.z);
                rotations.Add(constraintTransformsList.transforms[i].rotation.w);
            }

            RunInference(rotations.ToArray(), "hipsleftArmrightArm", strength);
        }

        void RunInference(float[] rotations, string modelName, float strength)
        {
            // Find name and Select model
            Model model;
            for (int j = 0; j < neuralNetworkModels.Length; j++)
            {
                if (modelName == neuralNetworkModels[j].name)
                {
                    model = ModelLoader.Load(neuralNetworkModels[j]);
                    worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
                    //Below implementation as float[1,variable] cannot compile
                    var input = new Tensor();
                    if (rotations.Length / 4 == 1)
                        input = new Tensor(1, 4, new float[1, 4] { { rotations[0], rotations[1], rotations[2], rotations[3] } });
                    else if (rotations.Length / 4 == 2)
                        input = new Tensor(1, 8, new float[1, 8] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], } });
                    else if (rotations.Length / 4 == 3)
                        input = new Tensor(1, 12, new float[1, 12] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], } });
                    else if (rotations.Length / 4 == 4)
                        input = new Tensor(1, 16, new float[1, 16] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], } });
                    else if (rotations.Length / 4 == 5)
                        input = new Tensor(1, 20, new float[1, 20] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], } });
                    else if (rotations.Length / 4 == 6)
                        input = new Tensor(1, 24, new float[1, 24] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], rotations[20], rotations[21], rotations[22], rotations[23], } });
                    else if (rotations.Length / 4 == 7)
                        input = new Tensor(1, 28, new float[1, 28] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], rotations[20], rotations[21], rotations[22], rotations[23], rotations[24], rotations[25], rotations[26], rotations[27], } });
                    else if (rotations.Length / 4 == 8)
                        input = new Tensor(1, 32, new float[1, 32] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], rotations[20], rotations[21], rotations[22], rotations[23], rotations[24], rotations[25], rotations[26], rotations[27], rotations[28], rotations[29], rotations[30], rotations[31], } });
                    else if (rotations.Length / 4 == 9)
                        input = new Tensor(1, 36, new float[1, 36] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], rotations[20], rotations[21], rotations[22], rotations[23], rotations[24], rotations[25], rotations[26], rotations[27], rotations[28], rotations[29], rotations[30], rotations[31], rotations[32], rotations[33], rotations[34], rotations[35], } });
                    else if (rotations.Length / 4 == 10)
                        input = new Tensor(1, 40, new float[1, 40] { { rotations[0], rotations[1], rotations[2], rotations[3], rotations[4], rotations[5], rotations[6], rotations[7], rotations[8], rotations[9], rotations[10], rotations[11], rotations[12], rotations[13], rotations[14], rotations[15], rotations[16], rotations[17], rotations[18], rotations[19], rotations[20], rotations[21], rotations[22], rotations[23], rotations[24], rotations[25], rotations[26], rotations[27], rotations[28], rotations[29], rotations[30], rotations[31], rotations[32], rotations[33], rotations[34], rotations[35], rotations[36], rotations[37], rotations[38], rotations[39], } });
                    worker.Execute(input);
                    Tensor output = worker.PeekOutput("11");
                    predictedValues = output.ToReadOnlyArray();
                    worker.Dispose();
                    input.Dispose();
                    output.Dispose();
                    //Decoder
                    List<int> fixedBones = new List<int>();
                    if (modelName.Contains("hips"))
                        fixedBones.Add(0);
                    if (modelName.Contains("leftLeg"))
                    {
                        fixedBones.Add(1);
                        fixedBones.Add(2);
                    }
                    if (modelName.Contains("rightLeg"))
                    {
                        fixedBones.Add(5);
                        fixedBones.Add(6);
                    }
                    if (modelName.Contains("leftArm"))
                    {
                        fixedBones.Add(13);
                        fixedBones.Add(14);
                    }
                    if (modelName.Contains("rightArm"))
                    {
                        fixedBones.Add(34);
                        fixedBones.Add(35);
                    }
                    if (modelName.Contains("head"))
                    {
                        fixedBones.Add(32);
                    }
                    List<int> sortedBones = new List<int>();
                    for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    {
                        if (!fixedBones.Contains(i))
                            sortedBones.Add(i);
                    }
                    for (int i = 13; i < 28; i++)
                    {
                        //                        Debug.Log(skeletonTransformsList.transforms[sortedBones[i]].name);
                        skeletonTransformsList.transforms[sortedBones[i]].rotation = Quaternion.Slerp(skeletonTransformsList.transforms[sortedBones[i]].rotation, new Quaternion(predictedValues[i * 4], predictedValues[(i * 4) + 1], predictedValues[(i * 4) + 2], predictedValues[(i * 4) + 3]), strength);
                    }
                    for (int i = 32; i < 47; i++)
                    {
                        skeletonTransformsList.transforms[sortedBones[i]].rotation = Quaternion.Slerp(skeletonTransformsList.transforms[sortedBones[i]].rotation, new Quaternion(predictedValues[i * 4], predictedValues[(i * 4) + 1], predictedValues[(i * 4) + 2], predictedValues[(i * 4) + 3]), strength);

                    }

                }

            }

        }

        [System.Serializable]
        public class TransformList
        {
            public List<Transform> transforms = new List<Transform>();
        }
        void PopulateSkeletonTransforms(GameObject humanoid)
        {
            skeletonTransformsList.transforms.Clear();

            Transform[] allTransforms = humanoid.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                if (IsSkeletonBone(t))
                {
                    skeletonTransformsList.transforms.Add(t);
                }
            }
        }
        bool IsSkeletonBone(Transform transform)
        {
            string[] boneNames = { "Hips", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase", "Spine", "Spine1", "Spine2", "Spine3", "Neck", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand" };

            foreach (string name in boneNames)
            {
                if (transform.name.EndsWith("_End") || transform.name.EndsWith("_end") || transform.name.EndsWith("4"))
                {
                    return false;
                }

                if (transform.name.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }
        private void PopulateNNModels()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:NNModel", new string[] { "Assets/PoseAI/NNModels" });
            neuralNetworkModels = new NNModel[modelGuids.Length];

            for (int i = 0; i < modelGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
                if (assetPath.EndsWith(".onnx"))
                {
                    neuralNetworkModels[i] = AssetDatabase.LoadAssetAtPath<NNModel>(assetPath);
                }
            }
        }
        void UpdateConstraints()
        {
            List<int> t = new List<int>();
            t.Clear();

            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                if (skeletonTransformsList.transforms[i].name.Contains("Hips"))
                    t.Add(i);

            // for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
            //     if (skeletonTransformsList.transforms[i].name.Contains("LeftUpLeg"))
            //         t.Add(i);
            // for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
            //     if (skeletonTransformsList.transforms[i].name.Contains("LeftLeg"))
            //         t.Add(i);

            // for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
            //     if (skeletonTransformsList.transforms[i].name.Contains("RightUpLeg"))
            //         t.Add(i);
            // for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
            //     if (skeletonTransformsList.transforms[i].name.Contains("RightLeg"))
            //         t.Add(i);

            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                if (skeletonTransformsList.transforms[i].name.Contains("LeftArm"))
                    t.Add(i);
            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                if (skeletonTransformsList.transforms[i].name.Contains("LeftForeArm"))
                    t.Add(i);


            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                if (skeletonTransformsList.transforms[i].name.Contains("RightArm"))
                    t.Add(i);
            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                if (skeletonTransformsList.transforms[i].name.Contains("RightForeArm"))
                    t.Add(i);


            PopulateConstraints(t.ToArray());


        }
        void PopulateConstraints(int[] t)
        {
            constraintTransformsList.transforms.Clear();
            for (int i = 0; i < t.Length; i++)
                constraintTransformsList.transforms.Add(skeletonTransformsList.transforms[t[i]]);
        }
    }
}
