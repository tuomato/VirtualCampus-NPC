using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 8f;    // 移动速度
    public float jumpForce = 5f;    // 跳跃力（可选）
    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;  // 防止旋转
    }
    void FixedUpdate()
    {
        // 获取输入
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 计算移动向量
        Vector3 move = transform.right * x + transform.forward * z;
        rb.AddForce(move * moveSpeed * 10f);

        // 跳跃（可选）
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
