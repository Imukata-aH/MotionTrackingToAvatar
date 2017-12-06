using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FPSCounter : MonoBehaviour {

    public float updateInterval = 0.5f;

    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft = 0.0f;

    // Use this for initialization
    void Start () {
        timeLeft = updateInterval;
    }
    
    // Update is called once per frame
    void Update () {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if(timeLeft <= 0.0f)
        {
            GetComponent<Text>().text = "FPS: " + (accum / frames).ToString("f2");
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}
