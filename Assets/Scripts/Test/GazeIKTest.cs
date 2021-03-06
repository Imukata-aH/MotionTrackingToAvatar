﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TestScripts
{
    public class GazeIKTest : MonoBehaviour
    {
        public bool IKActive = false;
        public Transform Target;
        public Transform ModelHead;

        private Animator _animator;

        // Use this for initialization
        private void Start()
        {
            this._animator = this.GetComponent<Animator>();

            this.Target.parent = this.ModelHead;    // モデルの頭部を基準に動くようにする。
            this.Target.localPosition = new Vector3(0, 0, 2.0f);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (this._animator == null) return;

            if (this.IKActive)
            {
                this._animator.SetLookAtWeight(1.0f, 0f, 0f, 1.0f, 0f);

                if (this.Target != null)
                {
                    this._animator.SetLookAtPosition(this.Target.position);
                }
            }
            else
            {
                this._animator.SetLookAtWeight(0.0f);

                if (this.Target != null)
                {
                    this.Target.position = this._animator.bodyPosition + this._animator.bodyRotation * new Vector3(0, 0.5f, 1);
                }
            }
        }
    }
}