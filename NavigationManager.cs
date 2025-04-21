using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    public void HighlightTarget(string buildingName, Vector2 coordinate)
    {
        // 实现地图标记逻辑
        Debug.Log($"导航到{buildingName} ({coordinate.x}, {coordinate.y})");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
