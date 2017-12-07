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
    /// LowPassFilteringのファクター（小さい値のほうがより平滑化されるが遅延が大きくなる）
    /// </summary>
    [Range(0.01f, 1.0f)]
    public float lowPassFactor = 0.5f;

    /// <summary>
    /// 対象頭部オブジェクトの初期 Transform
    /// </summary>
    private Quaternion initialModelHeadRotation;

    /// <summary>
    /// FaceTracking の結果による Transform 到達値
    /// </summary>
    private Quaternion destinationFaceRotation;

    /// <summary>
    /// 初回のFilteringを判定（Filterの初期値を設定する）
    /// </summary>
    private bool isInitialFiltering = true;

    public bool isLocal = false;

    private OpenFaceNativePluginWrapper wrapper;
    private OpenFaceNativePluginWrapper.FaceTrackingValues trackingValue;
    private Matrix4x4 transformationM = Matrix4x4.identity;
    private Matrix4x4 invertYM;
    private Matrix4x4 invertZM;

    private Thread faceTrackingWorkerThread;
    private bool isRunning = false;
    private bool isUpdatedFaceTracking = false;
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
        if(!isLocal)
            this.initialModelHeadRotation = targetFaceObject.transform.rotation;
        else
            this.initialModelHeadRotation = targetFaceObject.transform.localRotation;

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
        lock (locker)
        {
            if (this.isUpdatedFaceTracking)
            {
                this.destinationFaceRotation = FaceTrackingUtils.ExtractRotationFromMatrix(ref transformationM);
                this.destinationFaceRotation.eulerAngles = new Vector3(-this.destinationFaceRotation.eulerAngles.x, -this.destinationFaceRotation.eulerAngles.y, this.destinationFaceRotation.eulerAngles.z);   // 鏡写しに回転するよう補正

                if (this.isLocal)
                {
                    this.destinationFaceRotation = Quaternion.Euler(this.targetFaceObject.transform.parent.TransformVector(this.destinationFaceRotation.eulerAngles));
                }

                this.isUpdatedFaceTracking = false;
            }
        }


        if (!this.isLocal)
            this.destinationFaceRotation = this.destinationFaceRotation * this.initialModelHeadRotation;
        else
            this.destinationFaceRotation = this.initialModelHeadRotation * this.destinationFaceRotation;

        // 毎フレーム頭部の回転値に対してLowPassFilteringして補間
        if (!isLocal)
            this.targetFaceObject.transform.rotation = LowpassFilterQuaternion(this.targetFaceObject.transform.rotation, this.destinationFaceRotation, this.lowPassFactor, this.isInitialFiltering);
        else
            this.targetFaceObject.transform.localRotation = LowpassFilterQuaternion(this.targetFaceObject.transform.localRotation, this.destinationFaceRotation, this.lowPassFactor, this.isInitialFiltering);

        this.isInitialFiltering = false;
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


        lock (locker)
        {
            double[] rotMat = trackingValue.rotationMatrix;
            double[] tVec = trackingValue.translation;
            transformationM.SetRow(0, new Vector4((float)rotMat[0 * 3 + 0], (float)rotMat[0 * 3 + 1], (float)rotMat[0 * 3 + 2], (float)tVec[0]));
            transformationM.SetRow(1, new Vector4((float)rotMat[1 * 3 + 0], (float)rotMat[1 * 3 + 1], (float)rotMat[1 * 3 + 2], (float)tVec[1]));
            transformationM.SetRow(2, new Vector4((float)rotMat[2 * 3 + 0], (float)rotMat[2 * 3 + 1], (float)rotMat[2 * 3 + 2], (float)tVec[2]));
            transformationM.SetRow(3, new Vector4(0, 0, 0, 1));

            this.isUpdatedFaceTracking = true;

        }

    }

    private Quaternion LowpassFilterQuaternion(Quaternion intermediateValue, Quaternion targetValue, float factor, bool init)
    {
        if (init)
        {
            intermediateValue = targetValue;
        }

        intermediateValue = Quaternion.Lerp(intermediateValue, targetValue, factor);
        return intermediateValue;
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
