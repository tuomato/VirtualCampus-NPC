using UnityEngine;

public class NPC_Trigger : MonoBehaviour
{
    [Header("交互设置")]
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    private bool isPlayerInRange;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("开始对话");
            DialogueManager.Instance.StartDialogue();
            UIManager.Instance.HidePrompt(); // 开始对话后隐藏提示
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UIManager.Instance.ShowPrompt("按 E 开始对话");
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