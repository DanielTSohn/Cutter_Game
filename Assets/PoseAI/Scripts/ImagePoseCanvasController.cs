using UnityEngine;
using UnityEngine.UI;

namespace PoseAI
{
    public class ImagePoseCanvasController : MonoBehaviour
    {
        PoseAI poseAI;
        public Button poseButton1, poseButton2, poseButton3;

        void Start()
        {
            poseAI = ScriptableObject.CreateInstance<PoseAI>();
            poseAI.AutoSetupCharacter();
            poseButton1.onClick.AddListener(() => PoseEstimation(poseButton1.GetComponent<RawImage>()));
            poseButton2.onClick.AddListener(() => PoseEstimation(poseButton2.GetComponent<RawImage>()));
            poseButton3.onClick.AddListener(() => PoseEstimation(poseButton3.GetComponent<RawImage>()));

        }
        void OnDisable()
        {
            if (poseAI != null)
            {
                DestroyImmediate(poseAI);
                poseAI = null;
            }

        }
        void PoseEstimation(RawImage rawImage)
        {
            Texture image = rawImage.mainTexture;
            Texture2D imageTex2D = (Texture2D)image;
            poseAI.CanvasGeneratePoseFromImage(imageTex2D);
        }
    }
}
