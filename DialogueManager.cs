using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
public class DialogueManager : MonoBehaviour
{
    [Header("AI消息滚动区域")]
    [SerializeField] private TMP_Text aimessageText; // 绑定到AIMessageText
    [SerializeField] private ScrollRect aiScrollRect; // 绑定到AIMessageScrollView的Scroll Rect
    [Header("输入框")]
    // 新增发送按钮点击方法
    [Header("消息滚动区")]
    [SerializeField] private TMP_Text messageText;  // 指向ContentText
    [SerializeField] private ScrollRect messageScroll;
    public static DialogueManager Instance; // 单例实例
    [Header("滚动视图")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_InputField inputField;
    public void AppendMessage(string speaker, string content)
    {
        string formatted = $"<color=#88FF88>{speaker}:</color> {content}\n\n";
        // 合并所有文本更新
        messageText.text += formatted;
        dialogueText.text += formatted;
        aimessageText.text += formatted;
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        aiScrollRect.verticalNormalizedPosition = 0;
        messageScroll.verticalNormalizedPosition = 0;
        scrollRect.verticalNormalizedPosition = 0;

    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("管理器初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartDialogue()
    {
        UIManager.Instance.ToggleDialogueUI(true);
        inputField.ActivateInputField();
    }

    // 其他对话方法...
    // Start is called before the first frame update
    void Start()
    {
        // 初始化输入框回调
        inputField.onSubmit.AddListener(delegate { OnSendButtonClick(); });
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnSendButtonClick()
    {
        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // 显示用户消息
        AppendMessage("你", message);
        inputField.text = "";

        // 禁用输入框直到收到响应
        inputField.interactable = false;

        // 调用修改后的API接口
        DeepSeekManager.Instance.SendMessageToAI(
            message,
            // 成功回调
            (response) =>
            {
                AppendMessage("导游", response);
                inputField.interactable = true;
                inputField.ActivateInputField();
            },
            // 失败回调
            (error) =>
            {
                AppendMessage("系统", $"<color=#FF8888>{error}</color>");
                inputField.interactable = true;
                inputField.ActivateInputField();
            }
        );

    }
}
