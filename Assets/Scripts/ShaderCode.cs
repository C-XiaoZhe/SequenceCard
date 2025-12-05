using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ShaderCode : MonoBehaviour
{

    Image image;
    Material m;
    //CardVisual visual;

    void Awake() // 改为 Awake 确保通过GetComponent能尽早获取
    {
        image = GetComponent<Image>();
        // 创建材质实例，防止所有卡牌共用一个材质
        m = new Material(image.material);
        image.material = m;
        
        // 默认初始化为普通材质
        SetEdition("REGULAR");
    }

    // [新增] 公开方法：设置卡牌的效果版本
    public void SetEdition(string editionName)
    {
        if (image == null || m == null) return;

        // 1. 先禁用所有已知的关键字
        // 假设Shader中定义的关键字是 _EDITION_REGULAR, _EDITION_POLYCHROME, _EDITION_NEGATIVE
        m.DisableKeyword("_EDITION_REGULAR");
        m.DisableKeyword("_EDITION_POLYCHROME");
        m.DisableKeyword("_EDITION_NEGATIVE");
        m.DisableKeyword("_EDITION_FOIL"); // 如果有全息等其他效果

        // 2. 启用目标关键字
        m.EnableKeyword("_EDITION_" + editionName);
    }

    // Start is called before the first frame update
    // void Start()
    // {
    //     image = GetComponent<Image>();
    //     m = new Material(image.material);
    //     image.material = m;
    //     visual = GetComponentInParent<CardVisual>();

    //     string[] editions = new string[4];
    //     editions[0] = "REGULAR";
    //     editions[1] = "POLYCHROME";
    //     editions[2] = "REGULAR";
    //     editions[3] = "NEGATIVE";

    //     for (int i = 0; i < image.material.enabledKeywords.Length; i++)
    //     {
    //         image.material.DisableKeyword(image.material.enabledKeywords[i]);
    //     }
    //     image.material.EnableKeyword("_EDITION_" + editions[Random.Range(0, editions.Length)]);
    // }

    

    // Update is called once per frame
    void Update()
    {

        // Get the current rotation as a quaternion
        Quaternion currentRotation = transform.parent.localRotation;

        // Convert the quaternion to Euler angles
        Vector3 eulerAngles = currentRotation.eulerAngles;

        // Get the X-axis angle
        float xAngle = eulerAngles.x;
        float yAngle = eulerAngles.y;

        // Ensure the X-axis angle stays within the range of -90 to 90 degrees
        xAngle = ClampAngle(xAngle, -90f, 90f);
        yAngle = ClampAngle(yAngle, -90f, 90);


        m.SetVector("_Rotation", new Vector2(ExtensionMethods.Remap(xAngle,-20,20,-.5f,.5f), ExtensionMethods.Remap(yAngle, -20, 20, -.5f, .5f)));

    }

    // Method to clamp an angle between a minimum and maximum value
    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -180f)
            angle += 360f;
        if (angle > 180f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
