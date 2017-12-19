﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Gaze handles everything related to a gaze ray that can interact with other objects.
    /// </summary>
    public class GazePointer : Singleton<GazePointer>, IPointingSource
    {
        public Cursor CursorOverride { get; set; }

        /// <summary>
        /// HitInfo property gives access to information at the object being gazed at, if any.
        /// </summary>
        public RaycastHit HitInfo { get; private set; }

        /// <summary>
        /// The game object that is currently being gazed at, if any.
        /// </summary>
        public GameObject HitObject { get; private set; }

        /// <summary>
        /// Position at which the gaze manager hit an object.
        /// If no object is currently being hit, this will use the last hit distance.
        /// </summary>
        public Vector3 HitPosition { get; private set; }

        /// <summary>
        /// Normal of the point at which the gaze manager hit an object.
        /// If no object is currently being hit, this will return the previous normal.
        /// </summary>
        public Vector3 HitNormal { get; private set; }

        /// <summary>
        /// Origin of the gaze.
        /// </summary>
        public Vector3 GazeOrigin
        {
            get { return Rays[0].origin; }
        }

        /// <summary>
        /// Normal of the gaze.
        /// </summary>
        public Vector3 GazeNormal
        {
            get { return Rays[0].direction; }
        }

        /// <summary>
        /// Maximum distance at which the gaze can collide with an object.
        /// </summary>
        [Tooltip("Maximum distance at which the gaze can collide with an object.")]
        public float MaxGazeCollisionDistance = 10.0f;

        /// <summary>
        /// The LayerMasks, in prioritized order, that are used to determine the HitObject when raycasting.
        ///
        /// Example Usage:
        ///
        /// // Allow the cursor to hit SR, but first prioritize any DefaultRaycastLayers (potentially behind SR)
        ///
        /// int sr = LayerMask.GetMask("SR");
        /// int nonSR = Physics.DefaultRaycastLayers & ~sr;
        /// GazeManager.Instance.RaycastLayerMasks = new LayerMask[] { nonSR, sr };
        /// </summary>
        [Tooltip("The LayerMasks, in prioritized order, that are used to determine the HitObject when raycasting.\n\nExample Usage:\n\n// Allow the cursor to hit SR, but first prioritize any DefaultRaycastLayers (potentially behind SR)\n\nint sr = LayerMask.GetMask(\"SR\");\nint nonSR = Physics.DefaultRaycastLayers & ~sr;\nGazeManager.Instance.RaycastLayerMasks = new LayerMask[] { nonSR, sr };")]
        public LayerMask[] RaycastLayerMasks = { Physics.DefaultRaycastLayers };

        /// <summary>
        /// Current stabilization method, used to smooth out the gaze ray data.
        /// If left null, no stabilization will be performed.
        /// </summary>
        [Tooltip("Stabilizer, if any, used to smooth out the gaze ray data.")]
        public BaseRayStabilizer Stabilizer = null;

        /// <summary>
        /// Transform that should be used as the source of the gaze position and rotation.
        /// Defaults to the main camera.
        /// </summary>
        [Tooltip("Transform that should be used to represent the gaze position and rotation. Defaults to CameraCache.Main")]
        public Transform GazeTransform;

        [Tooltip("True to draw a debug view of the ray.")]
        public bool DebugDrawRay;

        public PointerResult Result { get; set; }

        public GameObject Target { get; set; }

        public GameObject PreviousTarget { get; set; }

        public RayStep[] Rays { get { return rays; } }

        private RayStep[] rays = new RayStep[1] { new RayStep(Vector3.zero, Vector3.zero) };

        public float? ExtentOverride
        {
            get { return MaxGazeCollisionDistance; }
        }

        public LayerMask[] PrioritizedLayerMasksOverride
        {
            get { return RaycastLayerMasks; }
        }

        public bool InteractionEnabled
        {
            get { return true; }
        }

        public bool FocusLocked { get; set; }

        private float lastHitDistance = 2.0f;

        protected override void Awake()
        {
            base.Awake();

            Result = new PointerResult();

            // Add default RaycastLayers as first layerPriority
            if (RaycastLayerMasks == null || RaycastLayerMasks.Length == 0)
            {
                RaycastLayerMasks = new LayerMask[] { Physics.DefaultRaycastLayers };
            }

            FindGazeTransform();
        }

        private void Update()
        {
            if (!FindGazeTransform())
            {
                Debug.Log("Didn't find gaze transform");
                return;
            }

            if (DebugDrawRay)
            {
                Debug.DrawRay(GazeOrigin, (HitPosition - GazeOrigin), Color.white);
            }
        }

        private bool FindGazeTransform()
        {
            if (GazeTransform != null) { return true; }

            if (CameraCache.Main != null)
            {
                GazeTransform = CameraCache.Main.transform;
                return true;
            }

            Debug.LogError("Gaze Manager was not given a GazeTransform and no main camera exists to default to.");
            return false;
        }

        /// <summary>
        /// Updates the current gaze information, so that the gaze origin and normal are accurate.
        /// </summary>
        private void UpdateGazeInfo()
        {
            if (GazeTransform == null)
            {
                Rays[0] = default(RayStep);
            }
            else
            {
                Vector3 newGazeOrigin = GazeTransform.position;
                Vector3 newGazeNormal = GazeTransform.forward;

                // Update gaze info from stabilizer
                if (Stabilizer != null)
                {
                    Stabilizer.UpdateStability(newGazeOrigin, GazeTransform.rotation);
                    newGazeOrigin = Stabilizer.StablePosition;
                    newGazeNormal = Stabilizer.StableRay.direction;
                }

                Rays[0].UpdateRayStep(newGazeOrigin, newGazeOrigin + (newGazeNormal * FocusManager.Instance.GetPointingExtent(this)));
            }

            UpdateHitPosition();
        }

        public virtual void OnPreRaycast()
        {
            UpdateGazeInfo();
        }

        public virtual void OnPostRaycast()
        {
            // Nothing needed
        }

        public bool OwnsInput(BaseInputEventData eventData)
        {
            // NOTE: This is a simple pointer and not meant to be used simultaneously with others.
            return isActiveAndEnabled;
        }

        /// <summary>
        /// Notifies this gaze manager of its new hit details.
        /// </summary>
        /// <param name="result">Details of the current focus.</param>
        /// <param name="hitInfo">Details of the focus raycast hit.</param>
        /// <param name="isRegisteredForFocus">Whether or not this gaze manager is registered as a focus pointer.</param>
        public void UpdateFocusResult(PointerResult result, RaycastHit hitInfo, bool isRegisteredForFocus)
        {
            HitInfo = hitInfo;
            HitObject = isRegisteredForFocus
                ? result.PointingSource.Target
                : null; // If we're not actually registered for focus, we keep HitObject as null so we don't mislead anyone.

            if (result.PointingSource.Target != null)
            {
                lastHitDistance = (result.Point - Rays[0].origin).magnitude;
                UpdateHitPosition();
                HitNormal = result.Normal;
            }
        }

        private void UpdateHitPosition()
        {
            HitPosition = (Rays[0].origin + (lastHitDistance * Rays[0].direction));
        }
    }
}
