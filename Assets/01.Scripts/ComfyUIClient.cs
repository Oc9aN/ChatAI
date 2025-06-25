using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ComfyUIClient
{
    private readonly string endpoint;

    public ComfyUIClient(string generateEndpoint)
    {
        this.endpoint = generateEndpoint;
    }

    // TODO : 특정 워크플로우 JSON을 받아 프롬프트만 수정하게 변경
    public async Task<string> GenerateImage(string positivePrompt, string negativePrompt)
    {
        var prompt = new Dictionary<string, object>();

        prompt["1"] = new
        {
            class_type = "CheckpointLoaderSimple",
            inputs = new Dictionary<string, object>
            {
                { "ckpt_name", "meinamix_v12Final.safetensors" }
            }
        };

        prompt["2"] = new
        {
            class_type = "EmptyLatentImage",
            inputs = new Dictionary<string, object>
            {
                { "width", 512 },
                { "height", 1024 },
                { "batch_size", 1 }
            }
        };

        prompt["3"] = new
        {
            class_type = "CLIPTextEncode",
            inputs = new Dictionary<string, object>
            {
                { "text", positivePrompt },
                { "clip", new object[] { "1", 1 } }
            }
        };

        prompt["4"] = new
        {
            class_type = "CLIPTextEncode",
            inputs = new Dictionary<string, object>
            {
                { "text", negativePrompt },
                { "clip", new object[] { "1", 1 } }
            }
        };

        prompt["5"] = new
        {
            class_type = "KSampler",
            inputs = new Dictionary<string, object>
            {
                { "model", new object[] { "1", 0 } },
                { "positive", new object[] { "3", 0 } },
                { "negative", new object[] { "4", 0 } },
                { "latent_image", new object[] { "2", 0 } },
                { "seed", 123456 },
                { "steps", 21 },
                { "cfg", 5.0 },
                { "sampler_name", "dpmpp_2m" },
                { "scheduler", "karras" },
                { "denoise", 1.0 }
            }
        };

        prompt["6"] = new
        {
            class_type = "VAEDecode",
            inputs = new Dictionary<string, object>
            {
                { "vae", new object[] { "1", 2 } },
                { "samples", new object[] { "5", 0 } }
            }
        };

        prompt["7"] = new
        {
            class_type = "SaveImage",
            inputs = new Dictionary<string, object>
            {
                { "images", new object[] { "6", 0 } },
                { "filename_prefix", "Unity" }
            }
        };

        var requestBody = new Dictionary<string, object>
        {
            { "prompt", prompt },
            { "outputs", new List<object> { new object[] { "7", 0 } } }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(endpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(jsonBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("ComfyUI 요청 실패: " + req.error);
            return null;
        }

        var responseJson = req.downloadHandler.text;
        Debug.Log("ComfyUI 응답: " + responseJson);

        // 여기서 prompt_id만 추출
        try
        {
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson);
            if (responseData.ContainsKey("prompt_id"))
            {
                return responseData["prompt_id"].ToString();
            }
            else
            {
                Debug.LogError("prompt_id가 응답에 없음");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("응답 파싱 실패: " + e.Message);
            return null;
        }
    }

    public async Task<Texture2D> GetGeneratedImageWithRetry(string promptId, int maxRetry = 5)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            var tex = await GetGeneratedImage(promptId);
            if (tex != null)
            {
                return tex;
            }

            Debug.Log($"히스토리 대기 중... {i + 1}/{maxRetry}");
            await Task.Delay(1000);
        }

        Debug.LogError("히스토리 타임아웃");
        return null;
    }

    public async Task<Texture2D> GetGeneratedImage(string promptId)
    {
        string historyUrl = $"http://localhost:8188/history/{promptId}";

        using var historyRequest = UnityWebRequest.Get(historyUrl);
        await historyRequest.SendWebRequest();

        if (historyRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("히스토리 요청 실패: " + historyRequest.error);
            return null;
        }

        var historyJson = historyRequest.downloadHandler.text;
        Debug.Log("히스토리 응답: " + historyJson);

        // 1단계: 전체 딕셔너리 파싱
        var fullHistory = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(historyJson);

        if (!fullHistory.ContainsKey(promptId))
        {
            Debug.LogWarning($"히스토리에 prompt_id {promptId} 없음");
            return null;
        }

        var promptData = fullHistory[promptId];

        // 2단계: outputs["7"]["images"]
        var outputs = promptData["outputs"] as JObject;
        if (outputs == null || outputs.Properties().Count() == 0)
        {
            Debug.LogWarning("outputs가 비어 있음");
            return null;
        }

        var node = outputs.Properties().FirstOrDefault(); // ex: "7"
        if (node == null)
        {
            Debug.LogWarning("outputs에 노드 없음");
            return null;
        }

        var images = node.Value["images"].ToObject<List<Dictionary<string, string>>>();
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning("이미지 없음");
            return null;
        }

        string filename = images[0]["filename"];
        string subfolder = images[0]["subfolder"];
        string viewUrl = $"http://localhost:8188/view?filename={filename}&subfolder={subfolder}&type=output";

        using var viewRequest = UnityWebRequestTexture.GetTexture(viewUrl);
        await viewRequest.SendWebRequest();

        if (viewRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("이미지 불러오기 실패: " + viewRequest.error);
            return null;
        }

        return DownloadHandlerTexture.GetContent(viewRequest);
    }

}