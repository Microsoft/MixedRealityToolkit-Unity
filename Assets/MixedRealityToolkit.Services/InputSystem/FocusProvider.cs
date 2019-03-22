﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Extensions;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Utilities.Physics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Services.InputSystem
{
    /// <summary>
    /// The focus provider handles the focused objects per input source.
    /// <remarks>There are convenience properties for getting only Gaze Pointer if needed.</remarks>
    /// </summary>
    public class FocusProvider : BaseDataProvider, IMixedRealityFocusProvider
    {
        public FocusProvider(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            MixedRealityInputSystemProfile profile) : base(registrar, inputSystem, null, DefaultPriority, profile)
        { }

        private readonly HashSet<PointerData> pointers = new HashSet<PointerData>();
        private readonly Dictionary<GameObject, HashSet<IMixedRealityPointer>> overallFocusSet = new Dictionary<GameObject, HashSet<IMixedRealityPointer>>();


        #region IFocusProvider Properties

        /// <inheritdoc />
        public override string Name => "Focus Provider";

        /// <inheritdoc />
        public override uint Priority => 2;

        /// <inheritdoc />
        float IMixedRealityFocusProvider.GlobalPointingExtent
        {
            get
            {
                MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;
                
                if ((Service != null) &&
                    (profile != null) &&
                    profile.PointerProfile != null)
                {
                    return profile.PointerProfile.PointingExtent;
                }

                return 10f;
            }
        }

        private LayerMask[] focusLayerMasks = null;

        /// <inheritdoc />
        public LayerMask[] FocusLayerMasks
        {
            get
            {
                if (focusLayerMasks == null)
                {
                    MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;

                    if ((Service != null) &&
                        (profile != null) &&
                        profile.PointerProfile != null)
                    {
                        return focusLayerMasks = profile.PointerProfile.PointingRaycastLayerMasks;
                    }

                    return focusLayerMasks = new LayerMask[] { Physics.DefaultRaycastLayers };
                }

                return focusLayerMasks;
            }
        }

        private Camera uiRaycastCamera = null;

        /// <inheritdoc />
        public Camera UIRaycastCamera
        {
            get
            {
                if (uiRaycastCamera == null)
                {
                    EnsureUiRaycastCameraSetup();
                }

                return uiRaycastCamera;
            }
        }

        /// <inheritdoc />
        public GameObject OverrideFocusedObject { get; set; }
       
        /// <inheritdoc />
        public bool IsOnlyFocusingPointer(GameObject focusedObject, IMixedRealityPointer pointer)
        {
            return overallFocusSet.ContainsKey(focusedObject) && overallFocusSet[focusedObject].Count == 1 && overallFocusSet[focusedObject].Contains(pointer);
        }

        #endregion IFocusProvider Properties

        /// <summary>
        /// Checks if the <see cref="MixedRealityToolkit"/> is setup correctly to start this service.
        /// </summary>
        /// <returns></returns>
        private bool IsSetupValid
        {
            get
            {
                if (Service == null)
                {
                    Debug.LogError($"Unable to start {Name}. An Input System is required for this feature.");
                    return false;
                }

                MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;

                if (profile == null)
                {
                    Debug.LogError($"Unable to start {Name}. An Input System Profile is required for this feature.");
                    return false;
                }

                if (profile.PointerProfile == null)
                {
                    Debug.LogError($"Unable to start {Name}. An Pointer Profile is required for this feature.");
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// GazeProvider is a little special, so we keep track of it even if it's not a registered pointer. For the sake
        /// of StabilizationPlaneModifier and potentially other components that care where the user's looking, we need
        /// to do a gaze raycast even if gaze isn't used for focus.
        /// </summary>
        private PointerData gazeProviderPointingData;

        /// <summary>
        /// Cached <see href="https://docs.unity3d.com/ScriptReference/Vector3.html">Vector3</see> reference to the new raycast position.
        /// <remarks>Only used to update UI raycast results.</remarks>
        /// </summary>
        private Vector3 newUiRaycastPosition = Vector3.zero;

        [Serializable]
        private class PointerData : IPointerResult, IEquatable<PointerData>
        {
            public readonly IMixedRealityPointer Pointer;

            /// <inheritdoc />
            public Vector3 StartPoint { get; private set; }

            /// <inheritdoc />
            public FocusDetails Details => focusDetails;

            /// <inheritdoc />
            public GameObject CurrentPointerTarget => focusDetails.Object;

            /// <inheritdoc />
            public GameObject PreviousPointerTarget { get; private set; }

            /// <inheritdoc />
            public int RayStepIndex { get; private set; }

            /// <summary>
            /// The graphic input event data used for raycasting uGUI elements.
            /// </summary>
            public GraphicInputEventData GraphicEventData
            {
                get
                {
                    if (graphicData == null)
                    {
                        graphicData = new GraphicInputEventData(EventSystem.current);
                    }

                    Debug.Assert(graphicData != null);

                    return graphicData;
                }
            }
            private GraphicInputEventData graphicData;

            private FocusDetails focusDetails = new FocusDetails();
            private Vector3 pointLocalSpace;
            private Vector3 normalLocalSpace;
            private bool pointerWasLocked;

            public PointerData(IMixedRealityPointer pointer)
            {
                Pointer = pointer;
            }

            /// <summary>
            /// Update focus information from a physics raycast
            /// </summary>
            public void UpdateHit(RaycastHit hit, RayStep sourceRay, int rayStepIndex)
            {
                PreviousPointerTarget = Details.Object;
                RayStepIndex = rayStepIndex;
                StartPoint = sourceRay.Origin;

                focusDetails.LastRaycastHit = hit;
                focusDetails.Point = hit.point;
                focusDetails.Normal = hit.normal;
                focusDetails.Object = hit.transform.gameObject;

                pointerWasLocked = false;
            }

            /// <summary>
            /// Update focus information from a Canvas raycast 
            /// </summary>
            public void UpdateHit(RaycastResult result, RaycastHit hit, RayStep sourceRay, int rayStepIndex)
            {
                // We do not update the PreviousPointerTarget here because
                // it's already been updated in the first physics raycast.

                RayStepIndex = rayStepIndex;
                StartPoint = sourceRay.Origin;

                focusDetails.Point = hit.point;
                focusDetails.Normal = hit.normal;
                focusDetails.Object = result.gameObject;
            }

            public void UpdateHit()
            {
                PreviousPointerTarget = Details.Object;

                RayStep firstStep = Pointer.Rays[0];
                RayStep finalStep = Pointer.Rays[Pointer.Rays.Length - 1];
                RayStepIndex = 0;

                StartPoint = firstStep.Origin;

                focusDetails.Point = finalStep.Terminus;
                focusDetails.Normal = -finalStep.Direction;
                focusDetails.Object = null;

                pointerWasLocked = false;
            }

            /// <summary>
            /// Update focus information while focus is locked. If the object is moving,
            /// this updates the hit point to its new world transform.
            /// </summary>
            public void UpdateFocusLockedHit()
            {
                if (!pointerWasLocked)
                {
                    PreviousPointerTarget = focusDetails.Object;
                    pointLocalSpace = focusDetails.Object.transform.InverseTransformPoint(focusDetails.Point);
                    normalLocalSpace = focusDetails.Object.transform.InverseTransformDirection(focusDetails.Normal);
                    pointerWasLocked = true;
                }

                if (focusDetails.Object != null && focusDetails.Object.transform != null)
                {
                    // In case the focused object is moving, we need to update the focus point based on the object's new transform.
                    focusDetails.Point = focusDetails.Object.transform.TransformPoint(pointLocalSpace);
                    focusDetails.Normal = focusDetails.Object.transform.TransformDirection(normalLocalSpace);
                }

                StartPoint = Pointer.Rays[0].Origin;

                for (int i = 0; i < Pointer.Rays.Length; i++)
                {
                    if (Pointer.Rays[i].Contains(focusDetails.Point))
                    {
                        RayStepIndex = i;
                        break;
                    }
                }
            }

            public void ResetFocusedObjects(bool clearPreviousObject = true)
            {
                PreviousPointerTarget = clearPreviousObject ? null : CurrentPointerTarget;

                focusDetails.Point = Details.Point;
                focusDetails.Normal = Details.Normal;
                focusDetails.Object = null;
            }

            /// <inheritdoc />
            public bool Equals(PointerData other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Pointer.PointerId == other.Pointer.PointerId;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((PointerData)obj);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Pointer != null ? Pointer.GetHashCode() : 0;
            }
        }

        #region IMixedRealityService Implementation

        /// <inheritdoc />
        public override void Initialize()
        {
            if (!IsSetupValid) { return; }

            foreach (var inputSource in MixedRealityToolkit.InputSystem.DetectedInputSources)
            {
                RegisterPointers(inputSource);
            }

            if (Application.isEditor && !Application.isPlaying)
            {
                UpdateCanvasEventSystems();
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (!IsSetupValid) { return; }

            UpdatePointers();
            UpdateFocusedObjects();
        }

        public override void Destroy()
        {
            if (uiRaycastCamera != null)
            {
                if (Application.isEditor)
                {
                    UnityEngine.Object.DestroyImmediate(uiRaycastCamera.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(uiRaycastCamera.gameObject);
                }
            }
        }

        #endregion IMixedRealityService Implementation

        #region Focus Details by IMixedRealityPointer

        /// <inheritdoc />
        public GameObject GetFocusedObject(IMixedRealityPointer pointingSource)
        {
            if (OverrideFocusedObject != null) { return OverrideFocusedObject; }

            if (pointingSource == null)
            {
                Debug.LogError("No Pointer passed to get focused object");
                return null;
            }

            FocusDetails focusDetails;
            if (!TryGetFocusDetails(pointingSource, out focusDetails)) { return null; }

            return focusDetails.Object;
        }

        /// <inheritdoc />
        public bool TryGetFocusDetails(IMixedRealityPointer pointer, out FocusDetails focusDetails)
        {
            PointerData pointerData;
            if (TryGetPointerData(pointer, out pointerData))
            {
                focusDetails = pointerData.Details;
                return true;
            }

            focusDetails = default(FocusDetails);
            return false;
        }

        /// <inheritdoc />
        public bool TryGetSpecificPointerGraphicEventData(IMixedRealityPointer pointer, out GraphicInputEventData graphicInputEventData)
        {
            PointerData pointerData;
            if (TryGetPointerData(pointer, out pointerData))
            {
                graphicInputEventData = pointerData.GraphicEventData;
                graphicInputEventData.selectedObject = pointerData.GraphicEventData.pointerCurrentRaycast.gameObject;
                return true;
            }

            graphicInputEventData = null;
            return false;
        }

        #endregion Focus Details by IMixedRealityPointer

        #region Utilities

        /// <inheritdoc />
        public uint GenerateNewPointerId()
        {
            var newId = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            foreach (var pointerData in pointers)
            {
                if (pointerData.Pointer.PointerId == newId)
                {
                    return GenerateNewPointerId();
                }
            }

            return newId;
        }

        /// <summary>
        /// Utility for validating the UIRaycastCamera.
        /// </summary>
        /// <returns>The UIRaycastCamera</returns>
        private void EnsureUiRaycastCameraSetup()
        {
            Transform cameraTransform = CameraCache.Main.transform.Find("UIRaycastCamera");
            GameObject cameraObject;

            if (cameraTransform == null)
            {
                cameraObject = new GameObject { name = "UIRaycastCamera" };
                cameraObject.transform.parent = CameraCache.Main.transform;
            }
            else
            {
                cameraObject = cameraTransform.gameObject;
                Debug.Assert(cameraObject.transform.parent == CameraCache.Main.transform);
            }

            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;

            // The raycast camera is used to raycast into the UI scene,
            // it doesn't need to render anything so is disabled
            // The default settings are all that is necessary 
            uiRaycastCamera = cameraObject.EnsureComponent<Camera>();
            uiRaycastCamera.enabled = false;
        }

        /// <summary>
        /// Helper for assigning world space canvases event cameras.
        /// </summary>
        /// <remarks>Warning! Very expensive. Use sparingly at runtime.</remarks>
        public void UpdateCanvasEventSystems()
        {
            Debug.Assert(UIRaycastCamera != null, "You must assign a UIRaycastCamera on the FocusProvider before updating your canvases.");

            // This will also find disabled GameObjects in the scene.
            // Warning! this look up is very expensive!
            var sceneCanvases = Resources.FindObjectsOfTypeAll<Canvas>();

            for (var i = 0; i < sceneCanvases.Length; i++)
            {
                if (sceneCanvases[i].isRootCanvas && sceneCanvases[i].renderMode == RenderMode.WorldSpace)
                {
                    sceneCanvases[i].worldCamera = UIRaycastCamera;
                }
            }
        }

        /// <inheritdoc />
        public bool IsPointerRegistered(IMixedRealityPointer pointer)
        {
            Debug.Assert(pointer.PointerId != 0, $"{pointer} does not have a valid pointer id!");
            PointerData pointerData;
            return TryGetPointerData(pointer, out pointerData);
        }

        /// <inheritdoc />
        public bool RegisterPointer(IMixedRealityPointer pointer)
        {
            Debug.Assert(pointer.PointerId != 0, $"{pointer} does not have a valid pointer id!");

            if (IsPointerRegistered(pointer)) { return false; }

            pointers.Add(new PointerData(pointer));
            return true;
        }

        private void RegisterPointers(IMixedRealityInputSource inputSource)
        {
            // If our input source does not have any pointers, then skip.
            if (inputSource.Pointers == null) { return; }

            for (int i = 0; i < inputSource.Pointers.Length; i++)
            {
                RegisterPointer(inputSource.Pointers[i]);

                // Special Registration for Gaze
                if (inputSource.SourceId == MixedRealityToolkit.InputSystem.GazeProvider.GazeInputSource.SourceId && gazeProviderPointingData == null)
                {
                    gazeProviderPointingData = new PointerData(inputSource.Pointers[i]);
                }
            }
        }

        /// <inheritdoc />
        public bool UnregisterPointer(IMixedRealityPointer pointer)
        {
            Debug.Assert(pointer.PointerId != 0, $"{pointer} does not have a valid pointer id!");

            PointerData pointerData;
            if (!TryGetPointerData(pointer, out pointerData)) { return false; }

            // Raise focus events if needed.
            if (pointerData.CurrentPointerTarget != null)
            {
                GameObject unfocusedObject = pointerData.CurrentPointerTarget;
                
                MixedRealityToolkit.InputSystem.RaisePreFocusChanged(pointer, unfocusedObject, null);

                MixedRealityToolkit.InputSystem.RaiseFocusExit(pointer, unfocusedObject);
                overallFocusSet.Remove(unfocusedObject, pointer);

                MixedRealityToolkit.InputSystem.RaiseFocusChanged(pointer, unfocusedObject, null);
            }

            pointers.Remove(pointerData);
            return true;
        }

        /// <summary>
        /// Returns the registered PointerData for the provided pointing input source.
        /// </summary>
        /// <param name="pointer">the pointer who's data we're looking for</param>
        /// <param name="data">The data associated to the pointer</param>
        /// <returns>Pointer Data if the pointing source is registered.</returns>
        private bool TryGetPointerData(IMixedRealityPointer pointer, out PointerData data)
        {
            foreach (var pointerData in pointers)
            {
                if (pointerData.Pointer.PointerId == pointer.PointerId)
                {
                    data = pointerData;
                    return true;
                }
            }

            data = null;
            return false;
        }

        private void UpdatePointers()
        {
            int pointerCount = 0;

            MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;
            if (profile == null) { return; }

            foreach (var pointer in pointers)
            {
                UpdatePointer(pointer);

                var pointerProfile = profile.PointerProfile;

                if (pointerProfile != null && pointerProfile.DebugDrawPointingRays)
                {
                    MixedRealityRaycaster.DebugEnabled = pointerProfile.DebugDrawPointingRays;

                    Color rayColor;

                    if ((pointerProfile.DebugDrawPointingRayColors != null) && (pointerProfile.DebugDrawPointingRayColors.Length > 0))
                    {
                        rayColor = pointerProfile.DebugDrawPointingRayColors[pointerCount++ % pointerProfile.DebugDrawPointingRayColors.Length];
                    }
                    else
                    {
                        rayColor = Color.green;
                    }

                    Debug.DrawRay(pointer.StartPoint, (pointer.Details.Point - pointer.StartPoint), rayColor);
                }
            }
        }

        private void UpdatePointer(PointerData pointer)
        {
            // Call the pointer's OnPreRaycast function
            // This will give it a chance to prepare itself for raycasts
            // eg, by building its Rays array
            pointer.Pointer.OnPreRaycast();

            // If pointer interaction isn't enabled, clear its result object and return
            if (!pointer.Pointer.IsInteractionEnabled)
            {
                // Don't clear the previous focused object since we still want to trigger FocusExit events
                pointer.ResetFocusedObjects(false);
            }
            else
            {
                // If the pointer is locked
                // Keep the focus objects the same
                // This will ensure that we execute events on those objects
                // even if the pointer isn't pointing at them
                if (!pointer.Pointer.IsFocusLocked)
                {
                    // Otherwise, continue
                    LayerMask[] prioritizedLayerMasks = (pointer.Pointer.PrioritizedLayerMasksOverride ?? FocusLayerMasks);

                    // Perform raycast to determine focused object
                    RaycastPhysics(pointer, prioritizedLayerMasks);

                    // If we have a unity event system, perform graphics raycasts as well to support Unity UI interactions
                    if (EventSystem.current != null)
                    {
                        // NOTE: We need to do this AFTER RaycastPhysics so we use the current hit point to perform the correct 2D UI Raycast.
                        RaycastGraphics(pointer, prioritizedLayerMasks);
                    }

                    // Set the pointer's result last
                    pointer.Pointer.Result = pointer;
                }
                else
                {
                    pointer.UpdateFocusLockedHit();
                }
            }

            // Call the pointer's OnPostRaycast function
            // This will give it a chance to respond to raycast results
            // eg by updating its appearance
            pointer.Pointer.OnPostRaycast();
        }

        #region Physics Raycasting

        /// <summary>
        /// Perform a Unity physics Raycast to determine which scene objects with a collider is currently being gazed at, if any.
        /// </summary>
        /// <param name="pointerData"></param>
        /// <param name="prioritizedLayerMasks"></param>
        private static void RaycastPhysics(PointerData pointerData, LayerMask[] prioritizedLayerMasks)
        {
            RaycastHit physicsHit;
            RayStep[] pointerRays = pointerData.Pointer.Rays;

            if (pointerRays == null)
            {
                Debug.LogError($"No valid rays for {pointerData.Pointer.PointerName} pointer.");
                return;
            }

            if (pointerRays.Length <= 0)
            {
                Debug.LogError($"No valid rays for {pointerData.Pointer.PointerName} pointer");
                return;
            }

            // Check raycast for each step in the pointing source
            for (int i = 0; i < pointerRays.Length; i++)
            {
                switch (pointerData.Pointer.RaycastMode)
                {
                    case RaycastMode.Simple:
                        if (MixedRealityRaycaster.RaycastSimplePhysicsStep(pointerRays[i], prioritizedLayerMasks, out physicsHit))
                        {
                            UpdatePointerRayOnHit(pointerData, pointerRays, in physicsHit, i);  
                            return;
                        }
                        break;
                    case RaycastMode.Sphere:
                        if (MixedRealityRaycaster.RaycastSpherePhysicsStep(pointerRays[i], pointerData.Pointer.SphereCastRadius, prioritizedLayerMasks, out physicsHit))
                        {
                            UpdatePointerRayOnHit(pointerData, pointerRays, in physicsHit, i);
                            return;
                        }
                        break;
                    case RaycastMode.Box:
                        Debug.LogWarning("Box Raycasting Mode not supported for pointers.");
                        break;
                    default:
                        Debug.LogError($"Invalid raycast mode {pointerData.Pointer.RaycastMode} for {pointerData.Pointer.PointerName} pointer.");
                        break;
                }
            }

            pointerData.UpdateHit();
        }

        private static void UpdatePointerRayOnHit(PointerData pointerData, RayStep[] raySteps, in RaycastHit physicsHit, int hitRayIndex)
        {
            Vector3 origin = raySteps[hitRayIndex].Origin;
            Vector3 terminus = physicsHit.point;
            raySteps[hitRayIndex].UpdateRayStep(ref origin, ref terminus);
            pointerData.UpdateHit(physicsHit, raySteps[hitRayIndex], hitRayIndex);
        }

        #endregion Physics Raycasting

        #region uGUI Graphics Raycasting

        /// <summary>
        /// Perform a Unity Graphics Raycast to determine which uGUI element is currently being gazed at, if any.
        /// </summary>
        /// <param name="pointerData"></param>
        /// <param name="prioritizedLayerMasks"></param>
        private void RaycastGraphics(PointerData pointerData, LayerMask[] prioritizedLayerMasks)
        {
            Debug.Assert(UIRaycastCamera != null, "Missing UIRaycastCamera!");

            RaycastResult raycastResult = default(RaycastResult);
            bool overridePhysicsRaycast = false;
            RayStep rayStep = default(RayStep);
            int rayStepIndex = 0;

            if (pointerData.Pointer.Rays == null)
            {
                Debug.LogError($"No valid rays for {pointerData.Pointer.PointerName} pointer.");
                return;
            }

            if (pointerData.Pointer.Rays.Length <= 0)
            {
                Debug.LogError($"No valid rays for {pointerData.Pointer.PointerName} pointer");
                return;
            }

            // Cast rays for every step until we score a hit
            for (int i = 0; i < pointerData.Pointer.Rays.Length; i++)
            {
                if (RaycastGraphicsStep(pointerData, pointerData.Pointer.Rays[i], prioritizedLayerMasks, out overridePhysicsRaycast, out raycastResult))
                {
                    rayStepIndex = i;
                    rayStep = pointerData.Pointer.Rays[i];
                    break;
                }
            }

            // Check if we need to overwrite the physics raycast info
            if ((pointerData.CurrentPointerTarget == null || overridePhysicsRaycast) && raycastResult.isValid &&
                 raycastResult.module != null && raycastResult.module.eventCamera == UIRaycastCamera)
            {
                newUiRaycastPosition.x = raycastResult.screenPosition.x;
                newUiRaycastPosition.y = raycastResult.screenPosition.y;
                newUiRaycastPosition.z = raycastResult.distance;

                Vector3 worldPos = UIRaycastCamera.ScreenToWorldPoint(newUiRaycastPosition);

                var hitInfo = new RaycastHit
                {
                    point = worldPos,
                    normal = -raycastResult.gameObject.transform.forward
                };

                pointerData.UpdateHit(raycastResult, hitInfo, rayStep, rayStepIndex);
            }
        }

        /// <summary>
        /// Raycasts each graphic <see cref="Microsoft.MixedReality.Toolkit.Core.Definitions.Physics.RayStep"/>
        /// </summary>
        /// <param name="pointerData"></param>
        /// <param name="step"></param>
        /// <param name="prioritizedLayerMasks"></param>
        /// <param name="overridePhysicsRaycast"></param>
        /// <param name="uiRaycastResult"></param>
        /// <returns></returns>
        private bool RaycastGraphicsStep(PointerData pointerData, RayStep step, LayerMask[] prioritizedLayerMasks, out bool overridePhysicsRaycast, out RaycastResult uiRaycastResult)
        {
            Debug.Assert(step.Direction != Vector3.zero, "RayStep Direction is Invalid.");

            // Move the uiRaycast camera to the current pointer's position.
            UIRaycastCamera.transform.position = step.Origin;
            UIRaycastCamera.transform.forward = step.Direction;

            // We always raycast from the center of the camera.
            pointerData.GraphicEventData.position = new Vector2(UIRaycastCamera.pixelWidth * 0.5f, UIRaycastCamera.pixelHeight * 0.5f);

            // Graphics raycast
            uiRaycastResult = EventSystem.current.Raycast(pointerData.GraphicEventData, prioritizedLayerMasks);
            pointerData.GraphicEventData.pointerCurrentRaycast = uiRaycastResult;

            overridePhysicsRaycast = false;

            // If we have a raycast result, check if we need to overwrite the physics raycast info
            if (uiRaycastResult.gameObject != null)
            {
                if (pointerData.CurrentPointerTarget != null)
                {
                    float distance = 0f;
                    for (int i = 0; i <= pointerData.RayStepIndex; i++) {
                        distance += pointerData.Pointer.Rays[i].Length;
                    }
                    
                    // Check layer prioritization
                    if (prioritizedLayerMasks.Length > 1)
                    {
                        // Get the index in the prioritized layer masks
                        int uiLayerIndex = uiRaycastResult.gameObject.layer.FindLayerListIndex(prioritizedLayerMasks);
                        int threeDLayerIndex = pointerData.Details.LastRaycastHit.collider.gameObject.layer.FindLayerListIndex(prioritizedLayerMasks);

                        if (threeDLayerIndex > uiLayerIndex)
                        {
                            overridePhysicsRaycast = true;
                        }
                        else if (threeDLayerIndex == uiLayerIndex)
                        {
                            if (distance > uiRaycastResult.distance)
                            {
                                overridePhysicsRaycast = true;
                            }
                        }
                    }
                    else
                    {
                        if (distance > uiRaycastResult.distance)
                        {
                            overridePhysicsRaycast = true;
                        }
                    }
                }
                // If we've hit something, no need to go further
                return true;
            }
            // If we haven't hit something, keep going
            return false;
        }

        #endregion uGUI Graphics Raycasting

        /// <summary>
        /// Raises the Focus Events to the Input Manger if needed.
        /// </summary>
        private void UpdateFocusedObjects()
        {
            // Collect all pointers that have their target changed:
            foreach (var pointer in pointers)
            {
                IMixedRealityPointer currentPointer = pointer.Pointer;
                GameObject pendingUnfocusObject = pointer.PreviousPointerTarget;
                GameObject pendingFocusObject = pointer.CurrentPointerTarget;

                if (pendingUnfocusObject != pendingFocusObject)
                {
                    MixedRealityToolkit.InputSystem.RaisePreFocusChanged(currentPointer, pendingUnfocusObject, pendingFocusObject);

                    if (pendingUnfocusObject != null)
                    {
                        MixedRealityToolkit.InputSystem.RaiseFocusExit(currentPointer, pendingUnfocusObject);
                        overallFocusSet.Remove(pendingUnfocusObject, currentPointer);
                    }

                    if (pendingFocusObject != null)
                    {
                        overallFocusSet.Add(pendingFocusObject, currentPointer);
                        MixedRealityToolkit.InputSystem.RaiseFocusEnter(currentPointer, pendingFocusObject);
                    }

                    MixedRealityToolkit.InputSystem.RaiseFocusChanged(currentPointer, pendingUnfocusObject, pendingFocusObject);
                }
            }
        }

        #endregion Accessors

        #region ISourceState Implementation

        /// <inheritdoc />
        public void OnSourceDetected(SourceStateEventData eventData)
        {
            RegisterPointers(eventData.InputSource);
        }

        /// <inheritdoc />
        public void OnSourceLost(SourceStateEventData eventData)
        {
            // If the input source does not have pointers, then skip.
            if (eventData.InputSource.Pointers == null) { return; }

            for (var i = 0; i < eventData.InputSource.Pointers.Length; i++)
            {
                // Special unregistration for Gaze
                if (gazeProviderPointingData != null && eventData.InputSource.Pointers[i].PointerId == gazeProviderPointingData.Pointer.PointerId)
                {
                    // If the source lost is the gaze input source, then reset it.
                    if (eventData.InputSource.SourceId == ((IMixedRealityInputSystem)Service).GazeProvider?.GazeInputSource.SourceId)
                    {
                        gazeProviderPointingData.ResetFocusedObjects();
                        gazeProviderPointingData = null;
                    }
                    // Otherwise, don't unregister the gaze pointer, since the gaze input source is still active.
                    else
                    {
                        continue;
                    }
                }

                UnregisterPointer(eventData.InputSource.Pointers[i]);
            }
        }

        #endregion ISourceState Implementation
    }
}
