using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("Textures:")]
    public Image pointer;
    [Space]
    public Sprite[] pointers;

#if UNITY_EDITOR
    private void Awake()
    {
        Cursor.visible = false;
        pointer.sprite = pointers[0];
    }

    private void Update()
    {
        pointer.gameObject.transform.position = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
            pointer.sprite = pointers[1];

        if (Input.GetMouseButtonUp(0))
            pointer.sprite = pointers[0];

        if (Input.GetMouseButtonDown(1)) HideCursor();
    }

    void HideCursor()
    {
        Cursor.visible = !Cursor.visible;
        pointer.gameObject.SetActive(!Cursor.visible);
    }
#endif
}
