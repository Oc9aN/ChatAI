using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WebText : MonoBehaviour
{
    public Text MyTextUI;
    void Start() {
        StartCoroutine(GetText());
    }
 
    IEnumerator GetText()
    {
        string url = "https://openapi.naver.com/v1/search/news.json?query=롯데자이언츠&display=30";
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("X-Naver-Client-Id", "74U5vcZG6yYD2Yhgk3hv");
        www.SetRequestHeader("X-Naver-Client-Secret", "z71lYKmVPq");
        yield return www.SendWebRequest();
 
        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
        }
        else {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
            
            MyTextUI.text = www.downloadHandler.text;
        }
    }
}
