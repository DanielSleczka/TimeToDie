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

    public GameObject deathScreen;
    public TextMeshProUGUI deathText;

    public Slider healthSlider;
    private void Awake()
    {
        instance = this;
    }

}
