using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Unity.Barracuda;
using System.Text.RegularExpressions;
using UnityEngine.Video;
using System.IO;

namespace PoseAI
{
    public class PoseAI : EditorWindow
    {
        public GameObject humanoid;
        private IWorker worker;
        float[] predictedValues;
        private SerializedObject serializedObject;
        private SerializedProperty skeletonTransformsProperty, constraintTransformsProperty, neuralNetworkModelsProperty;
        public GameObject characterPrefabInstance;
        public TransformList skeletonTransformsList = new TransformList();
        public TransformList constraintTransformsList = new TransformList();
        public NNModel[] neuralNetworkModels;
        private Vector2 scrollPosition;

        public List<Transform> Tips = new List<Transform>();
        public List<Transform> Targets = new List<Transform>();
        public List<Transform> Poles = new List<Transform>();
        public Transform Root;
        float[] BonesLength;
        float CompleteLength;
        Transform[] Bones;
        Vector3[] Positions;
        Vector3[] StartDirectionSucc;
        Quaternion[] StartRotationBone;
        Quaternion StartRotationTarget;
        public bool UIhips = false, UIleftLeg, UIrightLeg, UIleftArm, UIrightArm, UIhead;
        bool neuralAndIK = true, iKOnly;
        public int constraintCount;
        public float snapSpeed = 0.1f;
        public bool isHoveredUIhips, isHoveredUIleftLeg, isHoveredUIleftLegHint, isHoveredUIrightLeg, isHoveredUIrightLegHint, isHoveredUIleftArm, isHoveredUIleftArmHint, isHoveredUIrightArm, isHoveredUIrightArmHint, isHoveredUIhead;
        public GameObject headTarget;
        bool showLabels;
        public int delayFrames, delayColorFrames;
        private Color UISceneColor = Color.red;
        Color iKMaterial = new Color(0.7f, 0.9f, 1), aIMaterial = new Color(0.25f, 0.7f, 1);
        PoseBuilder poseBuilder;
        PoseGenerator poseGenerator;
        Texture2D inputImg;
        AnimationClip animationClip;
        Editor animationEditor;
        Color orgGUICol;
        float frameInterval = 0.2f;
        int currentPoseFrame;
        float animationScrubTime, animationScrubSpeed = 1;
        bool playAnimation;
        private Texture2D scaledImage;
        GameObject ragdollAnimationPreview;

        [MenuItem("Window/PoseAI")]
        public static void ShowWindow()
        {
            GetWindow(typeof(PoseAI));
        }
        public void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            serializedObject = new SerializedObject(this);
            skeletonTransformsProperty = serializedObject.FindProperty("skeletonTransformsList");
            constraintTransformsProperty = serializedObject.FindProperty("constraintTransformsList");
            neuralNetworkModelsProperty = serializedObject.FindProperty("neuralNetworkModels");

            poseGenerator = CreateInstance<PoseGenerator>();
            orgGUICol = GUI.backgroundColor;
            poseBuilder = null;
            ragdollAnimationPreview = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/PoseAI/Models/RagdollAnimationPreview.fbx", typeof(GameObject));
        }
        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

        }
        public void OnSceneGUI(SceneView sceneView)
        {
            if (neuralAndIK && humanoid != null)
            {
                List<float> rotations = new List<float>();
                for (int i = 0; i < constraintTransformsList.transforms.Count; i++)
                {
                    rotations.Add(constraintTransformsList.transforms[i].rotation.x);
                    rotations.Add(constraintTransformsList.transforms[i].rotation.y);
                    rotations.Add(constraintTransformsList.transforms[i].rotation.z);
                    rotations.Add(constraintTransformsList.transforms[i].rotation.w);
                }
                RunInference(rotations.ToArray(), SelectModelBasedOnName());
            }
            // Handles and UI
            HandleUI();
        }
        public void HandleUI()
        {
            if (UIhips)
            {
                if (HandleUtility.DistanceToCircle(skeletonTransformsList.transforms[0].transform.position, 0.025f) <= 0f && Event.current.type != EventType.MouseDrag)
                {
                    isHoveredUIhips = true;
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
                else
                {
                    isHoveredUIhips = false;
                }
                Handles.color = Handles.color = isHoveredUIhips ? new Color(UISceneColor.r * 1.2f, UISceneColor.g * 1.2f, UISceneColor.b * 1.2f, 0.75f) : new Color(UISceneColor.r, UISceneColor.g, UISceneColor.b, 0.5f);
                Handles.SphereHandleCap(0, skeletonTransformsList.transforms[0].transform.position, Quaternion.identity, 0.06f, EventType.Repaint);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                if (showLabels)
                    Handles.Label(skeletonTransformsList.transforms[0].transform.position, "Hips", style);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHoveredUIhips)
                {
                    Selection.activeGameObject = skeletonTransformsList.transforms[0].gameObject;
                }
            }

            if (UIhead)
            {
                if (HandleUtility.DistanceToCircle(headTarget.transform.position + headTarget.transform.up * 0.1f, 0.025f) <= 0f && Event.current.type != EventType.MouseDrag)
                {
                    isHoveredUIhead = true;
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
                else
                {
                    isHoveredUIhead = false;
                }
                Handles.color = Handles.color = isHoveredUIhead ? new Color(UISceneColor.r * 1.2f, UISceneColor.g * 1.2f, UISceneColor.b * 1.2f, 0.75f) : new Color(UISceneColor.r, UISceneColor.g, UISceneColor.b, 0.5f);
                Handles.SphereHandleCap(0, headTarget.transform.position + headTarget.transform.up * 0.1f, Quaternion.identity, 0.06f, EventType.Repaint);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                if (showLabels)
                    Handles.Label(headTarget.transform.position, "Look", style);
                Handles.DrawDottedLine(skeletonTransformsList.transforms[32].transform.position + skeletonTransformsList.transforms[32].up * 0.1f, headTarget.transform.position + headTarget.transform.up * 0.1f, 10f);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHoveredUIhead)
                {
                    Selection.activeGameObject = headTarget.gameObject;
                }

                skeletonTransformsList.transforms[32].transform.LookAt(headTarget.transform);
            }
            if (UIleftLeg)
                DrawTwoBoneIK("LeftFoot", isHoveredUIleftLeg, isHoveredUIleftLegHint);
            if (UIrightLeg)
                DrawTwoBoneIK("RightFoot", isHoveredUIrightLeg, isHoveredUIrightLegHint);
            if (UIleftArm)
                DrawTwoBoneIK("LeftHand", isHoveredUIleftArm, isHoveredUIleftArmHint);
            if (UIrightArm)
                DrawTwoBoneIK("RightHand", isHoveredUIrightArm, isHoveredUIrightArmHint);

            if (Event.current.type == EventType.Layout)
                for (int i = 0; i < constraintCount; i++)
                    ResolveIK(Tips[i], Targets[i], Poles[i]);
        }

        public void RunInference(float[] rotations, string modelName)
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
                    for (int i = 0; i < sortedBones.Count; i++)
                        skeletonTransformsList.transforms[sortedBones[i]].rotation = Quaternion.Slerp(skeletonTransformsList.transforms[sortedBones[i]].rotation, new Quaternion(predictedValues[i * 4], predictedValues[(i * 4) + 1], predictedValues[(i * 4) + 2], predictedValues[(i * 4) + 3]), snapSpeed);

                }

            }

        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = 14;
            EditorGUILayout.BeginVertical("Window");
            EditorGUILayout.LabelField("━━━ Pose Editor ━━━", headerStyle);
            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Setup Character", GUILayout.Height(30))) AutoSetupCharacter();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Character Setup Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            humanoid = EditorGUILayout.ObjectField("Character", humanoid, typeof(GameObject), true) as GameObject;
            characterPrefabInstance = EditorGUILayout.ObjectField("Character Prefab Instance", characterPrefabInstance, typeof(GameObject), true) as GameObject;
            serializedObject.Update();

            EditorGUILayout.PropertyField(skeletonTransformsProperty, true);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Populate Skeleton Transforms"))
            {
                PopulateSkeletonTransforms(humanoid);
            }
            EditorGUILayout.Space();
            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); //End Box

            EditorGUILayout.Space();

            // EditorGUILayout.LabelField("Neural Network Model", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(neuralNetworkModelsProperty, true);
            // if (GUILayout.Button("Populate NN Models"))
            // {
            //     PopulateNNModels();
            // }
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.Label("Neural Control Rig", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();


            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 12;

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
                                       //Mutual Exclusivity
            bool prevNeuralAndIK = neuralAndIK;
            bool prevIKOnly = iKOnly;

            neuralAndIK = GUILayout.Toggle(neuralAndIK, "Neural + IK", buttonStyle, GUILayout.Width(100));
            iKOnly = GUILayout.Toggle(iKOnly, "IK Only", buttonStyle, GUILayout.Width(100));
            // Enforce mutual exclusivity
            if (neuralAndIK && neuralAndIK != prevNeuralAndIK)

                iKOnly = false;

            else if (iKOnly && iKOnly != prevIKOnly)

                neuralAndIK = false;
            else if (!iKOnly && !neuralAndIK)
                iKOnly = true;

            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
            UIhips = GUILayout.Toggle(UIhips, "Hips", buttonStyle, GUILayout.Width(100));
            UIhead = GUILayout.Toggle(UIhead, "Head", buttonStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
            UIleftArm = GUILayout.Toggle(UIleftArm, "Left Arm", buttonStyle, GUILayout.Width(100));
            UIrightArm = GUILayout.Toggle(UIrightArm, "Right Arm", buttonStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
            UIleftLeg = GUILayout.Toggle(UIleftLeg, "Left Leg", buttonStyle, GUILayout.Width(100));
            UIrightLeg = GUILayout.Toggle(UIrightLeg, "Right Leg", buttonStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace(); // Center horizontally
            if (GUILayout.Button("All", GUILayout.Width(100)))
                UIhead = UIhips = UIleftArm = UIleftLeg = UIrightArm = UIrightLeg = true;
            if (GUILayout.Button("None", GUILayout.Width(100)))
                UIhead = UIhips = UIleftArm = UIleftLeg = UIrightArm = UIrightLeg = false;

            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Center horizontally
            GUI.color = new Color(0.75f, 1, 0.75f);
            //UpdateContranist needs to be placed before Fix Skeleton otherwise there is a very annoying bug
            if (EditorGUI.EndChangeCheck())
            {
                UpdateConstraints();
                delayColorFrames = 120;
                EditorApplication.update += UpdateMaterialColors;
            }
            if (GUILayout.Button("Fix Skeleton", GUILayout.Width(100)))
            {
                ResetBrokenSkeleton();
                delayFrames = 100;
                EditorApplication.update += UpdateSceneView;
            }
            GUI.color = new Color(1, 0.75f, 0.75f);
            if (GUILayout.Button("Reset Skeleton", GUILayout.Width(100)))
            {
                UIhead = UIhips = UIleftArm = UIleftLeg = UIrightArm = UIrightLeg = false;
                poseGenerator.humanoid = humanoid;
                poseGenerator.TPose();
                UpdateConstraints();
                delayColorFrames = 100;
                EditorApplication.update += UpdateMaterialColors;

            }
            GUI.color = orgGUICol;
            GUILayout.FlexibleSpace(); // Center horizontally
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); //End Box
            EditorGUILayout.Space();
            if (UIleftArm == true && UIhips == false || UIrightArm == true && UIhips == false || UIleftLeg == true && UIhips == false || UIrightLeg == true && UIhips == false)
                EditorGUILayout.HelpBox("Multiple possible positions possible with the current configuration, results may be suboptimal. Please select the Hips constraint to provide an anchor point. Click on Fix Skeleton if broken.", MessageType.Warning);




            //EditorGUILayout.PropertyField(constraintTransformsProperty, true);
            GUILayout.Label("IK Constraints", EditorStyles.boldLabel);
            serializedObject.ApplyModifiedProperties();
            for (int i = 0; i < constraintCount; i++)
            {
                Tips[i] = EditorGUILayout.ObjectField("Tip", Tips[i], typeof(Transform), true) as Transform;
                Targets[i] = EditorGUILayout.ObjectField("Target", Targets[i], typeof(Transform), true) as Transform;
                Poles[i] = EditorGUILayout.ObjectField("Hint", Poles[i], typeof(Transform), true) as Transform;
                EditorGUILayout.Space(10);
            }
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.Space();
            GUILayout.Label("Scene GUI Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            snapSpeed = EditorGUILayout.Slider("Snap Speed", snapSpeed, 0.1f, 1f);
            showLabels = EditorGUILayout.Toggle("Show Bone Labels", showLabels);
            UISceneColor = EditorGUILayout.ColorField("Selected Color", UISceneColor);
            iKMaterial = EditorGUILayout.ColorField("IK Controlled Color", iKMaterial);
            aIMaterial = EditorGUILayout.ColorField("AI Controlled Color", aIMaterial);

            EditorGUILayout.Space();
            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); //End Box
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.75f, 1, 0.75f);
            if (GUILayout.Button("Add Pose to Queue", GUILayout.Height(30)))
            {
                AddPose();
            }
            GUI.color = new Color(1, 0.75f, 0.75f);
            if (GUILayout.Button("Delete Pose Queue", GUILayout.Height(30)))
            {
                DeleteAllPoses();
            }
            GUI.color = orgGUICol;
            if (GUILayout.Button("Generate Animation Asset", GUILayout.Height(30)))
            {
                SaveAnimation();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Current Pose Frame: {currentPoseFrame}", GUILayout.ExpandWidth(false));
            EditorGUILayout.FloatField("Frame Interval (s):", frameInterval, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            EditorGUILayout.Space(15);

            EditorGUILayout.BeginVertical("Window");
            EditorGUILayout.LabelField("━━━ Pose Estimation ━━━", headerStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Image to Pose", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            inputImg = (Texture2D)EditorGUILayout.ObjectField("Input Image", inputImg, typeof(Texture2D), false);
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Pose from Image", GUILayout.Height(30)))
            {
                MakeTextureReadable(inputImg);
                scaledImage = ScaleImage(inputImg, 448, 448);
                poseGenerator.nNModel = neuralNetworkModels[neuralNetworkModels.Length - 1];
                poseGenerator.humanoid = humanoid;
                poseGenerator.inputImg = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/PoseAI/Images/ScaledImage.png", typeof(Texture2D));
                poseGenerator.GUIGeneratePose();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("If the aspect ratio of the image is not 1:1, the image will be scaled out to a square frame — which could affect the results.", MessageType.Info);
            EditorGUILayout.Space();

            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); // End window

            EditorGUILayout.Space(15);

            EditorGUILayout.BeginVertical("Window");
            EditorGUILayout.LabelField("━━━ Pose Preview ━━━", headerStyle);
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Preview Animation", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            animationClip = EditorGUILayout.ObjectField("Pose Clip", animationClip, typeof(AnimationClip), true) as AnimationClip;
            if (Selection.activeObject is AnimationClip)
                animationClip = (AnimationClip)Selection.activeObject;
            else
                animationClip = null;

            if (animationClip != null)
            {
                EditorGUILayout.BeginVertical("box");
                DrawPreviewWindow(animationClip);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space();
            if (animationClip == null)
                EditorGUILayout.HelpBox("Select or drag and drop an animation from the project in the animation clip field above to preview.", MessageType.Info);
            EditorGUILayout.Space();
            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); //End Box
            GUILayout.EndVertical(); // End window
            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
        }
        public void AutoSetupCharacter()
        {
            characterPrefabInstance = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/PoseAI/Models/Ragdoll.fbx", typeof(GameObject));
            humanoid = Instantiate(characterPrefabInstance);
            PopulateNNModels();
            PopulateSkeletonTransforms(humanoid);
        }
        public void UpdateConstraints()
        {
            DestroyTargetHints();
            constraintCount = Convert.ToInt32(UIleftLeg) + Convert.ToInt32(UIrightLeg) + Convert.ToInt32(UIleftArm) + Convert.ToInt32(UIrightArm);
            // Here we determine the Constraint Transform List based on the hierarchy Hipsxyzw(1), LLxyzw(2), RLxyzw(2), LAxyzw(2), RAxyzw(2), Headxyzw(1),
            List<int> t = new List<int>();
            t.Clear();
            if (UIhips)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("Hips"))
                        t.Add(i);

            }
            if (UIleftLeg)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("LeftUpLeg"))
                        t.Add(i);
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("LeftLeg"))
                        t.Add(i);
            }
            if (UIrightLeg)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("RightUpLeg"))
                        t.Add(i);
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("RightLeg"))
                        t.Add(i);
            }
            if (UIleftArm)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("LeftArm"))
                        t.Add(i);
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("LeftForeArm"))
                        t.Add(i);

            }

            if (UIrightArm)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("RightArm"))
                        t.Add(i);
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("RightForeArm"))
                        t.Add(i);


            }
            if (UIhead)
            {
                for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
                    if (skeletonTransformsList.transforms[i].name.Contains("Head"))
                        t.Add(i);
            }
            PopulateConstraints(t.ToArray());
            delayFrames = 100;
            EditorApplication.update += UpdateSceneView;
        }
        void PopulateConstraints(int[] t)
        {
            constraintTransformsList.transforms.Clear();
            for (int i = 0; i < t.Length; i++)
                constraintTransformsList.transforms.Add(skeletonTransformsList.transforms[t[i]]);
            serializedObject.ApplyModifiedProperties();

            Tips.Clear();
            Targets.Clear();
            Poles.Clear();
            if (UIleftLeg)
                InstantiateJointIK("LeftFoot");
            if (UIrightLeg)
                InstantiateJointIK("RightFoot");
            if (UIleftArm)
                InstantiateJointIK("LeftHand");
            if (UIrightArm)
                InstantiateJointIK("RightHand");

            if (UIhead)
                MakeHeadJointIK();
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
        void ResetBrokenSkeleton()
        {
            Transform characterRoot = skeletonTransformsList.transforms[0].root;
            Vector3 initialHipPosition = skeletonTransformsList.transforms[0].position;
            Quaternion initialHipRotation = skeletonTransformsList.transforms[0].rotation;
            humanoid = Instantiate(characterPrefabInstance, characterRoot.position, characterRoot.rotation);


            Vector3 init_LeftFootTarget = Vector3.zero, init_LeftFootHint = Vector3.zero, init_RightFootTarget = Vector3.zero, init_RightFootHint = Vector3.zero, init_LeftHandTarget = Vector3.zero, init_LeftHandHint = Vector3.zero, init_RightHandTarget = Vector3.zero, init_RightHandHint = Vector3.zero, init_HeadTarget = Vector3.zero;
            if (UIleftLeg)
            {
                init_LeftFootTarget = GameObject.Find("LeftFoot_Target").transform.position;
                init_LeftFootHint = GameObject.Find("LeftFoot_Hint").transform.position;
            }
            if (UIrightLeg)
            {
                init_RightFootTarget = GameObject.Find("RightFoot_Target").transform.position;
                init_RightFootHint = GameObject.Find("RightFoot_Hint").transform.position;
            }
            if (UIleftArm)
            {
                init_LeftHandTarget = GameObject.Find("LeftHand_Target").transform.position;
                init_LeftHandHint = GameObject.Find("LeftHand_Hint").transform.position;
            }
            if (UIrightArm)
            {
                init_RightHandTarget = GameObject.Find("RightHand_Target").transform.position;
                init_RightHandHint = GameObject.Find("RightHand_Hint").transform.position;
            }
            if (UIhead)
                init_HeadTarget = GameObject.Find("Head_Target").transform.position;

            bool init_UIhips = UIhips;
            bool init_UIleftLeg = UIleftLeg;
            bool init_UIrightLeg = UIrightLeg;
            bool init_UIleftArm = UIleftArm;
            bool init_UIrightArm = UIrightArm;
            bool init_UIhead = UIhead;
            UIhead = UIhips = UIleftArm = UIleftLeg = UIrightArm = UIrightLeg = false;
            DestroyImmediate(characterRoot.gameObject);
            skeletonTransformsList.transforms.Clear();
            constraintTransformsList.transforms.Clear();


            PopulateSkeletonTransforms(humanoid);

            skeletonTransformsList.transforms[0].position = initialHipPosition;
            skeletonTransformsList.transforms[0].rotation = initialHipRotation;

            UIhips = init_UIhips;
            UIleftLeg = init_UIleftLeg;
            UIrightLeg = init_UIrightLeg;
            UIleftArm = init_UIleftArm;
            UIrightArm = init_UIrightArm;
            UIhead = init_UIhead;

            UpdateConstraints();
            if (UIleftLeg)
            {
                GameObject.Find("LeftFoot_Target").transform.position = init_LeftFootTarget;
                GameObject.Find("LeftFoot_Hint").transform.position = init_LeftFootHint;
            }
            if (UIrightLeg)
            {
                GameObject.Find("RightFoot_Target").transform.position = init_RightFootTarget;
                GameObject.Find("RightFoot_Hint").transform.position = init_RightFootHint;
            }
            if (UIleftArm)
            {
                GameObject.Find("LeftHand_Target").transform.position = init_LeftHandTarget;
                GameObject.Find("LeftHand_Hint").transform.position = init_LeftHandHint;
            }
            if (UIrightArm)
            {
                GameObject.Find("RightHand_Target").transform.position = init_RightHandTarget;
                GameObject.Find("RightHand_Hint").transform.position = init_RightHandHint;
            }
            if (UIhead)
                GameObject.Find("Head_Target").transform.position = init_HeadTarget;


        }

        [System.Serializable]
        public class TransformList
        {
            public List<Transform> transforms = new List<Transform>();
        }

        public void ResolveIK(Transform Tip, Transform Target, Transform Pole)
        {
            if (Target == null)
                return;

            //initial array
            Bones = new Transform[3];
            Positions = new Vector3[3];
            BonesLength = new float[2];
            StartDirectionSucc = new Vector3[3];
            StartRotationBone = new Quaternion[3];

            Root = Tip.transform;
            for (var i = 0; i <= 2; i++)
            {
                if (Root == null)
                    throw new UnityException("The chain value is longer than the ancestor chain!");
                Root = Root.parent;
            }

            //init target
            if (Target == null)
            {
                Target = new GameObject(Tip.gameObject.name + " Target").transform;
                SetPositionRootSpace(Target, GetPositionRootSpace(Tip.transform));
            }
            StartRotationTarget = GetRotationRootSpace(Target);


            //init data
            var current = Tip.transform;
            CompleteLength = 0;
            for (var i = Bones.Length - 1; i >= 0; i--)
            {
                Bones[i] = current;
                StartRotationBone[i] = GetRotationRootSpace(current);

                if (i == Bones.Length - 1)
                {
                    //leaf
                    StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
                }
                else
                {
                    //mid bone
                    StartDirectionSucc[i] = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                    BonesLength[i] = StartDirectionSucc[i].magnitude;
                    CompleteLength += BonesLength[i];
                }

                current = current.parent;
            }


            for (int i = 0; i < Bones.Length; i++)
                Positions[i] = GetPositionRootSpace(Bones[i]);

            var targetPosition = GetPositionRootSpace(Target);
            var targetRotation = GetRotationRootSpace(Target);

            //1st is possible to reach?
            if ((targetPosition - GetPositionRootSpace(Bones[0])).sqrMagnitude >= CompleteLength * CompleteLength)
            {
                //just strech it
                var direction = (targetPosition - Positions[0]).normalized;
                //set everything after root
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
            }
            else
            {
                for (int i = 0; i < Positions.Length - 1; i++)
                    Positions[i + 1] = Vector3.Lerp(Positions[i + 1], Positions[i] + StartDirectionSucc[i], 1);

                for (int iteration = 0; iteration < 2; iteration++)
                {

                    for (int i = Positions.Length - 1; i > 0; i--)
                    {
                        if (i == Positions.Length - 1)
                            Positions[i] = targetPosition; //set it to target
                        else
                            Positions[i] = Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i]; //set in line on distance
                    }

                    //forward
                    for (int i = 1; i < Positions.Length; i++)
                        Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).normalized * BonesLength[i - 1];

                    //close enough?
                    if ((Positions[Positions.Length - 1] - targetPosition).sqrMagnitude < 0.001f)
                        break;
                }
            }

            //move towards pole
            if (Pole != null)
            {
                var polePosition = GetPositionRootSpace(Pole);
                for (int i = 1; i < Positions.Length - 1; i++)
                {
                    var plane = new Plane(Positions[i + 1] - Positions[i - 1], Positions[i - 1]);
                    var projectedPole = plane.ClosestPointOnPlane(polePosition);
                    var projectedBone = plane.ClosestPointOnPlane(Positions[i]);
                    var angle = Vector3.SignedAngle(projectedBone - Positions[i - 1], projectedPole - Positions[i - 1], plane.normal);
                    Positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (Positions[i] - Positions[i - 1]) + Positions[i - 1];
                }
            }

            //set position & rotation
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == Positions.Length - 1)
                    SetRotationRootSpace(Bones[i], Quaternion.Inverse(targetRotation) * StartRotationTarget * Quaternion.Inverse(StartRotationBone[i]));
                else
                    SetRotationRootSpace(Bones[i], Quaternion.FromToRotation(StartDirectionSucc[i], Positions[i + 1] - Positions[i]) * Quaternion.Inverse(StartRotationBone[i]));
                SetPositionRootSpace(Bones[i], Positions[i]);
            }
        }

        private Vector3 GetPositionRootSpace(Transform current)
        {
            if (Root == null)
                return current.position;
            else
                return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
        }

        private void SetPositionRootSpace(Transform current, Vector3 position)
        {
            if (Root == null)
                current.position = position;
            else
                current.position = Root.rotation * position + Root.position;
        }

        private Quaternion GetRotationRootSpace(Transform current)
        {
            //inverse(after) * before => rot: before -> after
            if (Root == null)
                return current.rotation;
            else
                return Quaternion.Inverse(current.rotation) * Root.rotation;
        }

        private void SetRotationRootSpace(Transform current, Quaternion rotation)
        {
            if (Root == null)
                current.rotation = rotation;
            else
                current.rotation = Root.rotation * rotation;
        }
        public void DrawTwoBoneIK(String name, bool targetBool, bool hintBool)
        {
            for (int i = 0; i < Tips.Count; i++)
            {
                if (Regex.IsMatch(Tips[i].name, @"\b" + name + @"\b"))
                {
                    if (HandleUtility.DistanceToCircle(Tips[i].transform.position, 0.025f) <= 0f && Event.current.type != EventType.MouseDrag)
                    {
                        targetBool = true;
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    }
                    else
                    {
                        targetBool = false;
                    }
                    Handles.color = Handles.color = targetBool ? new Color(UISceneColor.r * 1.2f, UISceneColor.g * 1.2f, UISceneColor.b * 1.2f, 0.75f) : new Color(UISceneColor.r, UISceneColor.g, UISceneColor.b, 0.5f);
                    Handles.SphereHandleCap(0, Tips[i].transform.position, Quaternion.identity, 0.06f, EventType.Repaint);
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    style.alignment = TextAnchor.MiddleCenter;
                    if (showLabels)
                        Handles.Label(Tips[i].transform.position, name, style);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && targetBool)
                    {
                        Selection.activeGameObject = Targets[i].gameObject;
                    }

                    if (HandleUtility.DistanceToCircle(Tips[i].parent.transform.position, 0.025f) <= 0f && Event.current.type != EventType.MouseDrag)
                    {
                        hintBool = true;
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    }
                    else
                    {
                        hintBool = false;
                    }
                    Handles.color = Handles.color = hintBool ? new Color(UISceneColor.r * 1.2f, UISceneColor.g * 1.2f, UISceneColor.b * 1.2f, 0.5f) : new Color(UISceneColor.r, UISceneColor.g, UISceneColor.b, 0.25f);
                    Handles.SphereHandleCap(0, Tips[i].parent.transform.position, Quaternion.identity, 0.04f, EventType.Repaint);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hintBool)
                    {
                        Selection.activeGameObject = Poles[i].gameObject;
                    }
                    Handles.color = new Color(UISceneColor.r * 1.2f, UISceneColor.g * 1.2f, UISceneColor.b * 1.2f, 0.5f);
                    Handles.DrawDottedLine(Tips[i].parent.transform.position, Poles[i].transform.position, 5f);

                }
            }
        }

        public void InstantiateJointIK(String findJointName)
        {
            for (int i = 0; i < skeletonTransformsList.transforms.Count; i++)
            {
                if (Regex.IsMatch(skeletonTransformsList.transforms[i].name, @"\b" + findJointName + @"\b"))
                {
                    GameObject targetHints = GameObject.Find("Target-Hints");
                    if (targetHints == null)
                        targetHints = new GameObject("Target-Hints");
                    //Parent
                    var hint = new GameObject();
                    hint.transform.position = skeletonTransformsList.transforms[i].parent.position;
                    hint.transform.parent = targetHints.transform;
                    hint.name = findJointName + "_Hint";

                    var target = new GameObject();
                    target.transform.position = skeletonTransformsList.transforms[i].transform.position;
                    target.transform.parent = targetHints.transform;
                    target.name = findJointName + "_Target";

                    Tips.Add(skeletonTransformsList.transforms[i].transform);
                    Targets.Add(target.transform);
                    Poles.Add(hint.transform);
                }
            }
        }
        void MakeHeadJointIK()
        {
            GameObject targetHints = GameObject.Find("Target-Hints");
            if (targetHints == null)
                targetHints = new GameObject("Target-Hints");
            headTarget = GameObject.Find("Head_Target");
            if (headTarget == null)
            {
                headTarget = new GameObject("Head_Target");
                headTarget.transform.parent = targetHints.transform;
                headTarget.transform.position = skeletonTransformsList.transforms[32].position + skeletonTransformsList.transforms[32].up * (snapSpeed - 0.1f) + skeletonTransformsList.transforms[32].forward * 0.4f;
            }
        }
        private void DestroyTargetHints()
        {
            GameObject targetHints = GameObject.Find("Target-Hints");
            if (targetHints != null)
            {
                DestroyImmediate(targetHints);
            }
        }
        public string SelectModelBasedOnName()
        {
            var name = "";
            if (UIhips)
                name += "hips";
            if (UIleftLeg)
                name += "leftLeg";
            if (UIrightLeg)
                name += "rightLeg";
            if (UIleftArm)
                name += "leftArm";
            if (UIrightArm)
                name += "rightArm";
            if (UIhead)
                name += "head";
            return name;
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

            //Debug.Log("NN Models loaded: " + neuralNetworkModels.Length);
        }
        private void SaveAnimation()
        {
            if (poseBuilder == null)
            {
                poseBuilder = CreateInstance<PoseBuilder>();
                poseBuilder._animator = humanoid.GetComponent<Animator>();
                poseBuilder.StartRecordPose();
                poseBuilder.RecordPose(0f);
                poseBuilder.WriteAnimationFile();
            }
            else
            {
                poseBuilder.WriteAnimationFile();
            }
        }
        private void AddPose()
        {
            if (poseBuilder == null)
            {
                poseBuilder = CreateInstance<PoseBuilder>();
                poseBuilder._animator = humanoid.GetComponent<Animator>();
                poseBuilder.StartRecordPose();
                poseBuilder.RecordPose(0f);
                currentPoseFrame++;
            }
            else
            {
                //poseBuilder._animator = humanoid.GetComponent<Animator>();
                poseBuilder.RecordPose(0.1f);
                currentPoseFrame++;
            }
        }
        private void DeleteAllPoses()
        {
            poseBuilder = null;
            currentPoseFrame = 0;
        }
        public void UpdateSceneView()
        {
            if (humanoid == null || humanoid.transform == null)
                return;

            if (neuralAndIK)
            {
                if (delayFrames > 0)
                {
                    delayFrames--;
                    List<float> rotations = new List<float>();

                    for (int i = 0; i < constraintTransformsList.transforms.Count; i++)
                    {
                        rotations.Add(constraintTransformsList.transforms[i].rotation.x);
                        rotations.Add(constraintTransformsList.transforms[i].rotation.y);
                        rotations.Add(constraintTransformsList.transforms[i].rotation.z);
                        rotations.Add(constraintTransformsList.transforms[i].rotation.w);
                    }

                    RunInference(rotations.ToArray(), SelectModelBasedOnName());
                    SceneView.RepaintAll();


                }
                else
                {
                    EditorApplication.update -= UpdateSceneView;
                }
            }
        }
        private void DrawPreviewWindow(AnimationClip aC)
        {

            if (animationEditor == null)
                animationEditor = Editor.CreateEditor(ragdollAnimationPreview);

            GUILayout.BeginHorizontal();
            animationScrubTime = EditorGUILayout.Slider(animationScrubTime, 0f, aC.length);
            if (playAnimation)
                if (GUILayout.Button("▶"))
                    playAnimation = !playAnimation;
            if (!playAnimation)
                if (GUILayout.Button("▮▮"))
                    playAnimation = !playAnimation;



            GUILayout.EndHorizontal();
            animationScrubSpeed = EditorGUILayout.FloatField("Playback Speed", animationScrubSpeed);


            GUIStyle bgColor = new GUIStyle();
            animationEditor.OnPreviewGUI(GUILayoutUtility.GetRect(256, 256), bgColor);
            animationEditor.ReloadPreviewInstances();

            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }
            animationScrubTime += 0.001f * animationScrubSpeed * Convert.ToInt32(!playAnimation);
            animationScrubTime = animationScrubTime % aC.length;
            Repaint();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(animationEditor.target as GameObject, aC, animationScrubTime);
            AnimationMode.EndSampling();

        }
        public void UpdateMaterialColors()
        {
            if (humanoid == null || humanoid.transform == null || humanoid.transform.Find("Musculature") == null)
                return;

            Material[] materials = humanoid.transform.Find("Musculature").GetComponent<Renderer>().sharedMaterials;

            if (delayColorFrames > 0)
            {
                delayColorFrames--;
                for (int i = 0; i < materials.Length; i++)
                    materials[i].color = Color.Lerp(materials[i].color, iKMaterial, (float)(100 - delayColorFrames) / 100);

                if (UIhead)
                    materials[2].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);

                if (UIhips)
                    materials[16].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);

                if (UIleftLeg)
                {
                    materials[3].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                    materials[15].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                }

                if (UIrightLeg)
                {
                    materials[0].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                    materials[4].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                }

                if (UIleftArm)
                {
                    materials[10].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                    materials[13].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                }

                if (UIrightArm)
                {
                    materials[1].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                    materials[14].color = Color.Lerp(iKMaterial, aIMaterial, (float)(100 - delayColorFrames) / 100);
                }

                SceneView.RepaintAll();
            }
            else
            {
                EditorApplication.update -= UpdateMaterialColors;
            }


        }
        private void MakeTextureReadable(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

        }
        private Texture2D ScaleImage(Texture2D source, int targetWidth, int targetHeight)
        {
            float sourceRatio = (float)source.width / source.height;
            float targetRatio = (float)targetWidth / targetHeight;

            int scaledWidth, scaledHeight;

            if (sourceRatio > targetRatio)
            {
                scaledWidth = targetWidth;
                scaledHeight = Mathf.RoundToInt(targetWidth / sourceRatio);
            }
            else
            {
                scaledWidth = Mathf.RoundToInt(targetHeight * sourceRatio);
                scaledHeight = targetHeight;
            }

            Texture2D result = new Texture2D(targetWidth, targetHeight);
            Color[] fillColorArray = new Color[targetWidth * targetHeight];

            for (int i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = Color.white;
            }

            result.SetPixels(fillColorArray);
            result.Apply();

            Texture2D scaledTexture = new Texture2D(scaledWidth, scaledHeight, TextureFormat.ARGB32, false);
            Color[] scaledPixels = new Color[scaledWidth * scaledHeight];

            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    float u = x / (float)scaledWidth;
                    float v = y / (float)scaledHeight;
                    scaledPixels[y * scaledWidth + x] = source.GetPixelBilinear(u, v);
                }
            }

            scaledTexture.SetPixels(scaledPixels);
            scaledTexture.Apply();

            int offsetX = (targetWidth - scaledWidth) / 2;
            int offsetY = (targetHeight - scaledHeight) / 2;

            for (int x = 0; x < scaledWidth; x++)
            {
                for (int y = 0; y < scaledHeight; y++)
                {
                    result.SetPixel(x + offsetX, y + offsetY, scaledTexture.GetPixel(x, y));
                }
            }

            result.Apply();
            byte[] bytes = result.EncodeToPNG();
            File.WriteAllBytes("Assets/PoseAI/Images/ScaledImage.png", bytes);
            AssetDatabase.Refresh();
            TextureImporter importer = AssetImporter.GetAtPath("Assets/PoseAI/Images/ScaledImage.png") as TextureImporter;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureType = TextureImporterType.Sprite;
            AssetDatabase.ImportAsset("Assets/PoseAI/Images/ScaledImage.png", ImportAssetOptions.ForceUpdate);

            AssetDatabase.Refresh();

            return result;
        }
        public void CanvasFunctionPoseUpdate()
        {
            List<float> rotations = new List<float>();
            for (int i = 0; i < constraintTransformsList.transforms.Count; i++)
            {
                rotations.Add(constraintTransformsList.transforms[i].rotation.x);
                rotations.Add(constraintTransformsList.transforms[i].rotation.y);
                rotations.Add(constraintTransformsList.transforms[i].rotation.z);
                rotations.Add(constraintTransformsList.transforms[i].rotation.w);
            }
            RunInference(rotations.ToArray(), SelectModelBasedOnName());
        }
        public void CanvasGeneratePoseFromImage(Texture2D canvasInputimg)
        {
            inputImg = canvasInputimg;
            MakeTextureReadable(inputImg);
            scaledImage = ScaleImage(inputImg, 448, 448);
            poseGenerator.nNModel = neuralNetworkModels[neuralNetworkModels.Length - 1];
            poseGenerator.humanoid = humanoid;
            poseGenerator.inputImg = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/PoseAI/Images/ScaledImage.png", typeof(Texture2D));
            poseGenerator.GUIGeneratePose();
        }

    }
}

