[System.Serializable]
public class ApiResponse
{
    public Result result;
}

[System.Serializable]
public class Result
{
    public string speak_url;
    public string speak_v2_url;
    public string play_id;
}

[System.Serializable]
public class TTSRequestData
{
    public string text;
    public string actor_id;
    public string lang;
    public bool xapi_hd;
    public string model_version;
}

[System.Serializable]
public class TTSResult
{
    public TTSResultData result;
}

[System.Serializable]
public class TTSResultData
{
    public string audio_download_url;
    public string status;
}