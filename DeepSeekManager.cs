using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class DeepSeekManager : MonoBehaviour
{
    // API����
    private const string API_URL = "https://api.deepseek.com/v1/chat/completions";
    [SerializeField] private string apiKey = "sk-your-api-key-here"; // Inspector�����д

    // ϵͳ�趨
    [Header("Ƶ�ʿ���")]
    [Tooltip("��С���������룩")]
    public float minRequestInterval = 1.5f;

    [Header("��������")]
    [Tooltip("������Դ���")]
    public int maxRetries = 2;
    [Tooltip("���Լ�����룩")]
    public float retryInterval = 2f;

    // ��ɫ�趨ģ�壨�ɸ�����Ŀ�����޸ģ�
    private const string ROLE_PROMPT = @"
�����ڵ��Ρ����ϴ�ѧ����AIУ԰���Σ����ϸ��������¹���

��������Ϣ��
- ѧУ���꣺����118.78��E��γ��32.07��N
- ��ǰѧ�ڣ�2023-2024ѧ���＾ѧ��
- У���汾��v2.4.1

��Ӧ��淶��
1. ʹ�ÿ��ﻯ�������ģ�����������Ȼ������
2. �ش𳤶ȿ�����100������
3. �漰λ����Ϣʱʹ�õ�����Ǹ�ʽ��[����]��������,X����,Y����
4. �γ���ѯ��ע������ʱ��ͽ�ʦ
5. �����޷��ش������ʱ������ϵ���񴦣��绰��025-88880000��

��У԰��ͼ���ݡ�
| ��������     | ����(X,Y) | ��������           |
|--------------|-----------|--------------------|
| ����ѧ¥     | (120,45)  | ��Ҫ�ڿγ���       |
| ͼ���       | (80,120)  | ����150���        |
| ��Էʳ��     | (150,90)  | Ӫҵʱ��7:00-21:00 |
| ��ԺA¥ | (200,60)  | ʵ���ҿ�����22:00  |

�û���ǰ����λ�ã�ͼ���ǰ�㳡
�û����⣺";

    // ����ʵ��
    public static DeepSeekManager Instance { get; private set; }

    // ������й���
    private Queue<ChatRequest> requestQueue = new Queue<ChatRequest>();
    private bool isProcessing = false;
    private float lastRequestTime;

    // ���ػ��棨����������ó־û��洢��
    private Dictionary<string, CachedResponse> responseCache = new Dictionary<string, CachedResponse>();

    // �������ݽṹ
    private class ChatRequest
    {
        public string userInput;
        public System.Action<string> onSuccess;
        public System.Action<string> onFailure;
    }

    // �������ݽṹ
    private class CachedResponse
    {
        public string response;
        public float expireTime; // ����Time.realtimeSinceStartup
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// �ⲿ�������
    /// </summary>
    public void SendMessageToAI(string userMessage, System.Action<string> successCallback, System.Action<string> failureCallback = null)
    {
        // ����Ԥ����
        string processedInput = PreprocessInput(userMessage);

        // Ƶ�����Ƽ��
        if (Time.realtimeSinceStartup - lastRequestTime < minRequestInterval)
        {
            Debug.LogWarning($"�������Ƶ�����Ѻ������룺{processedInput}");
            failureCallback?.Invoke("����̫Ƶ�������Ժ�����");
            return;
        }

        // ������
        if (responseCache.TryGetValue(processedInput, out CachedResponse cached) &&
            Time.realtimeSinceStartup < cached.expireTime)
        {
            successCallback?.Invoke(cached.response);
            return;
        }

        // �������
        requestQueue.Enqueue(new ChatRequest
        {
            userInput = processedInput,
            onSuccess = successCallback,
            onFailure = failureCallback
        });

        // ��������Э��
        if (!isProcessing)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    /// <summary>
    /// ���д���Э��
    /// </summary>
    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            ChatRequest currentRequest = requestQueue.Dequeue();
            yield return StartCoroutine(ProcessSingleRequest(currentRequest));

            // ���м������
            yield return new WaitForSeconds(0.3f);
        }

        isProcessing = false;
    }

    /// <summary>
    /// ����������
    /// </summary>
    private IEnumerator ProcessSingleRequest(ChatRequest request)
    {
        float startTime = Time.realtimeSinceStartup;
        int retryCount = 0;
        bool success = false;
        string finalResponse = null;

        while (retryCount <= maxRetries && !success)
        {
            using UnityWebRequest webRequest = CreateAPIRequest(request.userInput);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                success = true;
                var response = ParseAPIResponse(webRequest.downloadHandler.text);
                finalResponse = ProcessResponseContent(response);

                // ���»��棨����5���ӣ�
                responseCache[request.userInput] = new CachedResponse
                {
                    response = finalResponse,
                    expireTime = Time.realtimeSinceStartup + 300
                };
            }
            else if (webRequest.isNetworkError)
            {
                Debug.LogWarning($"�������({webRequest.error})����{++retryCount}������...");
                yield return new WaitForSecondsRealtime(retryInterval);
            }
            else
            {
                break;
            }
        }

        // �����������ʱ��
        lastRequestTime = Time.realtimeSinceStartup;

        // �ص�����
        if (success)
        {
            request.onSuccess?.Invoke(finalResponse);
            Debug.Log($"����ɹ�����ʱ��{Time.realtimeSinceStartup - startTime:F2}s");
        }
        else
        {
            string errorMsg = $"����ʧ�ܣ�{GetErrorMessage(request.userInput)}";
            request.onFailure?.Invoke(errorMsg);
            Debug.LogError(errorMsg);
        }
    }

    /// <summary>
    /// ����API�������
    /// </summary>
    private UnityWebRequest CreateAPIRequest(string userInput)
    {
        // ��������prompt
        string fullPrompt = ROLE_PROMPT + userInput;

        // ��������
        var requestData = new
        {
            model = "deepseek-chat",
            messages = new object[] {
                new { role = "system", content = ROLE_PROMPT },
                new { role = "user", content = userInput }
            },
            temperature = 0.7,
            max_tokens = 150,
            top_p = 0.9
        };

        // ���л�����
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

        // ��������
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 60; // ��ʱ����Ϊ60��

        return request;
    }

    /// <summary>
    /// ����API��Ӧ
    /// </summary>
    private string ParseAPIResponse(string jsonResponse)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<DeepSeekResponse>(jsonResponse);
            return response.choices[0].message.content;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"��Ӧ����ʧ�ܣ�{e.Message}");
            return "���ݽ����������Ժ�����";
        }
    }

    /// <summary>
    /// ��Ӧ���ݺ���
    /// </summary>
    private string ProcessResponseContent(string rawResponse)
    {
        // ���������
        ProcessNavigationMarks(rawResponse);

        // ������Ӧ����
        string cleanResponse = Regex.Replace(rawResponse, @"\[����\]\w+,\d+,\d+", "").Trim();

        // ��ӱ������
        if (!cleanResponse.EndsWith("��") && !cleanResponse.EndsWith("��") && !cleanResponse.EndsWith("��"))
        {
            cleanResponse += "��";
        }

        return cleanResponse;
    }

    /// <summary>
    /// ������������
    /// </summary>
    private void ProcessNavigationMarks(string response)
    {
        MatchCollection matches = Regex.Matches(response, @"\[����\](\w+),(\d+),(\d+)");

        foreach (Match match in matches)
        {
            if (match.Groups.Count == 4)
            {
                string buildingName = match.Groups[1].Value;
                int x = int.Parse(match.Groups[2].Value);
                int y = int.Parse(match.Groups[3].Value);

                // ���õ���ϵͳ����Ҫʵ��NavigationManager��
                NavigationManager.Instance?.HighlightTarget(buildingName, new Vector2(x, y));
            }
        }
    }

    /// <summary>
    /// ����Ԥ����
    /// </summary>
    private string PreprocessInput(string rawInput)
    {
        // ��������
        string cleanInput = Regex.Replace(rawInput, @"[^\w\u4e00-\u9fa5\?\.]", "");

        // ��������ǩ
        if (Regex.IsMatch(cleanInput, @"λ��|����|��ô��"))
        {
            cleanInput += " [λ�ò�ѯ]";
        }
        else if (Regex.IsMatch(cleanInput, @"�γ�|ʱ���|�Ͽ�"))
        {
            cleanInput += " [�γ̲�ѯ]";
        }

        return cleanInput.Trim();
    }

    /// <summary>
    /// �����Ѻô�����Ϣ
    /// </summary>
    private string GetErrorMessage(string userInput)
    {
        if (userInput.Contains("λ��") || userInput.Contains("����"))
        {
            return "��ʱ�޷���ȡλ����Ϣ���볢������'����ѧ¥'��'ͼ���'�鿴Ԥ�赼��";
        }
        return "������ʱ�����ã�������������룺\n1. ͼ��ݿ���ʱ��\n2. �γ̲�ѯ\n3. ʳ�ò˵�";
    }

    // API��Ӧ���ݽṹ
    [System.Serializable]
    private class DeepSeekResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
    }

    [System.Serializable]
    private class Message
    {
        public string content;
    }
}