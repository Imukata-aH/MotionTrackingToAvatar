using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TestScripts
{
    public class GazeRotationTest : MonoBehaviour
    {
        public Transform EyeL;
        public Transform EyeR;

        [HeaderAttribute("目の回転")]
        public bool SyncLRRotation = true; //左右の回転を同期するか(左の回転に同期します)

        [Range(-22f, 30f)]
        public float EyeLRotationY = 0f;

        [Range(-22f, 30f)]
        public float EyeRRotationY = 0f;

        [Range(-35f, 35f)]
        public float EyeLRotationZ = 0f;

        [Range(-35f, 35f)]
        public float EyeRRotationZ = 0f;

        [HeaderAttribute("目の大きさ")]
        public bool SyncLRSize = true; //左右の大きさを同期するか(左のサイズに同期します)

        [Range(0f, 1.5f)]
        public float EyeLScaleY = 1f;

        [Range(0f, 1.5f)]
        public float EyeRScaleY = 1f;

        [Range(0f, 1.5f)]
        public float EyeLScaleZ = 1f;

        [Range(0f, 1.5f)]
        public float EyeRScaleZ = 1f;

        private Vector3 _defaultRotationL;
        private Vector3 _defaultRotationR;
        private Material _highLightMaterial;
        private bool _isStarted;

        // Use this for initialization
        private void Start()
        {
            this._isStarted = true;

            //目の回転の初期値取得
            this._defaultRotationL = this.EyeL.localRotation.eulerAngles;
            this._defaultRotationR = this.EyeR.localRotation.eulerAngles;
        }

        // Update is called once per frame
        private void Update()
        {
        }

        public void OnValidate()
        {
            if (Application.isPlaying && this._isStarted)
            {
                //目の回転
                if (this.SyncLRRotation)
                {
                    this.EyeRRotationY = this.EyeLRotationY;
                    this.EyeRRotationZ = this.EyeLRotationZ;
                }
                var rotation = this._defaultRotationL;
                rotation.y += this.EyeLRotationY;
                rotation.z += this.EyeLRotationZ;
                this.EyeL.localRotation = Quaternion.Euler(rotation);

                rotation = this._defaultRotationR;
                rotation.y += this.EyeRRotationY;
                rotation.z += this.EyeRRotationZ;
                this.EyeR.localRotation = Quaternion.Euler(rotation);

                //目の大きさ
                if (this.SyncLRSize)
                {
                    this.EyeRScaleY = this.EyeLScaleY;
                    this.EyeRScaleZ = this.EyeLScaleZ;
                }

                this.EyeL.localScale = new Vector3(1f, this.EyeLScaleY, this.EyeLScaleZ);
                this.EyeR.localScale = new Vector3(1f, this.EyeRScaleY, this.EyeRScaleZ);
            }
        }
    }
}