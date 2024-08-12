using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Landmarks_ui : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject tmpText;
    Vector3 trans_tempText;
    public RawImage previewUI;
    float px = 0;
    float py = 0;

    GameObject[] spawnedLandmarks = new GameObject[10];

    void Start()
    {
        for (int n = 0; n < 10; n++)
        {
            float displayWidth = previewUI.rectTransform.rect.width;
            float displayHeight = previewUI.rectTransform.rect.height;

            float uguiX = (px - 0.5f) * displayHeight;
            //float uguiY = (py - 0.5f) * displayWidth;
            float uguiY = 0f;

            TextMeshProUGUI textMeshProUGUI = tmpText.GetComponent<TextMeshProUGUI>();
            RectTransform rectTransform = tmpText.GetComponent<RectTransform>();

            GameObject landmakrs = Instantiate(tmpText, transform);
            rectTransform.anchoredPosition = new Vector2(uguiX, uguiY);
            textMeshProUGUI.text = n.ToString();

            //trans_tempText = new Vector3(uguiX, uguiY, 0);
            //Debug.Log(trans_tempText);
        
            px += 0.1f;
            //py += 0.1f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*for (int n = 0; n < spawnedLandmarks.Length; n++)
        {
            if (spawnedLandmarks[n] != null)
            {
                Destroy(spawnedLandmarks[n]);
            }
        }*/
    }
}
