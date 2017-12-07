using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationAroundPointTest : MonoBehaviour {

    public Transform LookTarget;
    public Transform RotationCenter;

    [Range(-90, 90)]
    public float rotationX;

    [Range(-90, 90)]
    public float rotationY;

    [Range(-90, 90)]
    public float rotationZ;

    public float targetDistanceFromCenter = 1.0f;

    // Use this for initialization
    void Start () {
        LookTarget.position = new Vector3(RotationCenter.position.x, RotationCenter.position.y, RotationCenter.position.z - targetDistanceFromCenter);
    }
    
    // Update is called once per frame
    void Update () {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
        RotationCenter.rotation = rotation;
    }
}
