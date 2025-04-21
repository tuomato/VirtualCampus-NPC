using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
public class DialogueManager : MonoBehaviour
{
    [Header("AI��Ϣ��������")]
    [SerializeField] private TMP_Text aimessageText; // �󶨵�AIMessageText
    [SerializeField] private ScrollRect aiScrollRect; // �󶨵�AIMessageScrollView��Scroll Rect
    [Header("�����")]
    // �������Ͱ�ť�������
    [Header("��Ϣ������")]
    [SerializeField] private TMP_Text messageText;  // ָ��ContentText
    [SerializeField] private ScrollRect messageScroll;
    public static DialogueManager Instance; // ����ʵ��
    [Header("������ͼ")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_InputField inputField;
    public void AppendMessage(string speaker, string content)
    {
        string formatted = $"<color=#88FF88>{speaker}:</color> {content}\n\n";
        // �ϲ������ı�����
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
            Debug.Log("��������ʼ�����");
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

    // �����Ի�����...
    // Start is called before the first frame update
    void Start()
    {
        // ��ʼ�������ص�
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

        // ��ʾ�û���Ϣ
        AppendMessage("��", message);
        inputField.text = "";

        // ���������ֱ���յ���Ӧ
        inputField.interactable = false;

        // �����޸ĺ��API�ӿ�
        DeepSeekManager.Instance.SendMessageToAI(
            message,
            // �ɹ��ص�
            (response) =>
            {
                AppendMessage("����", response);
                inputField.interactable = true;
                inputField.ActivateInputField();
            },
            // ʧ�ܻص�
            (error) =>
            {
                AppendMessage("ϵͳ", $"<color=#FF8888>{error}</color>");
                inputField.interactable = true;
                inputField.ActivateInputField();
            }
        );

    }
}
