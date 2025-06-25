using Newtonsoft.Json;

public class NpcResponse
{
    [JsonProperty("ReplyMessage")]
    public string ReplyMessage { get; set; }
    
    [JsonProperty("SuspicionLevel")]
    public string SuspicionLevel { get; set; }
    
    [JsonProperty("Emotion")]
    public string Emotion { get; set; }
    
    [JsonProperty("PositivePrompt")]
    public string PositivePrompt { get; set; }
    
    [JsonProperty("NegativePrompt")]
    public string NegativePrompt { get; set; }
}
