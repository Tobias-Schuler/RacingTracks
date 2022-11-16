using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateParameters : MonoBehaviour {

    [SerializeField]
    private TMP_InputField seed;
    [SerializeField]
    private TMP_InputField trackLength;
    [SerializeField]
    private TMP_InputField segmentLength;
    [SerializeField]
    private TMP_InputField segmentWidth;
    [SerializeField]
    private Slider railWidthSlider;
    [SerializeField]
    private Slider railHeightSlider;

    // Start is called before the first frame update
    void Start() {

        //load default on startup
        seed.text = "" + RTP.seed;
        trackLength.text = "" + RTP.trackLength;
        segmentLength.text = "" + RTP.segmentLength;
        segmentWidth.text = "" + RTP.segmentWidth;
        railWidthSlider.value = RTP.railWidth;
        railHeightSlider.value = RTP.railHeight;
        //event listeners when UI updates
        seed.onValueChanged.AddListener(delegate { UpdateFields(); });
        trackLength.onValueChanged.AddListener(delegate { UpdateFields(); });
        segmentLength.onValueChanged.AddListener(delegate { UpdateFields(); });
        segmentWidth.onValueChanged.AddListener(delegate { UpdateFields(); });
        railWidthSlider.onValueChanged.AddListener(delegate { UpdateRailWidth(); });
        railHeightSlider.onValueChanged.AddListener(delegate { UpdateRailHeight(); });

    }

    // Update is called once per frame
    void Update() {

    }

    private void UpdateRailWidth() {
        RTP.railWidth = railWidthSlider.value;
    }

    private void UpdateRailHeight() {
        RTP.railHeight = railHeightSlider.value;
    }

    public void UpdateFields() {

        try {
            RTP.seed = int.Parse(seed.text);
            RTP.trackLength = int.Parse(trackLength.text);
            RTP.segmentLength = float.Parse(segmentLength.text);
            RTP.segmentWidth = float.Parse(segmentWidth.text);
        } catch (FormatException) {

        }

    }

    public void NewRandomSeed() {
        RTP.seed = (int)DateTime.Now.Ticks;
        seed.text = "" + RTP.seed;
    }

}
