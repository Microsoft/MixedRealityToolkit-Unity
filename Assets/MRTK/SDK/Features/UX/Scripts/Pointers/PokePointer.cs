// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.Profiling;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// A near interaction pointer that generates touch events based on touchables in close proximity.
    /// </summary>
    /// <remarks><format type="text/markdown">
    /// _Reachable Objects_ are objects with a both a [BaseNearInteractionTouchable](xref:Microsoft.MixedReality.Toolkit.Input.BaseNearInteractionTouchable) and a collider within [TouchableDistance](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer.TouchableDistance) from the poke pointer (based on [OverlapSphere](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphere.html)).
    ///
    /// If a poke pointer has no [CurrentTouchableObjectDown](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer.CurrentTouchableObjectDown), then it will try to select one from the Reachable Objects based on:
    /// 1. Layer mask priority: Lower-priority layer masks will only be considered if higher-priority layers don't contain any Reachable Objects.
    /// 1. Touchable Distance: the closest object in the highest priority layers is selected based on [DistanceToTouchable](xref:Microsoft.MixedReality.Toolkit.Input.BaseNearInteractionTouchable.DistanceToTouchable*).
    /// 1. Ray Distance: The object becomes the [CurrentTouchableObjectDown](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer.CurrentTouchableObjectDown) once the ray cast distance becomes negative (behind the surface). At this point the [OnTouchStarted](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealityTouchHandler.OnTouchStarted*) or [OnPointerDown](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealityPointerHandler.OnPointerDown*) event is raised.
    ///
    /// If a poke pointer _does_  have a [CurrentTouchableObjectDown](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer.CurrentTouchableObjectDown) it will not consider any other object, until the [DistanceToTouchable](xref:Microsoft.MixedReality.Toolkit.Input.BaseNearInteractionTouchable.DistanceToTouchable*) exceeds the [DebounceThreshold](xref:Microsoft.MixedReality.Toolkit.Input.BaseNearInteractionTouchable.DebounceThreshold) (in front of the surface). At this point the active object is cleared and the [OnTouchCompleted](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealityTouchHandler.OnTouchCompleted*) or [OnPointerUp](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealityPointerHandler.OnPointerUp*) event is raised.
    /// </format></remarks>
    [AddComponentMenu("Scripts/MRTK/SDK/PokePointer")]
    public class PokePointer : BaseControllerPointer, IMixedRealityNearPointer
    {
        /// <summary>
        /// If touchable volumes are larger than this size (meters), pointer will raise
        /// touch up even when pointer is inside the volume
        /// </summary>
        private const int maximumTouchableVolumeSize = 1000;

        /// <summary>
        /// The offset that the poke pointer has from the source pose when the index finger pose is not available.
        /// This value puts the pointer slightly in front of the source pose's origin, oriented according to the source pose's rotation
        /// </summary>
        private const float sourcePoseOffset = 0.075f;

        [SerializeField]
        [Tooltip("Maximum distance a which a touchable surface can be interacted with.")]
        protected float touchableDistance = 0.2f;
        /// <summary>
        /// Maximum distance a which a touchable surface can be interacted with.
        /// </summary>
        public float TouchableDistance => touchableDistance;

        [SerializeField]
        [Tooltip("Maximum number of colliders that can be detected in a scene query.")]
        [Min(1)]
        private int sceneQueryBufferSize = 64;
        /// <summary>
        /// Maximum number of colliders that can be detected in a scene query.
        /// </summary>
        public int SceneQueryBufferSize => sceneQueryBufferSize;

        [SerializeField]
        [Tooltip("Whether to ignore colliders that may be near the pointer, but not actually in the visual FOV. " +
            "This can prevent accidental touches, and will allow hand rays to turn on when you may be near a " +
            "touchable but cannot see it. Visual FOV is defined by cone centered about display center, " +
            "radius equal to half display height.")]
        private bool ignoreCollidersNotInFOV = true;

        /// <summary>
        /// Whether to ignore colliders that may be near the pointer, but not actually in the visual FOV.
        /// This can prevent accidental touches, and will allow hand rays to turn on when you may be near 
        /// a touchable but cannot see it. Visual FOV is defined by cone centered about display center, 
        /// radius equal to half display height.
        /// </summary>
        public bool IgnoreCollidersNotInFOV
        {
            get => ignoreCollidersNotInFOV;
            set => ignoreCollidersNotInFOV = value;
        }

        [SerializeField]
        [Tooltip("The LayerMasks, in prioritized order, that are used to determine the touchable objects.")]
        private LayerMask[] pokeLayerMasks = { UnityEngine.Physics.DefaultRaycastLayers };
        /// <summary>
        /// The LayerMasks, in prioritized order, that are used to determine the touchable objects.
        /// </summary>
        /// <remarks>
        /// Only [BaseNearInteractionTouchables](xref:Microsoft.MixedReality.Toolkit.Input.BaseNearInteractionTouchable) in one of the LayerMasks will raise touch events.
        /// </remarks>
        public LayerMask[] PokeLayerMasks => pokeLayerMasks;

        [SerializeField]
        [Tooltip("Specify whether queries for touchable surfaces hit triggers.")]
        protected QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        /// <summary>
        /// Specify whether queries for touchable surfaces hit triggers.
        /// </summary>
        public QueryTriggerInteraction TriggerInteraction => triggerInteraction;

        private Collider[] queryBuffer;

        private float closestDistance = 0.0f;

        private Vector3 closestNormal = Vector3.forward;

        private Vector3 endPoint;

        // previous frame pointer position
        public Vector3 PreviousPosition { get; private set; } = Vector3.zero;

        private BaseNearInteractionTouchable closestProximityTouchable = null;
        /// <summary>
        /// The closest touchable component that has been detected.
        /// </summary>
        /// <remarks>
        /// <para>The closest touchable component limits the set of objects which are currently touchable.
        /// These are all the game objects in the subtree of the closest touchable component's owner object.</para>
        /// </remarks>
        public BaseNearInteractionTouchable ClosestProximityTouchable => closestProximityTouchable;

        private GameObject currentTouchableObjectDown = null;
        /// <summary>
        /// The current object that is being touched.
        /// </summary>
        /// We need to make sure to consistently fire
        /// poke-down / poke-up events for this object. This is also the case when the object within
        /// the same current closest touchable component's changes (e.g. Unity UI control elements).
        public GameObject CurrentTouchableObjectDown => currentTouchableObjectDown;

        private void Awake()
        {
            queryBuffer = new Collider[sceneQueryBufferSize];
        }

        protected void OnValidate()
        {
            touchableDistance = Mathf.Max(touchableDistance, 0);
            sceneQueryBufferSize = Mathf.Max(sceneQueryBufferSize, 1);
        }

        /// <inheritdoc />
        public virtual bool IsNearObject
        {
            get => closestProximityTouchable != null;
        }

        /// <inheritdoc />
        public override bool IsInteractionEnabled => base.IsInteractionEnabled && IsNearObject;

        private static readonly ProfilerMarker OnPreSceneQueryPerfMarker = new ProfilerMarker("[MRTK] PokePointer.OnPreSceneQuery");

        public override void OnPreSceneQuery()
        {
            using (OnPreSceneQueryPerfMarker.Auto())
            {
                if (Rays == null)
                {
                    Rays = new RayStep[1];
                }

                closestNormal = Rotation * Vector3.forward;

                var layerMasks = PrioritizedLayerMasksOverride ?? PokeLayerMasks;

                // Find closest touchable
                BaseNearInteractionTouchable newClosestTouchable = null;
                foreach (var layerMask in layerMasks)
                {
                    if (FindClosestTouchableForLayerMask(layerMask, out newClosestTouchable, out closestDistance, out closestNormal))
                    {
                        break;
                    }
                }

                if (newClosestTouchable != null)
                {
                    // Build ray (poke from in front to the back of the pointer position)
                    // We make a very long ray if we are touching a touchable volume to ensure that we actually 
                    // hit the volume when we are inside of the volume, which could be very large.
                    var lengthOfPointerRay = newClosestTouchable is NearInteractionTouchableVolume ?
                        maximumTouchableVolumeSize : touchableDistance;
                    Vector3 start = Position + lengthOfPointerRay * closestNormal;
                    Vector3 end = Position - lengthOfPointerRay * closestNormal;
                    Rays[0].UpdateRayStep(ref start, ref end);
                }

                // Check if the currently touched object is still part of the new touchable.
                if (currentTouchableObjectDown != null)
                {
                    if (!IsObjectPartOfTouchable(currentTouchableObjectDown, newClosestTouchable))
                    {
                        TryRaisePokeUp();
                    }
                }

                // Set new touchable only now: If we have to raise a poke-up event for the previous touchable object,
                // we need to do so using the previous touchable in TryRaisePokeUp().
                closestProximityTouchable = newClosestTouchable;
            }
        }

        private static readonly ProfilerMarker FindClosestTouchableForLayerMaskPerfMarker = new ProfilerMarker("[MRTK] PokePointer.FindClosestTouchableForLayerMask");

        private bool FindClosestTouchableForLayerMask(LayerMask layerMask, out BaseNearInteractionTouchable closest, out float closestDistance, out Vector3 closestNormal)
        {
            using (FindClosestTouchableForLayerMaskPerfMarker.Auto())
            {
                closest = null;
                closestDistance = float.PositiveInfinity;
                closestNormal = Vector3.zero;

                int numColliders = UnityEngine.Physics.OverlapSphereNonAlloc(Position, touchableDistance, queryBuffer, layerMask, triggerInteraction);
                if (numColliders == queryBuffer.Length)
                {
                    Debug.LogWarning($"Maximum number of {numColliders} colliders found in PokePointer overlap query. Consider increasing the query buffer size in the input system settings.");
                }

                Camera mainCam = CameraCache.Main;
                for (int i = 0; i < numColliders; ++i)
                {
                    var collider = queryBuffer[i];
                    var touchable = collider.GetComponent<BaseNearInteractionTouchable>();
                    if (touchable)
                    {
                        if (IgnoreCollidersNotInFOV && !mainCam.IsInFOVCached(collider))
                        {
                            continue;
                        }
                        float distance = touchable.DistanceToTouchable(Position, out Vector3 normal);
                        if (distance < closestDistance)
                        {
                            closest = touchable;
                            closestDistance = distance;
                            closestNormal = normal;
                        }
                    }
                }

                // Unity UI does not provide an equivalent broad-phase test to Physics.OverlapSphere,
                // so we have to use a static instances list to test all NearInteractionTouchableUnityUI
                for (int i = 0; i < NearInteractionTouchableUnityUI.Instances.Count; i++)
                {
                    NearInteractionTouchableUnityUI touchable = NearInteractionTouchableUnityUI.Instances[i];
                    if (touchable.gameObject.IsInLayerMask(layerMask))
                    {
                        float distance = touchable.DistanceToTouchable(Position, out Vector3 normal);
                        if (distance <= touchableDistance && distance < closestDistance)
                        {
                            closest = touchable;
                            closestDistance = distance;
                            closestNormal = normal;
                        }
                    }
                }

                return closest != null;
            }
        }

        private static readonly ProfilerMarker OnPostSceneQueryPerfMarker = new ProfilerMarker("[MRTK] PokePointer.OnPostSceneQuery");

        /// <inheritdoc />
        public override void OnPostSceneQuery()
        {
            using (OnPostSceneQueryPerfMarker.Auto())
            {
                base.OnPostSceneQuery();

                if (!IsActive)
                {
                    return;
                }

                if (Result?.CurrentPointerTarget != null && closestProximityTouchable != null)
                {
                    float distToTouchable;
                    if (closestProximityTouchable is NearInteractionTouchableVolume)
                    {
                        // Volumes can be arbitrary size, so don't rely on the length of the raycast ray
                        // instead just have the volume itself give us the distance.
                        distToTouchable = closestProximityTouchable.DistanceToTouchable(Position, out _);
                    }
                    else
                    {
                        // Start position of the ray is offset by TouchableDistance, subtract to get distance between surface and pointer position.
                        distToTouchable = Vector3.Distance(Result.StartPoint, Result.Details.Point) - touchableDistance;
                    }

                    bool newIsDown = (distToTouchable < 0.0f);
                    bool newIsUp = (distToTouchable > closestProximityTouchable.DebounceThreshold);

                    if (newIsDown)
                    {
                        TryRaisePokeDown();
                    }
                    else if (currentTouchableObjectDown != null)
                    {
                        if (newIsUp)
                        {
                            TryRaisePokeUp();
                        }
                        else
                        {
                            TryRaisePokeDown();
                        }
                    }
                }

                PreviousPosition = Position;
            }
        }

        private static readonly ProfilerMarker OnPreCurrentPointerTargetChangePerfMarker = new ProfilerMarker("[MRTK] PokePointer.OnPreCurrentPointerTargetChange");

        /// <inheritdoc />
        public override void OnPreCurrentPointerTargetChange()
        {
            using (OnPreCurrentPointerTargetChangePerfMarker.Auto())
            {
                // We need to raise the event now, since the pointer's focused object or touchable will change
                // after we leave this function. This will make sure the same object that received the Down event
                // will also receive the Up event.
                TryRaisePokeUp();
            }
        }

        private static readonly ProfilerMarker TryRaisePokeDownPerfMarker = new ProfilerMarker("[MRTK] PokePointer.TryRaisePokeDown");

        private void TryRaisePokeDown()
        {
            using (TryRaisePokeDownPerfMarker.Auto())
            {
                GameObject targetObject = Result.CurrentPointerTarget;

                if (currentTouchableObjectDown == null)
                {
                    // In order to get reliable up/down event behavior, only allow the closest touchable to be touched.
                    if (IsObjectPartOfTouchable(targetObject, closestProximityTouchable))
                    {
                        currentTouchableObjectDown = targetObject;

                        if (closestProximityTouchable.EventsToReceive == TouchableEventType.Pointer)
                        {
                            CoreServices.InputSystem?.RaisePointerDown(this, pointerAction, Handedness);
                        }
                        else if (closestProximityTouchable.EventsToReceive == TouchableEventType.Touch)
                        {
                            CoreServices.InputSystem?.RaiseOnTouchStarted(InputSourceParent, Controller, Handedness, Position);
                        }
                    }
                }
                else
                {
                    RaiseTouchUpdated(targetObject, Position);
                }
            }
        }

        private static readonly ProfilerMarker TryRaisePokeUpPerfMarker = new ProfilerMarker("[MRTK] PokePointer.TryRaisePokeUp");

        private void TryRaisePokeUp()
        {
            using (TryRaisePokeUpPerfMarker.Auto())
            {
                if (currentTouchableObjectDown != null)
                {
                    Debug.Assert(Result.CurrentPointerTarget == currentTouchableObjectDown, "PokeUp will not be raised for correct object.");

                    if (closestProximityTouchable.EventsToReceive == TouchableEventType.Pointer)
                    {
                        CoreServices.InputSystem.RaisePointerClicked(this, pointerAction, 0, Handedness);
                        CoreServices.InputSystem?.RaisePointerUp(this, pointerAction, Handedness);
                    }
                    else if (closestProximityTouchable.EventsToReceive == TouchableEventType.Touch)
                    {
                        CoreServices.InputSystem?.RaiseOnTouchCompleted(InputSourceParent, Controller, Handedness, Position);
                    }

                    currentTouchableObjectDown = null;
                }
            }
        }

        private static readonly ProfilerMarker RaiseTouchUpdatedPerfMarker = new ProfilerMarker("[MRTK] PokePointer.RaiseTouchUpdated");

        private void RaiseTouchUpdated(GameObject targetObject, Vector3 touchPosition)
        {
            using (RaiseTouchUpdatedPerfMarker.Auto())
            {
                if (currentTouchableObjectDown != null)
                {
                    Debug.Assert(Result?.CurrentPointerTarget == currentTouchableObjectDown);

                    if (closestProximityTouchable.EventsToReceive == TouchableEventType.Touch)
                    {
                        CoreServices.InputSystem?.RaiseOnTouchUpdated(InputSourceParent, Controller, Handedness, touchPosition);
                    }
                    else if (closestProximityTouchable.EventsToReceive == TouchableEventType.Pointer)
                    {
                        CoreServices.InputSystem?.RaisePointerDragged(this, pointerAction, Handedness, InputSourceParent);
                    }
                }
            }
        }

        private static readonly ProfilerMarker IsObjectPartOfTouchablePerfMarker = new ProfilerMarker("[MRTK] PokePointer.IsObjectPartOfTouchable");

        private static bool IsObjectPartOfTouchable(GameObject targetObject, BaseNearInteractionTouchable touchable)
        {
            using (IsObjectPartOfTouchablePerfMarker.Auto())
            {
                return targetObject != null && touchable != null &&
                    (targetObject == touchable.gameObject ||
                    // Descendant game objects are touchable as well. In particular, this is needed to be able to send
                    // touch events to Unity UI control elements.
                    (targetObject.transform != null && touchable.gameObject.transform != null &&
                    targetObject.transform.IsChildOf(touchable.gameObject.transform)));
            }
        }

        /// <inheritdoc />
        bool IMixedRealityNearPointer.TryGetNearGraspPoint(out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }

        /// <inheritdoc />
        bool IMixedRealityNearPointer.TryGetDistanceToNearestSurface(out float distance)
        {
            distance = closestDistance;
            return true;
        }

        /// <inheritdoc />
        bool IMixedRealityNearPointer.TryGetNormalToNearestSurface(out Vector3 normal)
        {
            normal = closestNormal;
            return true;
        }

        private static readonly ProfilerMarker OnSourceLostPerfMarker = new ProfilerMarker("[MRTK] PokePointer.OnSourceLost");

        /// <inheritdoc />
        public override void OnSourceLost(SourceStateEventData eventData)
        {
            using (OnSourceLostPerfMarker.Auto())
            {
                TryRaisePokeUp();

                base.OnSourceLost(eventData);
            }
        }

        private static readonly ProfilerMarker OnSourceDetectedPerfMarker = new ProfilerMarker("[MRTK] PokePointer.OnSourceDetected");

        /// <inheritdoc />
        public override void OnSourceDetected(SourceStateEventData eventData)
        {
            using (OnSourceDetectedPerfMarker.Auto())
            {
                base.OnSourceDetected(eventData);
                PreviousPosition = Position;
            }
        }

        /// <inheritdoc />
        public override void OnSourcePoseChanged(SourcePoseEventData<MixedRealityPose> eventData)
        {
            base.OnSourcePoseChanged(eventData);

            if (SourcePoseDataUsable(eventData))
            {
                transform.position += sourcePoseOffset * transform.forward;
            }
        }

        /// <inheritdoc />
        public override void OnInputDown(InputEventData eventData)
        {
            // Poke pointer should not respond when a button is pressed or hand is pinched
            // It should only dispatch events based on collision with touchables.
        }

        /// <inheritdoc />
        public override void OnInputUp(InputEventData eventData)
        {
            // Poke pointer should not respond when a button is released or hand is un-pinched
            // It should only dispatch events based on collision with touchables.
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            IsTargetPositionLockedOnFocusLock = false;
        }

        private void OnDrawGizmos()
        {
            if (!IsNearObject)
            {
                return;
            }
            else
            {
                Gizmos.color = Color.green;
            }

            if (closestProximityTouchable != null)
            {
                Gizmos.DrawLine(Position, closestProximityTouchable.transform.position);
            }
        }
    }
}
