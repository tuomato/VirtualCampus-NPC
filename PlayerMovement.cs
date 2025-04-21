using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 8f;    // �ƶ��ٶ�
    public float jumpForce = 5f;    // ��Ծ������ѡ��
    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;  // ��ֹ��ת
    }
    void FixedUpdate()
    {
        // ��ȡ����
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // �����ƶ�����
        Vector3 move = transform.right * x + transform.forward * z;
        rb.AddForce(move * moveSpeed * 10f);

        // ��Ծ����ѡ��
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
