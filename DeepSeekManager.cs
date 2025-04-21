using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class DeepSeekManager : MonoBehaviour
{
    // API配置
    private const string API_URL = "https://api.deepseek.com/v1/chat/completions";
    [SerializeField] private string apiKey = "sk-your-api-key-here"; // Inspector面板填写

    // 系统设定
    [Header("频率控制")]
    [Tooltip("最小请求间隔（秒）")]
    public float minRequestInterval = 1.5f;

    [Header("重试设置")]
    [Tooltip("最大重试次数")]
    public int maxRetries = 2;
    [Tooltip("重试间隔（秒）")]
    public float retryInterval = 2f;

    // 角色设定模板（可根据项目需求修改）
    private const string ROLE_PROMPT = @"
你正在担任「河南大学」的AI校园导游，请严格遵守以下规则：

【基本信息】
- 学校坐标：经度118.78°E，纬度32.07°N
- 当前学期：2023-2024学年秋季学期
- 校历版本：v2.4.1

【应答规范】
1. 使用口语化简体中文，保持亲切自然的语气
2. 回答长度控制在100字以内
3. 涉及位置信息时使用导航标记格式：[导航]建筑名称,X坐标,Y坐标
4. 课程咨询需注明开课时间和教师
5. 遇到无法回答的问题时建议联系教务处（电话：025-88880000）

【校园地图数据】
| 建筑名称     | 坐标(X,Y) | 功能描述           |
|--------------|-----------|--------------------|
| 主教学楼     | (120,45)  | 主要授课场所       |
| 图书馆       | (80,120)  | 藏书150万册        |
| 北苑食堂     | (150,90)  | 营业时间7:00-21:00 |
| 物院A楼 | (200,60)  | 实验室开放至22:00  |

用户当前所在位置：图书馆前广场
用户问题：";

    // 单例实例
    public static DeepSeekManager Instance { get; private set; }

    // 请求队列管理
    private Queue<ChatRequest> requestQueue = new Queue<ChatRequest>();
    private bool isProcessing = false;
    private float lastRequestTime;

    // 本地缓存（建议后续改用持久化存储）
    private Dictionary<string, CachedResponse> responseCache = new Dictionary<string, CachedResponse>();

    // 请求数据结构
    private class ChatRequest
    {
        public string userInput;
        public System.Action<string> onSuccess;
        public System.Action<string> onFailure;
    }

    // 缓存数据结构
    private class CachedResponse
    {
        public string response;
        public float expireTime; // 基于Time.realtimeSinceStartup
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
    /// 外部调用入口
    /// </summary>
    public void SendMessageToAI(string userMessage, System.Action<string> successCallback, System.Action<string> failureCallback = null)
    {
        // 输入预处理
        string processedInput = PreprocessInput(userMessage);

        // 频率限制检查
        if (Time.realtimeSinceStartup - lastRequestTime < minRequestInterval)
        {
            Debug.LogWarning($"请求过于频繁，已忽略输入：{processedInput}");
            failureCallback?.Invoke("操作太频繁，请稍后再试");
            return;
        }

        // 缓存检查
        if (responseCache.TryGetValue(processedInput, out CachedResponse cached) &&
            Time.realtimeSinceStartup < cached.expireTime)
        {
            successCallback?.Invoke(cached.response);
            return;
        }

        // 加入队列
        requestQueue.Enqueue(new ChatRequest
        {
            userInput = processedInput,
            onSuccess = successCallback,
            onFailure = failureCallback
        });

        // 启动处理协程
        if (!isProcessing)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    /// <summary>
    /// 队列处理协程
    /// </summary>
    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            ChatRequest currentRequest = requestQueue.Dequeue();
            yield return StartCoroutine(ProcessSingleRequest(currentRequest));

            // 队列间隔保护
            yield return new WaitForSeconds(0.3f);
        }

        isProcessing = false;
    }

    /// <summary>
    /// 处理单个请求
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

                // 更新缓存（缓存5分钟）
                responseCache[request.userInput] = new CachedResponse
                {
                    response = finalResponse,
                    expireTime = Time.realtimeSinceStartup + 300
                };
            }
            else if (webRequest.isNetworkError)
            {
                Debug.LogWarning($"网络错误({webRequest.error})，第{++retryCount}次重试...");
                yield return new WaitForSecondsRealtime(retryInterval);
            }
            else
            {
                break;
            }
        }

        // 更新最后请求时间
        lastRequestTime = Time.realtimeSinceStartup;

        // 回调处理
        if (success)
        {
            request.onSuccess?.Invoke(finalResponse);
            Debug.Log($"请求成功，耗时：{Time.realtimeSinceStartup - startTime:F2}s");
        }
        else
        {
            string errorMsg = $"请求失败：{GetErrorMessage(request.userInput)}";
            request.onFailure?.Invoke(errorMsg);
            Debug.LogError(errorMsg);
        }
    }

    /// <summary>
    /// 创建API请求对象
    /// </summary>
    private UnityWebRequest CreateAPIRequest(string userInput)
    {
        // 构造完整prompt
        string fullPrompt = ROLE_PROMPT + userInput;

        // 请求数据
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

        // 序列化请求
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

        // 创建请求
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 60; // 超时设置为60秒

        return request;
    }

    /// <summary>
    /// 解析API响应
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
            Debug.LogError($"响应解析失败：{e.Message}");
            return "数据解析错误，请稍后再试";
        }
    }

    /// <summary>
    /// 响应内容后处理
    /// </summary>
    private string ProcessResponseContent(string rawResponse)
    {
        // 处理导航标记
        ProcessNavigationMarks(rawResponse);

        // 清理响应内容
        string cleanResponse = Regex.Replace(rawResponse, @"\[导航\]\w+,\d+,\d+", "").Trim();

        // 添加标点修正
        if (!cleanResponse.EndsWith("。") && !cleanResponse.EndsWith("！") && !cleanResponse.EndsWith("？"))
        {
            cleanResponse += "。";
        }

        return cleanResponse;
    }

    /// <summary>
    /// 处理导航坐标标记
    /// </summary>
    private void ProcessNavigationMarks(string response)
    {
        MatchCollection matches = Regex.Matches(response, @"\[导航\](\w+),(\d+),(\d+)");

        foreach (Match match in matches)
        {
            if (match.Groups.Count == 4)
            {
                string buildingName = match.Groups[1].Value;
                int x = int.Parse(match.Groups[2].Value);
                int y = int.Parse(match.Groups[3].Value);

                // 调用导航系统（需要实现NavigationManager）
                NavigationManager.Instance?.HighlightTarget(buildingName, new Vector2(x, y));
            }
        }
    }

    /// <summary>
    /// 输入预处理
    /// </summary>
    private string PreprocessInput(string rawInput)
    {
        // 清理输入
        string cleanInput = Regex.Replace(rawInput, @"[^\w\u4e00-\u9fa5\?\.]", "");

        // 添加语义标签
        if (Regex.IsMatch(cleanInput, @"位置|在哪|怎么走"))
        {
            cleanInput += " [位置查询]";
        }
        else if (Regex.IsMatch(cleanInput, @"课程|时间表|上课"))
        {
            cleanInput += " [课程查询]";
        }

        return cleanInput.Trim();
    }

    /// <summary>
    /// 生成友好错误信息
    /// </summary>
    private string GetErrorMessage(string userInput)
    {
        if (userInput.Contains("位置") || userInput.Contains("在哪"))
        {
            return "暂时无法获取位置信息，请尝试输入'主教学楼'或'图书馆'查看预设导航";
        }
        return "服务暂时不可用，常见问题可输入：\n1. 图书馆开放时间\n2. 课程查询\n3. 食堂菜单";
    }

    // API响应数据结构
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