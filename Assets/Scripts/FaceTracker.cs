using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FaceTracker : MonoBehaviour
{

    private OpenFaceNativePluginWrapper wrapper;
    private OpenFaceNativePluginWrapper.FaceTrackingValues trackingValue;

    // Use this for initialization
    void Start()
    {
        string basePath = "Assets/Resources";

        wrapper = new OpenFaceNativePluginWrapper();
        wrapper.Initialize(Path.Combine(basePath, "model/main_clnf_general.txt").ToString(), Path.Combine(basePath, "classifiers/haarcascade_frontalface_alt.xml"), basePath, false);
    }

    // Update is called once per frame
    void Update()
    {
        wrapper.Update();
        trackingValue = wrapper.GetFaceTrackingValues();
    }

    private void OnApplicationQuit()
    {
        wrapper.Terminate();
    }
}
