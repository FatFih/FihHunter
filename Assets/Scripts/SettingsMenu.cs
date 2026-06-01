using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public Slider sensXSlider;
    public Slider sensYSlider;
    public TextMeshProUGUI sensXText;
    public TextMeshProUGUI sensYText;

    void Start()
    {
        sensXSlider.minValue = 400f;
        sensXSlider.maxValue = 50000f;
        sensYSlider.minValue = 400f;
        sensYSlider.maxValue = 50000f;
        sensXSlider.value = PlayerPrefs.GetFloat("SensX", 26000f);
        sensYSlider.value = PlayerPrefs.GetFloat("SensY", 26000f);

        if (sensXText != null) sensXText.text = Mathf.Round(sensXSlider.value).ToString();
        if (sensYText != null) sensYText.text = Mathf.Round(sensYSlider.value).ToString();

        sensXSlider.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("SensX", v);
            if (sensXText != null) sensXText.text = Mathf.Round(v).ToString();
        });
        sensYSlider.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("SensY", v);
            if (sensYText != null) sensYText.text = Mathf.Round(v).ToString();
        });
    }
}
