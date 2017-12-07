using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class FaceTrackerToLookTarget : MonoBehaviour {
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
    /// LowPassFilteringのファクター（小さい値のほうがより平滑化されるが遅延が大きくなる）
    /// </summary>
    [Range(0.01f, 1.0f)]
    public float lowPassFactor = 0.5f;

    /// <summary>
    /// 頭部の Look Target
    /// </summary>
    public Transform HeadLookTarget;

    /// <summary>
    /// 頭部 LookTarget の回転中心
    /// </summary>
    public Transform HeadLookTargetRotationCenter;

    /// <summary>
    /// 頭部モデル
    /// </summary>
    public Transform HeadModel;

    /// <summary>
    /// FaceTracking の結果による Transform 到達値
    /// </summary>
    private Quaternion destinationFaceRotation = Quaternion.identity;

    /// <summary>
    /// 初回のFilteringを判定（Filterの初期値を設定する）
    /// </summary>
    private bool isInitialFiltering = true;

    /// <summary>
    /// 頭部モデルから LookTarget までの距離
    /// </summary>
    private float headLookTargetDistance = 2.0f;

    private OpenFaceNativePluginWrapper wrapper;
    private OpenFaceNativePluginWrapper.FaceTrackingValues trackingValue;
    private Matrix4x4 transformationM = Matrix4x4.identity;

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

        // 頭部 LookTarget の初期配置
        this.HeadLookTargetRotationCenter.position = this.HeadModel.position;
        this.HeadLookTarget.localPosition = new Vector3(0, 0, headLookTargetDistance);

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
        if (this.faceTrackingWorkerThread != null)
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
                this.destinationFaceRotation.eulerAngles = new Vector3(this.destinationFaceRotation.eulerAngles.x, -this.destinationFaceRotation.eulerAngles.y, this.destinationFaceRotation.eulerAngles.z);   // 鏡写しに回転するよう補正

                this.isUpdatedFaceTracking = false;
            }
        }

        // ワールド座標中心に回す
        this.HeadLookTargetRotationCenter.rotation = this.destinationFaceRotation;
        // 回転対象のローカル座標に中心位置を戻す
        this.HeadLookTargetRotationCenter.position = this.HeadModel.position;
        // モデル全体(Root)の回転値を反映
        this.HeadLookTargetRotationCenter.rotation *= this.HeadModel.root.rotation;

        this.isInitialFiltering = false;
    }

    private void DoFaceTracking()
    {
        while (this.isRunning)
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
