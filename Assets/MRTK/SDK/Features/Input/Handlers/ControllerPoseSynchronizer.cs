// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Waits for a controller to be initialized, then synchronizes its transform position to a specified handedness.
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/ControllerPoseSynchronizer")]
    public class ControllerPoseSynchronizer : InputSystemGlobalHandlerListener, IMixedRealityControllerPoseSynchronizer
    {
        #region Helpers
        protected bool SourcePoseDataUsable<T>(SourcePoseEventData<T> eventData)
        {
            return ((UseSourcePoseAsFallback && !poseActionDetected) || UseSourcePoseData) && eventData.SourceId == Controller?.InputSource.SourceId;
        }


        protected bool InputEventDataUsable<T>(InputEventData<T> eventData)
        {
            return !UseSourcePoseData && eventData.SourceId == Controller?.InputSource.SourceId && PoseAction == eventData.MixedRealityInputAction;
        }
        #endregion

        #region IMixedRealityControllerPoseSynchronizer Implementation

        /// <inheritdoc />
        public Handedness Handedness
        {
            get => Controller == null ? Handedness.None : Controller.ControllerHandedness;
        }

        [SerializeField]
        [Tooltip("Should this GameObject clean itself up when its controller is lost?")]
        private bool destroyOnSourceLost = true;

        /// <inheritdoc />
        public bool DestroyOnSourceLost
        {
            get => destroyOnSourceLost;
            set => destroyOnSourceLost = value;
        }

        /// <summary>
        /// Is the controller this Synchronizer is registered to currently tracked?
        /// </summary>
        public bool IsTracked => TrackingState == TrackingState.Tracked;

        /// <summary>
        /// The current tracking state of the assigned <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityController"/>
        /// </summary>
        protected TrackingState TrackingState = TrackingState.NotTracked;

        private IMixedRealityController controller;

        /// <inheritdoc />
        public virtual IMixedRealityController Controller
        {
            get => controller;
            set => controller = value;
        }

        [SerializeField]
        [Tooltip("Should the Transform's position be driven from the source pose or from input handler?")]
        private bool useSourcePoseData = true;

        /// <inheritdoc />
        public bool UseSourcePoseData
        {
            get => useSourcePoseData;
            set => useSourcePoseData = value;
        }

        [SerializeField]
        [Tooltip("Should the Transform's position use the source pose by default until the input handler events are received?")]
        private bool useSourcePoseAsFallback = true;

        /// <summary>
        /// Should the Transform's position use the source pose by default until the input handler events are received?
        /// </summary>
        public bool UseSourcePoseAsFallback
        {
            get => useSourcePoseAsFallback;
            set => useSourcePoseAsFallback = value;
        }

        /// <summary>
        /// Tracks whether or not a pose action event has been fired is actively being used by the pointer
        /// </summary>
        private bool poseActionDetected;

        [SerializeField]
        [Tooltip("The input action that will drive the Transform's pose, position, or rotation.")]
        private MixedRealityInputAction poseAction = MixedRealityInputAction.None;

        /// <inheritdoc />
        public MixedRealityInputAction PoseAction
        {
            get => poseAction;
            set => poseAction = value;
        }

        #endregion IMixedRealityControllerPoseSynchronizer Implementation

        #region InputSystemGlobalHandlerListener Implementation

        /// <inheritdoc />
        protected override void RegisterHandlers()
        {
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityControllerPoseSynchronizer>(this);
        }

        /// <inheritdoc />
        protected override void UnregisterHandlers()
        {
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityControllerPoseSynchronizer>(this);
        }

        #endregion InputSystemGlobalHandlerListener Implementation

        #region IMixedRealitySourcePoseHandler Implementation

        /// <inheritdoc />
        public virtual void OnSourceDetected(SourceStateEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnSourceLost(SourceStateEventData eventData)
        {
            if (eventData.SourceId == Controller?.InputSource.SourceId &&
                eventData.Controller?.ControllerHandedness == Handedness)
            {
                poseActionDetected = false;
                TrackingState = TrackingState.NotTracked;

                if (DestroyOnSourceLost)
                {
                    GameObjectExtensions.DestroyGameObject(gameObject);
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnSourcePoseChanged(SourcePoseEventData<TrackingState> eventData)
        {
            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                TrackingState = eventData.SourceData;
            }
        }

        /// <inheritdoc />
        public virtual void OnSourcePoseChanged(SourcePoseEventData<Vector2> eventData) { }

        /// <inheritdoc />
        public virtual void OnSourcePoseChanged(SourcePoseEventData<Vector3> eventData)
        {
            if (SourcePoseDataUsable(eventData))
            {
                TrackingState = eventData.Controller.TrackingState;
                transform.position = eventData.SourceData;
            }
        }

        /// <inheritdoc />
        public virtual void OnSourcePoseChanged(SourcePoseEventData<Quaternion> eventData)
        {
            if (SourcePoseDataUsable(eventData))
            {
                TrackingState = eventData.Controller.TrackingState;
                transform.rotation = eventData.SourceData;
            }
        }

        /// <inheritdoc />
        public virtual void OnSourcePoseChanged(SourcePoseEventData<MixedRealityPose> eventData)
        {
            if (SourcePoseDataUsable(eventData))
            {
                TrackingState = eventData.Controller.TrackingState;
                transform.position = eventData.SourceData.Position;
                transform.rotation = eventData.SourceData.Rotation;
            }
        }

        #endregion IMixedRealitySourcePoseHandler Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<float> eventData) { }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData) { }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector3> eventData)
        {
            if (InputEventDataUsable(eventData))
            {
                poseActionDetected = true;
                TrackingState = TrackingState.Tracked;
                transform.position = eventData.InputData;
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Quaternion> eventData)
        {
            if (InputEventDataUsable(eventData))
            {
                poseActionDetected = true;
                TrackingState = TrackingState.Tracked;
                transform.rotation = eventData.InputData;
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<MixedRealityPose> eventData)
        {
            if (InputEventDataUsable(eventData))
            {
                poseActionDetected = true;
                TrackingState = TrackingState.Tracked;
                transform.position = eventData.InputData.Position;
                transform.rotation = eventData.InputData.Rotation;
            }
        }

        #endregion  IMixedRealityInputHandler Implementation

        #region Obsolete

#pragma warning disable 0414
        [SerializeField]
        [HideInInspector]
        [System.Obsolete("Use the Handedness property instead to get current handedness which is set by Controller attached")]
        [Tooltip("Use the Handedness property instead to get current handedness which is set by Controller attached")]
        private Handedness handedness = Handedness.Left;
#pragma warning restore 0414

        #endregion

    }
}