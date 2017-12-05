using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FaceTracker : MonoBehaviour
{
    /// <summary>
    /// FaceTracker の Tracking 中の画像表示等を行うかどうか
    /// </summary>
    public bool isQuietMode;

    /// <summary>
    /// Tracking 結果の Certainty を受け入れる上限 (-1 is max certainty, 1 is minimum certainty)
    /// </summary>
    [Range(-1.0f, 1.0f)]
    public float certaintyThreshold = 0.2f;

    /// <summary>
    /// FaceTracking の結果を適用する頭部対象
    /// </summary>
    public GameObject targetFaceObject;

    /// <summary>
    /// 対象頭部オブジェクトの初期 Transform
    /// </summary>
    private Transform initialFaceTransform;

    /// <summary>
    /// FaceTracking の結果による Transform 到達値
    /// </summary>
    private Transform destinationFaceTransform;

    private OpenFaceNativePluginWrapper wrapper;
    private OpenFaceNativePluginWrapper.FaceTrackingValues trackingValue;

    // Use this for initialization
    void Start()
    {
        // 初期 Transform 値の設定
        this.initialFaceTransform = targetFaceObject.transform;
        this.destinationFaceTransform = this.initialFaceTransform;

        // FaceTracker の初期化
        string basePath = "Assets/Resources";
        wrapper = new OpenFaceNativePluginWrapper();
        wrapper.Initialize(Path.Combine(basePath, "model/main_clnf_general.txt").ToString(), Path.Combine(basePath, "classifiers/haarcascade_frontalface_alt.xml"), basePath, this.isQuietMode);
    }

    // Update is called once per frame
    void Update()
    {
        wrapper.Update();
        UpdateTrackingValues();
        UpdateTarget();
    }

    private void OnApplicationQuit()
    {
        wrapper.Terminate();
    }

    private void UpdateTrackingValues()
    {
        trackingValue = wrapper.GetFaceTrackingValues();

        if(trackingValue.detectionCertainty > this.certaintyThreshold)
        {
            // 結果の信頼度が一定以下であれば無視
            return;
        }

        Matrix4x4 mat = Matrix4x4.identity;

        // Euler to Matrix
        float s1 = Mathf.Sin((float)trackingValue.rotationEuler[0]);
        float s2 = Mathf.Sin((float)trackingValue.rotationEuler[1]);
        float s3 = Mathf.Sin((float)trackingValue.rotationEuler[2]);

        float c1 = Mathf.Cos((float)trackingValue.rotationEuler[0]);
        float c2 = Mathf.Cos((float)trackingValue.rotationEuler[1]);
        float c3 = Mathf.Cos((float)trackingValue.rotationEuler[2]);

        mat[0, 0] = c2 * c3;
        mat[0, 1] = -c2 * s3;
        mat[0, 2] = s2;
        mat[1, 0] = c1 * s3 + c3 * s1 * s2;
        mat[1, 1] = c1 * c3 - s1 * s2 * s3;
        mat[1, 2] = -c2 * s1;
        mat[2, 0] = s1 * s3 - c1 * c3 * s2;
        mat[2, 1] = c3 * s1 + c1 * s2 * s3;
        mat[2, 2] = c1 * c2;

        Matrix4x4 invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
        Matrix4x4 invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

        mat = invertYM * mat;
        mat = mat * invertZM;

        Vector3 forward;
        forward.x = mat.m02;
        forward.y = mat.m12;
        forward.z = mat.m22;

        Vector3 upwards;
        upwards.x = mat.m01;
        upwards.y = mat.m11;
        upwards.z = mat.m21;

        this.destinationFaceTransform.localRotation = Quaternion.LookRotation(forward, upwards);
        this.destinationFaceTransform.localRotation *= this.initialFaceTransform.localRotation;

        // OpenCV の座標系から Unity の座標系へ移しながら値を更新
        //this.destinationFaceTransform.localRotation = Quaternion.Euler(0, 0, - (float)trackingValue.rotationEuler[0]);
        //this.destinationFaceTransform.localRotation *= this.initialFaceTransform.localRotation;


    }

    private void UpdateTarget()
    {
        this.targetFaceObject.transform.localRotation = this.destinationFaceTransform.localRotation;
    }

}
