using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WebImage : MonoBehaviour
{
    public RawImage MyImage;
    private void Start()
    {
        StartCoroutine(GetTexture());
    }

    private IEnumerator GetTexture()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture("https://dh.aks.ac.kr/Edu/wiki/images/9/91/%ED%8C%A8%ED%8A%B8%EC%99%80.jpg");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            MyImage.texture = myTexture;
            MyImage.SetNativeSize();
        }
    }
}