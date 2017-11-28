﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA.Input;
#else
using UnityEngine.VR.WSA.Input;
#endif
using HoloToolkit.Unity.InputModule;

namespace HoloToolkit.Unity
{
    public class SolverControllerFinder : MonoBehaviour
    {
        #region public members
        public InteractionSourceHandedness Handedness { get { return handedness; } set { handedness = value; } }
        public MotionControllerInfo.ControllerElementEnum Element { get { return element; } }
        public Transform ElementTransform { get; private set; }
        public bool IsAttached { get { return isAttached; } }
        #endregion

        #region private members
        protected MotionControllerInfo controller;
        protected InteractionSourceHandedness handedness = InteractionSourceHandedness.Left;
        protected MotionControllerInfo.ControllerElementEnum element = MotionControllerInfo.ControllerElementEnum.PointingPose; // This probably should be hard coded? Maybe?
        private bool isAttached = false;
        private Transform elementTransform;
        #endregion


        public virtual void OnEnable()
        {
            #if UNITY_WSA && UNITY_2017_2_OR_NEWER
            // Look if the controller has loaded.
            if (MotionControllerVisualizer.Instance.TryGetControllerModel(handedness, out controller))
            {
                AddControllerTransform(controller);
            }
            #endif

            MotionControllerVisualizer.Instance.OnControllerModelLoaded += AddControllerTransform;
            MotionControllerVisualizer.Instance.OnControllerModelUnloaded += RemoveControllerTransform;
        }

        protected virtual void OnDisable()
        {
            if (MotionControllerVisualizer.IsInitialized)
            {
                MotionControllerVisualizer.Instance.OnControllerModelLoaded -= AddControllerTransform;
                MotionControllerVisualizer.Instance.OnControllerModelUnloaded -= RemoveControllerTransform;
            }
        }

        protected virtual void OnDestroy()
        {
            if (MotionControllerVisualizer.IsInitialized)
            {
                MotionControllerVisualizer.Instance.OnControllerModelLoaded -= AddControllerTransform;
                MotionControllerVisualizer.Instance.OnControllerModelUnloaded -= RemoveControllerTransform;
            }
        }

        private void AddControllerTransform(MotionControllerInfo newController)
        {
            if (!isAttached && newController.Handedness == handedness)
            {
                if (!newController.TryGetElement(element, out elementTransform))
                {
                    Debug.LogError("Unable to find element of type " + element + " under controller " + newController.ControllerParent.name + "; not attaching.");
                    return;
                }
                controller = newController;
                // update elementTransform for cosnsumption
                controller.TryGetElement(element, out elementTransform);
                ElementTransform = elementTransform;

                isAttached = true;
            }
        }

        private void RemoveControllerTransform(MotionControllerInfo oldController)
        {
            if (isAttached && oldController.Handedness == handedness)
            {
                controller = null;
                isAttached = false;
            }

        }

    }
}