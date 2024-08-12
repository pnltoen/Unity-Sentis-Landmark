using UnityEngine;
using Unity.Sentis;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;
using Lays = Unity.Sentis.Layers;
using System;
using TMPro;

/*
 *                   Face Landmarks Inference
 *                   ========================
 *                   
 * Basic inference script for mediapose face landmarks
 * 
 * Put this script on the Main Camera
 * Put face_landmarks.sentis in the Assets/StreamingAssets folder
 * Create a RawImage of in the scene
 * Put a link to that image in previewUI
 * Put a video in Assets/StreamingAssets folder and put the name of it int videoName
 * Or put a test image in inputImage
 * Set inputType to appropriate input
 */


public class RunFaceLandmark : MonoBehaviour
{
    //Drag a link to a raw image here:
    public RawImage previewUI = null;

    public string videoName = "chatting.mp4";

    // Image to put into neural network
    public Texture2D inputImage;

    public InputType inputType = InputType.Video;

    //Resolution of displayed image
    Vector2Int resolution = new Vector2Int(640, 640);
    WebCamTexture webcam;
    VideoPlayer video;

    const BackendType backend = BackendType.GPUCompute;

    RenderTexture targetTexture;
    public enum InputType { Image, Video, Webcam };

    const int markerWidth = 5;

    //Holds array of colors to draw landmarks
    Color32[] markerPixels;

    IWorker worker;

    //Size of input image to neural network (196)
    const int size = 192;


    Model model;

    //webcam device name:
    const string deviceName = "";

    bool closing = false;

    Texture2D canvasTexture;
    Action<Texture> RunInference;

    public GameObject tmpText;
    Vector3 trans_tempText;

    void Start()
    {
        //(Note: if using a webcam on mobile get permissions here first)

        SetupTextures();
        SetupMarkers();
        SetupInput();
        SetupModel();
        SetupEngine();
        RunInference = HandleRunInference;
    }

    void SetupModel()
    {
        model = ModelLoader.Load(Application.streamingAssetsPath + "/face_landmark.sentis");
    }
    public void SetupEngine()
    {
        worker = WorkerFactory.CreateWorker(backend, model);
    }

    void SetupTextures()
    {
        //To display the get and display the original image:
        targetTexture = new RenderTexture(resolution.x, resolution.y, 0);

        //Used for drawing the markers:
        canvasTexture = new Texture2D(targetTexture.width, targetTexture.height);

        //previewUI.texture = targetTexture;
        previewUI.texture = canvasTexture;
    }

    void SetupMarkers()
    {
        markerPixels = new Color32[markerWidth * markerWidth];
        for (int n = 0; n < markerWidth * markerWidth; n++)
        {
            markerPixels[n] = Color.white;
        }
        int center = markerWidth / 2;
        markerPixels[center * markerWidth + center] = Color.black;
    }

    void SetupInput()
    {
        switch (inputType)
        {
            case InputType.Webcam:
                {
                    webcam = new WebCamTexture(deviceName, resolution.x, resolution.y);
                    webcam.requestedFPS = 30;
                    webcam.Play();
                    break;
                }
            case InputType.Video:
                {
                    video = gameObject.AddComponent<VideoPlayer>();//new VideoPlayer();
                    video.renderMode = VideoRenderMode.APIOnly;
                    video.source = VideoSource.Url;
                    video.url = Application.streamingAssetsPath + "/"+videoName;
                    video.isLooping = true;
                    video.Play();
                    break;
                }
            default:
                {
                    Graphics.Blit(inputImage, targetTexture);
                }
                break;
        }
    }

    void Update()
    {
        GetImageFromSource();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            closing = true;
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            previewUI.enabled = !previewUI.enabled;
        }
    }

    void GetImageFromSource()
    {
        if (inputType == InputType.Webcam)
        {
            // Format video input
            if (!webcam.didUpdateThisFrame) return;

            var aspect1 = (float)webcam.width / webcam.height;
            var aspect2 = (float)resolution.x / resolution.y;
            var gap = aspect2 / aspect1;

            var vflip = webcam.videoVerticallyMirrored;
            var scale = new Vector2(gap, vflip ? -1 : 1);
            var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

            Graphics.Blit(webcam, targetTexture, scale, offset);
            RunInference?.Invoke(targetTexture);
        }
        if (inputType == InputType.Video)
        {
            var aspect1 = (float)video.width / video.height;
            var aspect2 = (float)resolution.x / resolution.y;
            var gap = aspect2 / aspect1;

            var vflip = false;
            var scale = new Vector2(gap, vflip ? -1 : 1);
            var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);
            Graphics.Blit(video.texture, targetTexture, scale, offset);
        }
        if (inputType == InputType.Image)
        {
            Graphics.Blit(inputImage, targetTexture);
        }
    }

    void LateUpdate()
    {
        if (!closing)
        {
            //RunInference(targetTexture);
        }
    }

    void HandleRunInference(Texture source)
    {
        var transform = new TextureTransform();
        transform.SetDimensions(size, size, 3);
        transform.SetTensorLayout(0, 3, 1, 2);
        using var image = TextureConverter.ToTensor(source, transform);

        // The image has pixels in the range [0..1]
        worker.Execute(image);

        using var landmarks = worker.PeekOutput("conv2d_21") as TensorFloat;

        float scaleX = canvasTexture.width * 1f / size;
        float scaleY = canvasTexture.height * 1f / size;

        var downloadLandmarksCopied = landmarks.ReadbackAndClone();
        DrawLandmarks(downloadLandmarksCopied, scaleX, scaleY);
    }

    void DrawLandmarks(TensorFloat landmarks, float scaleX, float scaleY)
    {
        int numLandmarks = landmarks.shape[3] / 3; // 468 face landmarks

        /*// Clear canvasTexture
        Color32 clearColor = new Color32(0, 0, 0, 0); // Transparent
        Color32[] clearPixels = new Color32[canvasTexture.width * canvasTexture.height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = clearColor;
        canvasTexture.SetPixels32(clearPixels);*/

        for (int n = 0; n < numLandmarks; n++)
        {
            float displayWidth = previewUI.rectTransform.rect.width;
            float displayHeight = previewUI.rectTransform.rect.height;

            int px = (int)(landmarks[0, 0, 0, n * 3 + 0] * scaleX) - (markerWidth - 1) / 2;
            int py = (int)(landmarks[0, 0, 0, n * 3 + 1] * scaleY) - (markerWidth - 1) / 2;
            //int destX = Mathf.Clamp(px, 0, canvasTexture.width - 1 - markerWidth);
            //int destY = Mathf.Clamp(canvasTexture.height - 1 - py, 0, canvasTexture.height - 1 - markerWidth);
            //canvasTexture.SetPixels32(destX, destY, markerWidth, markerWidth, markerPixels);

            TextMeshProUGUI textMeshProUGUI = tmpText.GetComponent<TextMeshProUGUI>();
            float uguiX = (px - 0.5f) * displayHeight;
            float uguiY = (py - 0.5f) * displayWidth;
            trans_tempText = new Vector3(uguiX, uguiY, 0);
            GameObject landmakrs = Instantiate(tmpText, trans_tempText, Quaternion.identity);
            textMeshProUGUI.text = n.ToString();
        }
        canvasTexture.Apply();
    }

    void CleanUp()
    {
        closing = true;
        if (webcam) Destroy(webcam);
        if (video) Destroy(video);
        RenderTexture.active = null;
        targetTexture.Release();
        worker?.Dispose();
        worker = null;
    }

    void OnDestroy()
    {
        CleanUp();
    }

}

