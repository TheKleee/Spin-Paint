using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MEC;
//using Tabtale.TTPlugins;

[RequireComponent(typeof(Camera))]
public class CamController : MonoBehaviour
{
    //[Header("Offset Position")]
    //public Vector3 offsetPos = new Vector3(0, 10, -10);

    [Header("Speed:")]
    [Range(1, 25)] public float camSpeed = 10;
    [Range(.5f, 12.5f)] public float camRotSpeed = 5;

    [Header("Target")]
    public Transform target;

    Camera cam;
    [HideInInspector] public bool moveAble = true;

    private void Awake()
    {
        //TTPCore.Setup();
        cam = GetComponent<Camera>();
        moveAble = true;
        //transform.localPosition = offsetPos;
    }

    private void LateUpdate()
    {
        if (target != null && moveAble)
                transform.parent.position = Vector3.Lerp(transform.parent.position, target.position, camSpeed * Time.fixedDeltaTime);
    }
}
