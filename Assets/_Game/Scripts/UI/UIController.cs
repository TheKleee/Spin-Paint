using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    #region Singleton
    public static UIController instance;
    void Awake() => instance = this;
    #endregion

    [Header("Outline List:"), SerializeField]
    Image[] outlines;

    public void Selected(int id)
    {
        for (int i = 0; i < outlines.Length; i++)
            outlines[i].enabled = false;

        outlines[id].enabled = true;
    }
}
