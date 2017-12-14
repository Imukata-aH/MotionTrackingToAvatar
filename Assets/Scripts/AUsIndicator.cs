using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AUsIndicator : MonoBehaviour {

    public GameObject TextPrefab;

    public Transform IntencityIndicatorPanel;
    public Transform ClassIndicatorPanel;

    public FaceTrackerToLookTarget FaceTracker;

    private List<Text> AUsIntensityTextList;
    private List<Text> AUsClassTextList;

    private static readonly List<string> AUsIntensityNameList = new List<string>()
    {
        "Inner Brow Raiser",    // AU1
        "Outer Brow Raiser",    // AU2
        "Brow Lowerer",         // AU4
        "Upper Lid Raiser",     // AU5
        "Cheek Raiser",         // AU6
        "Lid Tightener",        // AU7
        "Nose Wrinkler",        // AU9
        "Upper Lip Raiser",     // AU10
        "Lip Corner Puller",    // AU12
        "Dimpler",              // AU14
        "Lip Corner Depressor", // AU15
        "Chin Raiser",          // AU17
        "Lip Stretcher",        // AU20
        "Lip Tightener",        // AU23
        "Lips Part",            // AU25
        "Jaw Drop",             // AU26
        "Blink",                // AU45
    };

    private static readonly List<string> AUsClassNameList = new List<string>()
    {
        "Inner Brow Raiser",    // AU1
        "Outer Brow Raiser",    // AU2
        "Brow Lowerer",         // AU4
        "Upper Lid Raiser",     // AU5
        "Cheek Raiser",         // AU6
        "Lid Tightener",        // AU7
        "Nose Wrinkler",        // AU9
        "Upper Lip Raiser",     // AU10
        "Lip Corner Puller",    // AU12
        "Dimpler",              // AU14
        "Lip Corner Depressor", // AU15
        "Chin Raiser",          // AU17
        "Lip Stretcher",        // AU20
        "Lip Tightener",        // AU23
        "Lips Part",            // AU25
        "Jaw Drop",             // AU26
        "Lip Suck",             // AU28
        "Blink",                // AU45
    };

    // Use this for initialization
    void Start () {
        this.AUsIntensityTextList = new List<Text>();
        for (int i = 0; i < AUsIntensityNameList.Count; i++)
        {
            var textInstance = Instantiate(this.TextPrefab);
            textInstance.transform.SetParent(this.IntencityIndicatorPanel);
            Text text = textInstance.GetComponent<Text>();
            text.text = string.Format("{0}:\n{1}", AUsIntensityNameList[i], 0.0f.ToString("F4"));
            this.AUsIntensityTextList.Add(text);
        }

        this.AUsClassTextList = new List<Text>();
        for (int i = 0; i < AUsClassNameList.Count; i++)
        {
            var textInstance = Instantiate(this.TextPrefab);
            textInstance.transform.SetParent(this.ClassIndicatorPanel);
            Text text = textInstance.GetComponent<Text>();
            text.text = string.Format("{0}:\n{1}", AUsClassNameList[i], 0.0f.ToString("F4"));
            this.AUsClassTextList.Add(text);
        }
    }
	
	// Update is called once per frame
	void Update () {
        List<float> intensityList = FaceTracker.GetAUsIntensity();
        if(intensityList != null)
        {
            for (int i = 0; i < intensityList.Count; i++)
            {
                this.AUsIntensityTextList[i].text = string.Format("{0}:\n{1}", AUsIntensityNameList[i], intensityList[i].ToString("F4"));
            }
        }

        List<float> classList = FaceTracker.GetAUsClass();
        if(classList != null)
        {
            for (int i = 0; i < classList.Count; i++)
            {
                this.AUsClassTextList[i].text = string.Format("{0}:\n{1}", AUsClassNameList[i], classList[i].ToString("F4"));
            }
        }
    }
}
