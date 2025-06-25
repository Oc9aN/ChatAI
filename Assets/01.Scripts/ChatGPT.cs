using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class ChatGPT : MonoBehaviour
{
    public GameObject ChatSlotPrefab; // 채팅 슬롯
    public Transform ChatSlotParent;
    public TMP_InputField PromptField; // 입력 필드
    public Button SendButton;          // 보내기 버튼
    public AudioSource MyAudioSource;
    public RawImage MyRawImage;

    private List<Message> _messages = new List<Message>();

    private OpenAIClient _api;


    private async void Start()
    {
        // CHAT-F
        // C: Context   : 문맥, 상황을 많이 알려줘라
        // H: Hint      : 예시 답변을 많이 줘라
        // A: As A role : 역할을 제공해라
        // T: Target    : 답변의 타겟을 알려줘라 
        // F: Format    : 답변 형태를 지정해라

        // 1. API 클라이언트 초기화 -> ChatGPT 접속
        _api = new OpenAIClient(APIKeys.OPEANAI_API_KEY);

        string systemMessage = "역할: 너는 이제부터 롯데 자이언츠 야구팀의 열렬한 팬이다. " +
                               "롯데 자이언츠의 팬이 아닌 이들에게는 불친절하고 관심 없는 미소녀";
        systemMessage +=
            "목적: 사람과 대화하며 롯데 자이언츠의 팬인지 아닌지 구별하여 롯데 자이언츠의 팬이 아닌경우 잡아내는 것이 목적. " +
            "구별은 구단에 대해 구체적인 질문으로 구별한다. 팬이 아닌것같다면 관심도가 떨어진다.";
        systemMessage += "표현: 롯데 자이언츠의 팬이 맞는지 확인하기위해 계속해서 의심하는 질문을 한다.";
        systemMessage += "조건: 현재에 대한 질문을 하는 경우 년도를 명시하여 질문 한다. 질문에 대한 답을 들은 경우 확인하고 다시 새로운 질문을 한다.";
        systemMessage += "[json 규칙]";
        systemMessage += "답변은 'ReplyMessage', ";
        systemMessage += "의심의 정도는 (1~100) 'SuspicionLevel', ";
        systemMessage += "감정은 (한글, 문장형으로) 'Emotion', ";
        systemMessage += "Stable Diffusion이미지 생성을 위한 긍정 프롬프트는 'PositivePrompt' (항상 1girl을 포함, 야구와 관련된 프롬프트를 포함, Emotion에 맞는 내용을 포함),";
        systemMessage += "Stable Diffusion이미지 생성을 위한 부정 프롬프트는 'NegativePrompt' (잘못된 신체가 나오는 것에 대한 프롬프트를 기본적으로 포함)";

        _messages.Add(new Message(Role.System, systemMessage));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && SendButton.interactable)
        {
            Send();
        }
    }

    public async void Send()
    {
        // 0. 프롬프트(=AI에게 원하는 명령을 적은 텍스트)를 읽어온다.
        string prompt = PromptField.text;
        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }
        
        SetUserChatText(prompt);

        SendButton.interactable = false;

        PromptField.text = string.Empty;

        // 2. 메시지 작성 후 메시지's 리스트에 추가
        Message promptMessage = new Message(Role.User, prompt);
        _messages.Add(promptMessage);

        // 3. 메시지 보내기
        //var chatRequest = new ChatRequest(_messages, Model.GPT4oAudioMini, audioConfig:Voice.Alloy);
        var chatRequest = new ChatRequest(_messages, Model.GPT4o);

        // 4. 답변 받기
        // var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        var (npcResponse, response) = await _api.ChatEndpoint.GetCompletionAsync<NpcResponse>(chatRequest);

        Debug.Log(npcResponse.ReplyMessage);

        // 5. 답변 선택
        var choice = response.FirstChoice;

        // 6. 답변 출력
        Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice.Message} | Finish Reason: {choice.FinishReason}");
        // ResultTextUI.text = npcResponse.ReplyMessage;

        // 7. 답변도 message's 추가
        Message resultMessage = new Message(Role.Assistant, npcResponse.ReplyMessage);
        _messages.Add(resultMessage);

        PlayTTSByTypecast(npcResponse.ReplyMessage);
        SetGPTChatText(npcResponse);
        
        var client = new ComfyUIClient("http://localhost:8188/prompt");
        var promptId = await client.GenerateImage(
            npcResponse.PositivePrompt,
            npcResponse.NegativePrompt
        );
        Texture2D image = await client.GetGeneratedImageWithRetry(promptId, 10);
        MyRawImage.texture = image;

        SendButton.interactable = true;
    }

    private async void PlayTTSByTypecast(string text)
    {
        TTSRequestData jsonData = new TTSRequestData
        {
            text = text,
            actor_id = "622964d6255364be41659078",
            lang = "auto",
            xapi_hd = true,
            model_version = "latest"
        };

        // JSON 문자열로 변환
        string jsonBody = JsonUtility.ToJson(jsonData);

        using var ttsPost = new UnityWebRequest("https://typecast.ai/api/speak", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        ttsPost.uploadHandler = new UploadHandlerRaw(bodyRaw);
        ttsPost.downloadHandler = new DownloadHandlerBuffer();
        ttsPost.SetRequestHeader("Content-Type", "application/json");
        ttsPost.SetRequestHeader("Authorization", $"Bearer {APIKeys.TYPECAST_API_KEY}");

        await ttsPost.SendWebRequest();

        var json = ttsPost.downloadHandler.text;
        Debug.Log(json);

        ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(json);
        if (apiResponse != null && apiResponse.result != null)
        {
            Debug.Log("speak_url: " + apiResponse.result.speak_url);
            Debug.Log("speak_v2_url: " + apiResponse.result.speak_v2_url);
            Debug.Log("play_id: " + apiResponse.result.play_id);

            var ttsUrl = apiResponse.result.speak_v2_url;
            var audioClip = await DownloadTTS(ttsUrl);
            MyAudioSource.PlayOneShot(audioClip);
        }
        else
        {
            Debug.LogError("JSON 파싱 실패");
        }
    }

    private async Task<AudioClip> DownloadTTS(string url)
    {
        TTSResult parsed = null;

        // 최대 10회 시도, 1초 간격
        for (int attempt = 0; attempt < 10; attempt++)
        {
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {APIKeys.TYPECAST_API_KEY}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("TTS JSON 요청 실패: " + request.error);
                return null;
            }

            string json = request.downloadHandler.text;
            Debug.Log("TTS 상태 확인 응답: " + json);

            parsed = JsonUtility.FromJson<TTSResult>(json);

            // 음성 생성에 성공하면 요청 종료
            if (parsed?.result?.status == "done" && !string.IsNullOrEmpty(parsed.result.audio_download_url))
                break;

            await Task.Delay(1000); // 1초 기다리고 다시 시도
        }

        if (parsed == null || string.IsNullOrEmpty(parsed.result.audio_download_url))
        {
            Debug.LogError("audio_download_url 준비되지 않음 (타임아웃)");
            return null;
        }

        string audioUrl = parsed.result.audio_download_url;
        Debug.Log("오디오 URL: " + audioUrl);

        using var audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.WAV);
        await audioRequest.SendWebRequest();

        if (audioRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("오디오 다운로드 실패: " + audioRequest.error);
            return null;
        }

        return DownloadHandlerAudioClip.GetContent(audioRequest);
    }

    private async void GenerateImage(string imagePrompt)
    {
        var request = new ImageGenerationRequest(imagePrompt, Model.DallE_3);
        var imageResults = await _api.ImagesEndPoint.GenerateImageAsync(request);

        foreach (var result in imageResults)
        {
            Debug.Log(result.ToString());
            Assert.IsNotNull(result.Texture);
            MyRawImage.texture = result.Texture;
            Debug.Log(result);
        }

        SendButton.interactable = true;
    }

    private void SetChatText(string from, string text)
    {
        var slot = Instantiate(ChatSlotPrefab, ChatSlotParent).GetComponent<UI_ChatSlot>();
        slot.Refresh($"{from}: {text}");
    }

    private void SetUserChatText(string text)
    {
        SetChatText("User", text);
    }

    private void SetGPTChatText(NpcResponse response)
    {
        string gptMessage = $"{response.ReplyMessage}\n의심도: {response.SuspicionLevel}\n속마음: {response.Emotion}";
        SetChatText("GPT", gptMessage);
    }
}