using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySliderValue : MonoBehaviour {
    // Start is called before the first frame update

    [SerializeField]
    Slider slider;
    TMP_Text text;
    void Start() {
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update() {
        text.text = slider.value.ToString("F2");
    }
}
