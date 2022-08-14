using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public TextMeshProUGUI overheatedMessage;
    public Slider weaponTempSlider;

    private void Awake()
    {
        instance = this;
    }

}
