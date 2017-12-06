using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    private Quaternion initialModelHeadRotation;

    /// <summary>
    /// FaceTracking の結果による Transform 到達値
    /// </summary>
    private Quaternion destinationFaceRotation;

    private OpenFaceNativePluginWrapper wrapper;
    private OpenFaceNativePluginWrapper.FaceTrackingValues trackingValue;
    private Matrix4x4 transformationM = Matrix4x4.identity;
    private Matrix4x4 invertYM;
    private Matrix4x4 invertZM;

    private Thread faceTrackingWorkerThread;
    private bool isRunning = false;
    private object locker;

    // Use this for initialization
    void Start()
    {
        // FaceTracking 用 WorkerThread 初期化
        locker = new object();
        this.faceTrackingWorkerThread = new Thread(new ThreadStart(DoFaceTracking))
        {
            Name = "FaceTrackingWorkerThread",
            IsBackground = true,
        };

        // 座標系変換行列の初期化
        invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
        //Debug.Log("invertYM " + invertYM.ToString());
        invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        //Debug.Log("invertZM " + invertZM.ToString());

        // 初期 Transform 値の設定
        this.initialModelHeadRotation = targetFaceObject.transform.rotation;
        this.destinationFaceRotation = this.initialModelHeadRotation;

        // FaceTracker の初期化
        string basePath = "Assets/Resources";
        wrapper = new OpenFaceNativePluginWrapper();
        wrapper.Initialize(Path.Combine(basePath, "model/main_clnf_general.txt").ToString(), Path.Combine(basePath, "classifiers/haarcascade_frontalface_alt.xml"), basePath, this.isQuietMode);
        
        // WorkerThread 開始
        this.isRunning = true;
        faceTrackingWorkerThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTargetModel();
    }

    private void OnApplicationQuit()
    {
        this.isRunning = false;
        if(this.faceTrackingWorkerThread != null)
        {
            this.faceTrackingWorkerThread.Join();
        }

        wrapper.Terminate();
    }

    private void UpdateTargetModel()
    {
        lock(locker)
        {
            this.targetFaceObject.transform.rotation = this.destinationFaceRotation;
        }
    }

    private void DoFaceTracking()
    {
        while(this.isRunning)
        {
            wrapper.Update();
            UpdateTrackingValues();
        }
    }

    private void UpdateTrackingValues()
    {
        wrapper.GetFaceTrackingValues(ref trackingValue);

        if (trackingValue.detectionCertainty > this.certaintyThreshold)
        {
            // 結果の信頼度が一定以下であれば無視
            return;
        }

        double[] rotMat = trackingValue.rotationMatrix;
        double[] tVec = trackingValue.translation;
        transformationM.SetRow(0, new Vector4((float)rotMat[0 * 3 + 0], (float)rotMat[0 * 3 + 1], (float)rotMat[0 * 3 + 2], (float)tVec[0]));
        transformationM.SetRow(1, new Vector4((float)rotMat[1 * 3 + 0], (float)rotMat[1 * 3 + 1], (float)rotMat[1 * 3 + 2], (float)tVec[1]));
        transformationM.SetRow(2, new Vector4((float)rotMat[2 * 3 + 0], (float)rotMat[2 * 3 + 1], (float)rotMat[2 * 3 + 2], (float)tVec[2]));
        transformationM.SetRow(3, new Vector4(0, 0, 0, 1));

        Quaternion rotation = FaceTrackingUtils.ExtractRotationFromMatrix(ref transformationM);
        rotation.eulerAngles = new Vector3(-rotation.eulerAngles.x, rotation.eulerAngles.y, -rotation.eulerAngles.z);   // 鏡写しに回転するよう補正

        lock(locker)
        {
            this.destinationFaceRotation = rotation * this.initialModelHeadRotation;
        }
    }
}

public class FaceTrackingUtils
{
    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }
}
