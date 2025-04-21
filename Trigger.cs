using UnityEngine;

public class NPC_Trigger : MonoBehaviour
{
    [Header("��������")]
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    private bool isPlayerInRange;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("��ʼ�Ի�");
            DialogueManager.Instance.StartDialogue();
            UIManager.Instance.HidePrompt(); // ��ʼ�Ի���������ʾ
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UIManager.Instance.ShowPrompt("�� E ��ʼ�Ի�");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            UIManager.Instance.HidePrompt();
        }
    }
}