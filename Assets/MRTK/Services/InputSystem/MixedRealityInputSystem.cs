// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// The Mixed Reality Toolkit's specific implementation of the <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/>
    /// </summary>
    [HelpURL("https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/input/overview")]
    public class MixedRealityInputSystem : BaseDataProviderAccessCoreSystem, IMixedRealityInputSystem, IMixedRealityCapabilityCheck
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the service.</param>
        /// <param name="profile">The configuration profile for the service.</param>
        [Obsolete("This constructor is obsolete (registrar parameter is no longer required) and will be removed in a future version of the Microsoft Mixed Reality Toolkit.")]
        public MixedRealityInputSystem(
            IMixedRealityServiceRegistrar registrar,
            MixedRealityInputSystemProfile profile) : this(profile)
        {
            Registrar = registrar;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile">The configuration profile for the service.</param>
        public MixedRealityInputSystem(
            MixedRealityInputSystemProfile profile) : base(profile)
        { }

        private static readonly ProfilerMarker ExecuteHierarchyPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem - Calling EventSystems.ExecuteEvents.ExecuteHierarchy");

        /// <inheritdoc/>
        public override string Name { get; protected set; } = "Mixed Reality Input System";

        /// <inheritdoc />
        public event Action InputEnabled;

        /// <inheritdoc />
        public event Action InputDisabled;

        /// <inheritdoc />
        public HashSet<IMixedRealityInputSource> DetectedInputSources { get; } = new HashSet<IMixedRealityInputSource>();

        /// <inheritdoc />
        public HashSet<IMixedRealityController> DetectedControllers { get; } = new HashSet<IMixedRealityController>();


        private MixedRealityInputSystemProfile inputSystemProfile = null;

        /// <inheritdoc/>
        public MixedRealityInputSystemProfile InputSystemProfile
        {
            get
            {
                if (inputSystemProfile == null)
                {
                    inputSystemProfile = ConfigurationProfile as MixedRealityInputSystemProfile;
                }
                return inputSystemProfile;
            }
        }

        /// <inheritdoc />
        public IMixedRealityFocusProvider FocusProvider => CoreServices.FocusProvider;

        /// <inheritdoc />
        public IMixedRealityRaycastProvider RaycastProvider => CoreServices.RaycastProvider;

        /// <inheritdoc />
        public IMixedRealityGazeProvider GazeProvider { get; private set; }

        /// <inheritdoc />
        public IMixedRealityEyeGazeProvider EyeGazeProvider { get; private set; }

        private readonly Stack<GameObject> modalInputStack = new Stack<GameObject>();
        private readonly Stack<GameObject> fallbackInputStack = new Stack<GameObject>();

        /// <inheritdoc />
        public bool IsInputEnabled => disabledRefCount <= 0;

        private int disabledRefCount;
        private bool isInputModuleAdded = false;

        private SourceStateEventData sourceStateEventData;
        private SourcePoseEventData<TrackingState> sourceTrackingEventData;
        private SourcePoseEventData<Vector2> sourceVector2EventData;
        private SourcePoseEventData<Vector3> sourcePositionEventData;
        private SourcePoseEventData<Quaternion> sourceRotationEventData;
        private SourcePoseEventData<MixedRealityPose> sourcePoseEventData;

        private FocusEventData focusEventData;

        private InputEventData inputEventData;
        private MixedRealityPointerEventData pointerEventData;

        private InputEventData<float> floatInputEventData;
        private InputEventData<Vector2> vector2InputEventData;
        private InputEventData<Vector3> positionInputEventData;
        private InputEventData<Quaternion> rotationInputEventData;
        private InputEventData<MixedRealityPose> poseInputEventData;
        private InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> jointPoseInputEventData;
        private InputEventData<HandMeshInfo> handMeshInputEventData;

        private SpeechEventData speechEventData;
        private DictationEventData dictationEventData;

        private HandTrackingInputEventData handTrackingInputEventData;

        private MixedRealityInputActionRulesProfile CurrentInputActionRulesProfile { get; set; }

        private bool inputModuleChecked = false;
        private MixedRealityInputModule inputModule;

        #region IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            foreach (var deviceManager in GetDataProviders<IMixedRealityInputDeviceManager>())
            {
                IMixedRealityCapabilityCheck capabilityChecker = deviceManager as IMixedRealityCapabilityCheck;

                // If one of the running data providers supports the requested capability, 
                // the application has the needed support to leverage the desired functionality.
                if (capabilityChecker?.CheckCapability(capability) == true)
                {
                    return true;
                }
            }

            // Check GazeProvider directly since not populated in data provider list but life-cycle is managed by InputSystem
            var gazeProvider_CapabilityCheck = GazeProvider as IMixedRealityCapabilityCheck;
            if (gazeProvider_CapabilityCheck?.CheckCapability(capability) == true)
            {
                return true;
            }

            return false;
        }

        #endregion IMixedRealityCapabilityCheck Implementation

        #region IMixedRealityService Implementation

        /// <inheritdoc />
        /// <remarks>
        /// Input system is critical, so should be processed before all other managers
        /// </remarks>
        public override uint Priority => 1;

        /// <inheritdoc />
        public override void Initialize()
        {
            MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;
            if (profile == null)
            {
                Debug.LogError("The Input system is missing the required Input System Profile!");
                return;
            }

            BaseInputModule[] inputModules = UnityEngine.Object.FindObjectsOfType<BaseInputModule>();

            if (inputModules.Length == 0)
            {
                DebugUtilities.LogVerbose("MixedRealityInputModule added to main camera");
                // There is no input module attached to the camera, add one.
                inputModule = CameraCache.Main.gameObject.AddComponent<MixedRealityInputModule>();
                isInputModuleAdded = true;
            }
            else if ((inputModules.Length == 1) && (inputModules[0] is MixedRealityInputModule))
            {
                inputModule = inputModules[0] as MixedRealityInputModule;
            }
            else
            {
                Debug.LogError($"For Mixed Reality Toolkit input to work properly, please remove your other input module(s) and add a {typeof(MixedRealityInputModule).Name} to your main camera.", inputModules[0]);
            }

            if (InputSystemProfile == null)
            {
                Debug.LogError("The Input system is missing the required Input System Profile!");
                return;
            }

            if (profile.InputActionRulesProfile != null)
            {
                CurrentInputActionRulesProfile = profile.InputActionRulesProfile;
            }
            else
            {
                Debug.LogError("The Input system is missing the required Input Action Rules Profile!");
                return;
            }

            if (profile.PointerProfile != null)
            {
                InstantiateGazeProvider(profile.PointerProfile);
            }
            else
            {
                Debug.LogError("The Input system is missing the required Pointer Profile!");
                return;
            }

            sourceStateEventData = new SourceStateEventData(EventSystem.current);

            sourceTrackingEventData = new SourcePoseEventData<TrackingState>(EventSystem.current);
            sourceVector2EventData = new SourcePoseEventData<Vector2>(EventSystem.current);
            sourcePositionEventData = new SourcePoseEventData<Vector3>(EventSystem.current);
            sourceRotationEventData = new SourcePoseEventData<Quaternion>(EventSystem.current);
            sourcePoseEventData = new SourcePoseEventData<MixedRealityPose>(EventSystem.current);

            focusEventData = new FocusEventData(EventSystem.current);

            inputEventData = new InputEventData(EventSystem.current);
            pointerEventData = new MixedRealityPointerEventData(EventSystem.current);

            floatInputEventData = new InputEventData<float>(EventSystem.current);
            vector2InputEventData = new InputEventData<Vector2>(EventSystem.current);
            positionInputEventData = new InputEventData<Vector3>(EventSystem.current);
            rotationInputEventData = new InputEventData<Quaternion>(EventSystem.current);
            poseInputEventData = new InputEventData<MixedRealityPose>(EventSystem.current);
            jointPoseInputEventData = new InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>>(EventSystem.current);
            handMeshInputEventData = new InputEventData<HandMeshInfo>(EventSystem.current);

            speechEventData = new SpeechEventData(EventSystem.current);
            dictationEventData = new DictationEventData(EventSystem.current);

            handTrackingInputEventData = new HandTrackingInputEventData(EventSystem.current);

            CreateDataProviders();

            // Call the base after initialization to ensure any early exits do not
            // artificially declare the service as initialized.
            base.Initialize();
        }

        /// <inheritdoc />
        public override void Enable()
        {
            CreateDataProviders();

            // Ensure data providers are enabled (performed by the base class)
            base.Enable();

            InputEnabled?.Invoke();
        }

        public override void LateUpdate()
        {
            // Check whether manual initialization of input module is needed.
            // The check is only required once after input system is created.
            if (!isInputModuleAdded && !inputModuleChecked)
            {
                if (inputModule.ManualInitializationRequired)
                {
                    inputModule.Initialize();
                }
                inputModuleChecked = true;
            }
            base.LateUpdate();
        }

        private void CreateDataProviders()
        {
            MixedRealityInputSystemProfile profile = ConfigurationProfile as MixedRealityInputSystemProfile;

            // If the system gets disabled, the gaze provider is destroyed.
            // Ensure that it gets recreated on when re-enabled.
            if (GazeProvider == null && profile != null)
            {
                InstantiateGazeProvider(profile.PointerProfile);
            }

            if ((GetDataProviders().Count == 0) && (profile != null))
            {
                // Register the input device managers.
                for (int i = 0; i < profile.DataProviderConfigurations.Length; i++)
                {
                    MixedRealityInputDataProviderConfiguration configuration = profile.DataProviderConfigurations[i];
                    object[] args = { this, configuration.ComponentName, configuration.Priority, configuration.Profile };

                    DebugUtilities.LogVerboseFormat(
                        "Attempting to register input system data provider {0}, {1}, {2}",
                        configuration.ComponentType.Type,
                        configuration.ComponentName,
                        configuration.RuntimePlatform);

                    RegisterDataProvider<IMixedRealityInputDeviceManager>(
                        configuration.ComponentType.Type,
                        configuration.ComponentName,
                        configuration.RuntimePlatform,
                        args);
                }
            }
        }

        private void InstantiateGazeProvider(MixedRealityPointerProfile pointerProfile)
        {
            if (pointerProfile != null && pointerProfile.GazeProviderType?.Type != null)
            {
                GazeProvider = CameraCache.Main.gameObject.EnsureComponent(pointerProfile.GazeProviderType.Type) as IMixedRealityGazeProvider;
                GazeProvider.GazeCursorPrefab = pointerProfile.GazeCursorPrefab;
                DebugUtilities.LogVerboseFormat("Initialized a gaze provider of type {0}", pointerProfile.GazeProviderType.Type);
                // Current default implementation implements both provider types in one concrete class.
                EyeGazeProvider = GazeProvider as IMixedRealityEyeGazeProvider;
                if (EyeGazeProvider != null)
                {
                    EyeGazeProvider.IsEyeTrackingEnabled = pointerProfile.IsEyeTrackingEnabled;
                    DebugUtilities.LogVerboseFormat("Gaze Provider supports IMixedRealityEyeGazeProvider, IsEyeTrackingEnabled set to {0}", EyeGazeProvider.IsEyeTrackingEnabled);
                }

                if (GazeProvider is IMixedRealityGazeProviderHeadOverride gazeProviderHeadOverride)
                {
                    gazeProviderHeadOverride.UseHeadGazeOverride = pointerProfile.UseHeadGazeOverride;
                    DebugUtilities.LogVerboseFormat("Gaze Provider supports IMixedRealityGazeProviderHeadOverride, UseHeadGazeOverride set to {0}", gazeProviderHeadOverride.UseHeadGazeOverride);
                }
            }
            else
            {
                Debug.LogError("The input system is missing the required GazeProviderType!");
                return;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            Disable();
            Initialize();
            Enable();
        }

        /// <inheritdoc />
        public override void Disable()
        {
            base.Disable();

            // Input System adds a gaze provider component on the main camera, which needs to be removed when the input system is disabled/removed.
            // Otherwise the component would keep references to dead objects.
            // Unity's way to remove component is to destroy it.
            if (GazeProvider != null)
            {
                if (Application.isPlaying)
                {
                    GazeProvider.GazePointer.BaseCursor.Destroy();
                    DebugUtilities.LogVerbose("Application was playing, destroyed the gaze pointer's BaseCursor");
                }

                UnityObjectExtensions.DestroyObject(GazeProvider as Component);

                GazeProvider = null;
                DebugUtilities.LogVerbose("Destroyed the GazeProvider in MixedRealityInputSystem");
            }

            foreach (var provider in GetDataProviders<IMixedRealityInputDeviceManager>())
            {
                if (provider != null)
                {
                    DebugUtilities.LogVerboseFormat("Unregistering input data provider {0}", provider);
                    UnregisterDataProvider<IMixedRealityInputDeviceManager>(provider);
                }
            }

            InputDisabled?.Invoke();
        }

        public override void Destroy()
        {
            if (isInputModuleAdded)
            {
                if (inputModule)
                {
                    if (Application.isPlaying)
                    {
                        inputModule.DeactivateModule();
                    }

                    UnityObjectExtensions.DestroyObject(inputModule);
                }
            }
            // If the MRTK profile is being switched and there is an input module in the scene in the beginning
            else if (Application.isPlaying && inputModule != null)
            {
                inputModule.Suspend();
            }

            inputModule = null;
            base.Destroy();
        }
        #endregion IMixedRealityService Implementation

        #region IMixedRealityDataProviderAccess Implementation

        /// <inheritdoc />
        public override IReadOnlyList<T> GetDataProviders<T>()
        {
            if (!typeof(IMixedRealityInputDeviceManager).IsAssignableFrom(typeof(T)))
            {
                return null;
            }

            return base.GetDataProviders<T>();
        }

        /// <inheritdoc />
        public override T GetDataProvider<T>(string name = null)
        {
            if (!typeof(IMixedRealityInputDeviceManager).IsAssignableFrom(typeof(T)))
            {
                return default(T);
            }

            return base.GetDataProvider<T>(name);
        }

        #endregion IMixedRealityDataProviderAccess Implementation

        #region IMixedRealityEventSystem Implementation

        private static readonly ProfilerMarker HandleEventPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.HandleEvent");

        /// <inheritdoc />
        public override void HandleEvent<T>(BaseEventData eventData, ExecuteEvents.EventFunction<T> eventHandler)
        {
            using (HandleEventPerfMarker.Auto())
            {
                if (disabledRefCount > 0)
                {
                    return;
                }

                Debug.Assert(eventData != null);
                Debug.Assert(!(eventData is MixedRealityPointerEventData), "HandleEvent called with a pointer event. All events raised by pointer should call HandlePointerEvent");

                var baseInputEventData = ExecuteEvents.ValidateEventData<BaseInputEventData>(eventData);
                DispatchEventToGlobalListeners(baseInputEventData, eventHandler);

                if (baseInputEventData.used)
                {
                    // All global listeners get a chance to see the event,
                    // but if any of them marked it used,
                    // we stop the event from going any further.
                    return;
                }

                if (baseInputEventData.InputSource.Pointers == null) { Debug.LogError($"InputSource {baseInputEventData.InputSource.SourceName} doesn't have any registered pointers! Input Sources without pointers should use the GazeProvider's pointer as a default fallback."); }

                var modalEventHandled = false;
                // Get the focused object for each pointer of the event source
                for (int i = 0; i < baseInputEventData.InputSource.Pointers.Length && !baseInputEventData.used; i++)
                {
                    modalEventHandled = DispatchEventToObjectFocusedByPointer(baseInputEventData.InputSource.Pointers[i], baseInputEventData, modalEventHandled, eventHandler);
                }

                if (!baseInputEventData.used)
                {
                    DispatchEventToFallbackHandlers(baseInputEventData, eventHandler);
                }
            }
        }

        private static readonly ProfilerMarker HandleFocusChangedEventsPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.HandleFocusChangedEvents");

        /// <summary>
        /// Handles focus changed events
        /// We send all focus events to all global listeners and the actual focus change receivers. the use flag is completely ignored to avoid any interception.
        /// </summary>
        private void HandleFocusChangedEvents(FocusEventData focusEventData, ExecuteEvents.EventFunction<IMixedRealityFocusChangedHandler> eventHandler)
        {
            using (HandleFocusChangedEventsPerfMarker.Auto())
            {
                Debug.Assert(focusEventData != null);

                DispatchEventToGlobalListeners(focusEventData, eventHandler);

                // Raise Focus Events on the old and new focused objects.
                if (focusEventData.OldFocusedObject != null)
                {
                    using (ExecuteHierarchyPerfMarker.Auto())
                    {
                        ExecuteEvents.ExecuteHierarchy(focusEventData.OldFocusedObject, focusEventData, eventHandler);
                    }
                }

                if (focusEventData.NewFocusedObject != null)
                {
                    using (ExecuteHierarchyPerfMarker.Auto())
                    {
                        ExecuteEvents.ExecuteHierarchy(focusEventData.NewFocusedObject, focusEventData, eventHandler);
                    }
                }

                // Raise Focus Events on the pointers cursor if it has one.
                if (focusEventData.Pointer != null && focusEventData.Pointer.BaseCursor != null)
                {
                    try
                    {
                        using (ExecuteHierarchyPerfMarker.Auto())
                        {
                            // When shutting down a game, we can sometime get old references to game objects that have been cleaned up.
                            // We'll ignore when this happens.
                            ExecuteEvents.ExecuteHierarchy(focusEventData.Pointer.BaseCursor.GameObjectReference, focusEventData, eventHandler);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
                }
            }
        }

        private static readonly ProfilerMarker HandleFocusEventPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.HandleFocusEvent");

        /// <summary>
        /// Handles focus enter and exit
        /// We send the focus event to all global listeners and the actual focus change receiver. the use flag is completely ignored to avoid any interception.
        /// </summary>
        private void HandleFocusEvent(GameObject eventTarget, FocusEventData focusEventData, ExecuteEvents.EventFunction<IMixedRealityFocusHandler> eventHandler)
        {
            using (HandleFocusEventPerfMarker.Auto())
            {
                Debug.Assert(focusEventData != null);

                DispatchEventToGlobalListeners(focusEventData, eventHandler);

                using (ExecuteHierarchyPerfMarker.Auto())
                {
                    ExecuteEvents.ExecuteHierarchy(eventTarget, focusEventData, eventHandler);
                }
            }
        }

        private static readonly ProfilerMarker HandlePointerEventPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.HandlePointerEvent");

        /// <summary>
        /// Handles a pointer event
        /// Assumption: We only send pointer events to the objects that pointers are focusing, except for global event listeners (which listen to everything)
        /// In contract, all other events get sent to all other pointers attached to a given input source
        /// </summary>
        private void HandlePointerEvent<T>(BaseEventData eventData, ExecuteEvents.EventFunction<T> eventHandler) where T : IMixedRealityPointerHandler
        {
            using (HandlePointerEventPerfMarker.Auto())
            {
                if (disabledRefCount > 0)
                {
                    return;
                }

                Debug.Assert(eventData != null);
                var baseInputEventData = ExecuteEvents.ValidateEventData<BaseInputEventData>(eventData);
                DispatchEventToGlobalListeners(baseInputEventData, eventHandler);

                if (baseInputEventData.used)
                {
                    // All global listeners get a chance to see the event,
                    // but if any of them marked it used,
                    // we stop the event from going any further.
                    return;
                }

                Debug.Assert(pointerEventData.Pointer != null, "Trying to dispatch event on pointer but pointerEventData is null");

                DispatchEventToObjectFocusedByPointer(pointerEventData.Pointer, baseInputEventData, false, eventHandler);

                if (!baseInputEventData.used)
                {
                    DispatchEventToFallbackHandlers(baseInputEventData, eventHandler);
                }
            }
        }

        private static readonly ProfilerMarker DispatchEventToGlobalListenersPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.DispatchEventToGlobalListeners");

        /// <summary>
        /// Dispatch an input event to all global event listeners
        /// Return true if the event has been handled by a global event listener
        /// </summary>
        private void DispatchEventToGlobalListeners<T>(BaseInputEventData baseInputEventData, ExecuteEvents.EventFunction<T> eventHandler) where T : IEventSystemHandler
        {
            using (DispatchEventToGlobalListenersPerfMarker.Auto())
            {
                Debug.Assert(baseInputEventData != null);
                Debug.Assert(!baseInputEventData.used);
                if (baseInputEventData.InputSource == null) { Debug.Assert(baseInputEventData.InputSource != null, $"Failed to find an input source for {baseInputEventData}"); }

                // Send the event to global listeners
                base.HandleEvent(baseInputEventData, eventHandler);
            }
        }

        /// <summary>
        /// Dispatch a focus event to all global event listeners
        /// </summary>
        private void DispatchEventToGlobalListeners<T>(FocusEventData focusEventData, ExecuteEvents.EventFunction<T> eventHandler) where T : IEventSystemHandler
        {
            using (DispatchEventToGlobalListenersPerfMarker.Auto())
            {
                Debug.Assert(focusEventData != null);
                Debug.Assert(!focusEventData.used);

                // Send the event to global listeners
                base.HandleEvent(focusEventData, eventHandler);
            }
        }

        private static readonly ProfilerMarker DispatchEventToFallbackHandlersPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.DispatchEventToFallbackHandlers");

        private void DispatchEventToFallbackHandlers<T>(BaseInputEventData baseInputEventData, ExecuteEvents.EventFunction<T> eventHandler) where T : IEventSystemHandler
        {
            using (DispatchEventToFallbackHandlersPerfMarker.Auto())
            {
                // If event was not handled by the focused object, pass it on to any fallback handlers
                if (!baseInputEventData.used && fallbackInputStack.Count > 0)
                {
                    GameObject fallbackInput = fallbackInputStack.Peek();
                    using (ExecuteHierarchyPerfMarker.Auto())
                    {
                        ExecuteEvents.ExecuteHierarchy(fallbackInput, baseInputEventData, eventHandler);
                    }
                }
            }
        }

        private static readonly ProfilerMarker DispatchEventToObjectFocusedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.DispatchEventToObjectFocusedByPointer");
        private static readonly ProfilerMarker DispatchModalEventPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.DispatchEventToObjectFocusedByPointer - Modal event handling");

        /// <summary>
        /// Dispatch an input event to the object focused by the given IMixedRealityPointer.
        /// If a modal dialog is active, dispatch the pointer event to that modal dialog
        /// Returns true if the event was handled by a modal handler
        /// </summary>
        private bool DispatchEventToObjectFocusedByPointer<T>(IMixedRealityPointer mixedRealityPointer, BaseInputEventData baseInputEventData,
            bool modalEventHandled, ExecuteEvents.EventFunction<T> eventHandler) where T : IEventSystemHandler
        {
            using (DispatchEventToObjectFocusedPerfMarker.Auto())
            {
                GameObject focusedObject = FocusProvider?.GetFocusedObject(mixedRealityPointer);

                using (DispatchModalEventPerfMarker.Auto())
                {
                    // Handle modal input if one exists
                    if (modalInputStack.Count > 0 && !modalEventHandled)
                    {
                        GameObject modalInput = modalInputStack.Peek();

                        if (modalInput != null)
                        {
                            // If there is a focused object in the hierarchy of the modal handler, start the event bubble there
                            if (focusedObject != null && focusedObject.transform.IsChildOf(modalInput.transform))
                            {
                                using (ExecuteHierarchyPerfMarker.Auto())
                                {
                                    if (ExecuteEvents.ExecuteHierarchy(focusedObject, baseInputEventData, eventHandler) && baseInputEventData.used)
                                    {
                                        return true;
                                    }
                                }
                            }
                            // Otherwise, just invoke the event on the modal handler itself
                            else
                            {
                                using (ExecuteHierarchyPerfMarker.Auto())
                                {
                                    if (ExecuteEvents.ExecuteHierarchy(modalInput, baseInputEventData, eventHandler) && baseInputEventData.used)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("ModalInput GameObject reference was null!\nDid this GameObject get destroyed?");
                        }
                    }
                }

                // If event was not handled by modal, pass it on to the current focused object
                if (focusedObject != null)
                {
                    using (ExecuteHierarchyPerfMarker.Auto())
                    {
                        ExecuteEvents.ExecuteHierarchy(focusedObject, baseInputEventData, eventHandler);
                    }
                }
                return modalEventHandled;
            }
        }

        /// <summary>
        /// Register a <see href="https://docs.unity3d.com/ScriptReference/GameObject.html">GameObject</see> to listen to events that will receive all input events, regardless
        /// of which other <see href="https://docs.unity3d.com/ScriptReference/GameObject.html">GameObject</see>s might have handled the event beforehand.
        /// </summary>
        /// <remarks>Useful for listening to events when the <see href="https://docs.unity3d.com/ScriptReference/GameObject.html">GameObject</see> is currently not being raycasted against by the <see cref="FocusProvider"/>.</remarks>
        /// <param name="listener">Listener to add.</param>
        public override void Register(GameObject listener)
        {
            base.Register(listener);
        }

        /// <summary>
        /// Unregister a <see href="https://docs.unity3d.com/ScriptReference/GameObject.html">GameObject</see> from listening to input events.
        /// </summary>
        public override void Unregister(GameObject listener)
        {
            base.Unregister(listener);
        }

        #endregion IMixedRealityEventSystem Implementation

        #region Input Disabled Options

        /// <summary>
        /// Push a disabled input state onto the input manager.
        /// While input is disabled no events will be sent out and the cursor displays
        /// a waiting animation.
        /// </summary>
        public void PushInputDisable()
        {
            ++disabledRefCount;

            if (disabledRefCount == 1)
            {
                InputDisabled?.Invoke();

                if (GazeProvider != null)
                {
                    GazeProvider.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Pop disabled input state. When the last disabled state is 
        /// popped off the stack input will be re-enabled.
        /// </summary>
        public void PopInputDisable()
        {
            --disabledRefCount;
            Debug.Assert(disabledRefCount >= 0, "Tried to pop more input disable than the amount pushed.");

            if (disabledRefCount == 0)
            {
                InputEnabled?.Invoke();

                if (GazeProvider != null)
                {
                    GazeProvider.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Clear the input disable stack, which will immediately re-enable input.
        /// </summary>
        public void ClearInputDisableStack()
        {
            bool wasInputDisabled = disabledRefCount > 0;
            disabledRefCount = 0;

            if (wasInputDisabled)
            {
                InputEnabled?.Invoke();

                if (GazeProvider != null)
                {
                    GazeProvider.Enabled = true;
                }
            }
        }

        #endregion Input Disabled Options

        #region Modal Input Options

        /// <summary>
        /// Push a game object into the modal input stack. Any input handlers
        /// on the game object are given priority to input events before any focused objects.
        /// </summary>
        /// <param name="inputHandler">The input handler to push</param>
        public void PushModalInputHandler(GameObject inputHandler)
        {
            modalInputStack.Push(inputHandler);
        }

        /// <summary>
        /// Remove the last game object from the modal input stack.
        /// </summary>
        public void PopModalInputHandler()
        {
            if (modalInputStack.Count > 0)
            {
                modalInputStack.Pop();

            }
        }

        /// <summary>
        /// Clear all modal input handlers off the stack.
        /// </summary>
        public void ClearModalInputStack()
        {
            modalInputStack.Clear();
        }

        #endregion Modal Input Options

        #region Fallback Input Handler Options

        /// <summary>
        /// Push a game object into the fallback input stack. Any input handlers on
        /// the game object are given input events when no modal or focused objects consume the event.
        /// </summary>
        /// <param name="inputHandler">The input handler to push</param>
        public void PushFallbackInputHandler(GameObject inputHandler)
        {
            fallbackInputStack.Push(inputHandler);
        }

        /// <summary>
        /// Remove the last game object from the fallback input stack.
        /// </summary>
        public void PopFallbackInputHandler()
        {
            fallbackInputStack.Pop();
        }

        /// <summary>
        /// Clear all fallback input handlers off the stack.
        /// </summary>
        public void ClearFallbackInputStack()
        {
            fallbackInputStack.Clear();
        }

        #endregion Fallback Input Handler Options

        #region Input Events

        #region Input Source Events

        /// <inheritdoc />
        public uint GenerateNewSourceId()
        {
            var newId = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            foreach (var inputSource in DetectedInputSources)
            {
                if (inputSource.SourceId == newId)
                {
                    return GenerateNewSourceId();
                }
            }

            return newId;
        }

        /// <inheritdoc />
        public IMixedRealityInputSource RequestNewGenericInputSource(string name, IMixedRealityPointer[] pointers = null, InputSourceType sourceType = InputSourceType.Other)
        {
            return new BaseGenericInputSource(name, pointers, sourceType);
        }

        /// <inheritdoc />
        public BaseGlobalInputSource RequestNewGlobalInputSource(string name, IMixedRealityFocusProvider focusProvider = null, InputSourceType sourceType = InputSourceType.Other)
        {
            var inputSourceFocusProvider = focusProvider.IsNull() ? FocusProvider : focusProvider;
            return new BaseGlobalInputSource(name, inputSourceFocusProvider, sourceType);
        }


        #region Input Source State Events

        private static readonly ProfilerMarker RaiseSourceDetectedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSourceDetected");

        /// <inheritdoc />
        public void RaiseSourceDetected(IMixedRealityInputSource source, IMixedRealityController controller = null)
        {
            using (RaiseSourceDetectedPerfMarker.Auto())
            {
                if (DetectedInputSources.Contains(source))
                {
                    Debug.LogWarning($"[MRTK Issue] {source.SourceName} has already been registered with the Input Manager!");
                    return;
                }

                // Create input event
                sourceStateEventData.Initialize(source, controller);

                DetectedInputSources.Add(source);

                if (controller != null)
                {
                    DetectedControllers.Add(controller);
                }

                DebugUtilities.LogVerboseFormat("RaiseSourceDetected: Source ID: {0}, Source Type: {1}", source.SourceId, source.SourceType);

                FocusProvider?.OnSourceDetected(sourceStateEventData);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourceStateEventData, OnSourceDetectedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourceStateHandler> OnSourceDetectedEventHandler =
            delegate (IMixedRealitySourceStateHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<SourceStateEventData>(eventData);
                handler.OnSourceDetected(casted);
            };

        private static readonly ProfilerMarker RaiseSourceLostPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSourceLost");

        /// <inheritdoc />
        public void RaiseSourceLost(IMixedRealityInputSource source, IMixedRealityController controller = null)
        {
            using (RaiseSourceLostPerfMarker.Auto())
            {
                if (!DetectedInputSources.Contains(source))
                {
                    Debug.LogWarning($"[MRTK Issue] {source.SourceName} was never registered with the Input Manager!");
                    return;
                }

                // Create input event
                sourceStateEventData.Initialize(source, controller);

                DetectedInputSources.Remove(source);

                DebugUtilities.LogVerboseFormat("RaiseSourceLost: Source ID: {0}, Source Type: {1}", source.SourceId, source.SourceType);

                if (controller != null)
                {
                    DetectedControllers.Remove(controller);
                }

                // Pass handler through HandleEvent to perform modal/fallback logic
                // Events have to be handled before FocusProvider.OnSourceLost since they won't be passed on without a focused object
                HandleEvent(sourceStateEventData, OnSourceLostEventHandler);

                FocusProvider?.OnSourceLost(sourceStateEventData);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourceStateHandler> OnSourceLostEventHandler =
                delegate (IMixedRealitySourceStateHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourceStateEventData>(eventData);
                    handler.OnSourceLost(casted);
                };

        #endregion Input Source State Events

        #region Input Source Pose Events

        private static readonly ProfilerMarker RaiseSourceTrackingStateChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseTrackingStateChanged");

        /// <inheritdoc />
        public void RaiseSourceTrackingStateChanged(IMixedRealityInputSource source, IMixedRealityController controller, TrackingState state)
        {
            using (RaiseSourceTrackingStateChangedPerfMarker.Auto())
            {
                // Create input event
                sourceTrackingEventData.Initialize(source, controller, state);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourceTrackingEventData, OnSourceTrackingChangedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourcePoseHandler> OnSourceTrackingChangedEventHandler =
                delegate (IMixedRealitySourcePoseHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourcePoseEventData<TrackingState>>(eventData);
                    handler.OnSourcePoseChanged(casted);
                };

        private static readonly ProfilerMarker RaiseSourcePositionChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSourcePositionChanged");

        /// <inheritdoc />
        public void RaiseSourcePositionChanged(IMixedRealityInputSource source, IMixedRealityController controller, Vector2 position)
        {
            using (RaiseSourcePositionChangedPerfMarker.Auto())
            {
                // Create input event
                sourceVector2EventData.Initialize(source, controller, position);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourceVector2EventData, OnSourcePoseVector2ChangedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourcePoseHandler> OnSourcePoseVector2ChangedEventHandler =
                delegate (IMixedRealitySourcePoseHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourcePoseEventData<Vector2>>(eventData);
                    handler.OnSourcePoseChanged(casted);
                };

        /// <inheritdoc />
        public void RaiseSourcePositionChanged(IMixedRealityInputSource source, IMixedRealityController controller, Vector3 position)
        {
            using (RaiseSourcePositionChangedPerfMarker.Auto())
            {
                // Create input event
                sourcePositionEventData.Initialize(source, controller, position);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourcePositionEventData, OnSourcePositionChangedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourcePoseHandler> OnSourcePositionChangedEventHandler =
                delegate (IMixedRealitySourcePoseHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourcePoseEventData<Vector3>>(eventData);
                    handler.OnSourcePoseChanged(casted);
                };

        private static readonly ProfilerMarker RaiseSourceRotationChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSourceRotationChanged");

        /// <inheritdoc />
        public void RaiseSourceRotationChanged(IMixedRealityInputSource source, IMixedRealityController controller, Quaternion rotation)
        {
            using (RaiseSourceRotationChangedPerfMarker.Auto())
            {
                // Create input event
                sourceRotationEventData.Initialize(source, controller, rotation);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourceRotationEventData, OnSourceRotationChangedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourcePoseHandler> OnSourceRotationChangedEventHandler =
                delegate (IMixedRealitySourcePoseHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourcePoseEventData<Quaternion>>(eventData);
                    handler.OnSourcePoseChanged(casted);
                };

        private static readonly ProfilerMarker RaiseSourcePoseChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSourcePoseChanged");

        /// <inheritdoc />
        public void RaiseSourcePoseChanged(IMixedRealityInputSource source, IMixedRealityController controller, MixedRealityPose position)
        {
            using (RaiseSourcePoseChangedPerfMarker.Auto())
            {
                // Create input event
                sourcePoseEventData.Initialize(source, controller, position);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(sourcePoseEventData, OnSourcePoseChangedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySourcePoseHandler> OnSourcePoseChangedEventHandler =
                delegate (IMixedRealitySourcePoseHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<SourcePoseEventData<MixedRealityPose>>(eventData);
                    handler.OnSourcePoseChanged(casted);
                };

        #endregion Input Source Pose Events

        #endregion Input Source Events

        #region Focus Events

        private static readonly ProfilerMarker RaisePreFocusChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePreFocusChanged");

        /// <inheritdoc />
        public void RaisePreFocusChanged(IMixedRealityPointer pointer, GameObject oldFocusedObject, GameObject newFocusedObject)
        {
            using (RaisePreFocusChangedPerfMarker.Auto())
            {
                focusEventData.Initialize(pointer, oldFocusedObject, newFocusedObject);

                HandleFocusChangedEvents(focusEventData, OnPreFocusChangedHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityFocusChangedHandler> OnPreFocusChangedHandler =
                delegate (IMixedRealityFocusChangedHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<FocusEventData>(eventData);
                    handler.OnBeforeFocusChange(casted);
                };

        private static readonly ProfilerMarker RaiseFocusChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseFocusChanged");

        /// <inheritdoc />
        public void RaiseFocusChanged(IMixedRealityPointer pointer, GameObject oldFocusedObject, GameObject newFocusedObject)
        {
            using (RaiseFocusChangedPerfMarker.Auto())
            {
                focusEventData.Initialize(pointer, oldFocusedObject, newFocusedObject);

                HandleFocusChangedEvents(focusEventData, OnFocusChangedHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityFocusChangedHandler> OnFocusChangedHandler =
            delegate (IMixedRealityFocusChangedHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<FocusEventData>(eventData);
                handler.OnFocusChanged(casted);
            };

        private static readonly ProfilerMarker RaiseFocusEnterPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseFocusEnter");

        /// <inheritdoc />
        public void RaiseFocusEnter(IMixedRealityPointer pointer, GameObject focusedObject)
        {
            using (RaiseFocusEnterPerfMarker.Auto())
            {
                focusEventData.Initialize(pointer);

                HandleFocusEvent(focusedObject, focusEventData, OnFocusEnterEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityFocusHandler> OnFocusEnterEventHandler =
                delegate (IMixedRealityFocusHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<FocusEventData>(eventData);
                    handler.OnFocusEnter(casted);
                };

        private static readonly ProfilerMarker RaiseFocusExitPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseFocusExit");

        /// <inheritdoc />
        public void RaiseFocusExit(IMixedRealityPointer pointer, GameObject unfocusedObject)
        {
            using (RaiseFocusExitPerfMarker.Auto())
            {
                focusEventData.Initialize(pointer);

                HandleFocusEvent(unfocusedObject, focusEventData, OnFocusExitEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityFocusHandler> OnFocusExitEventHandler =
                delegate (IMixedRealityFocusHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<FocusEventData>(eventData);
                    handler.OnFocusExit(casted);
                };

        #endregion Focus Events

        #region Pointers

        #region Pointer Down

        public static readonly ExecuteEvents.EventFunction<IMixedRealityPointerHandler> OnPointerDownEventHandler =
            delegate (IMixedRealityPointerHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<MixedRealityPointerEventData>(eventData);
                handler.OnPointerDown(casted);
            };

        private static readonly ProfilerMarker RaisePointerDownPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePointerDown");

        /// <inheritdoc />
        public void RaisePointerDown(IMixedRealityPointer pointer, MixedRealityInputAction inputAction, Handedness handedness = Handedness.None, IMixedRealityInputSource inputSource = null)
        {
            using (RaisePointerDownPerfMarker.Auto())
            {
                // Only lock the object if there is a grabbable above in the hierarchy
                Transform currentObject = null;
                GameObject currentGameObject = pointer.Result?.Details.Object;
                if (currentGameObject != null)
                {
                    currentObject = currentGameObject.transform;
                }
                IMixedRealityPointerHandler ancestorPointerHandler = null;
                while (currentObject != null && ancestorPointerHandler == null)
                {
                    foreach (IMixedRealityPointerHandler handler in currentObject.GetComponents<IMixedRealityPointerHandler>())
                    {
                        if(handler is MonoBehaviour behavior && !behavior.enabled)
                        {
                            continue;
                        }
                        ancestorPointerHandler = handler;
                        break;
                    }
                    currentObject = currentObject.transform.parent;
                }
                pointer.IsFocusLocked = ancestorPointerHandler != null;

                pointerEventData.Initialize(pointer, inputAction, handedness, inputSource);

                HandlePointerEvent(pointerEventData, OnPointerDownEventHandler);
            }
        }

        #endregion Pointer Down

        #region Pointer Dragged

        public static readonly ExecuteEvents.EventFunction<IMixedRealityPointerHandler> OnPointerDraggedEventHandler =
            delegate (IMixedRealityPointerHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<MixedRealityPointerEventData>(eventData);
                handler.OnPointerDragged(casted);
            };

        private static readonly ProfilerMarker RaisePointerDraggedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePointerDragged");

        /// <inheritdoc />
        public void RaisePointerDragged(IMixedRealityPointer pointer, MixedRealityInputAction inputAction, Handedness handedness = Handedness.None, IMixedRealityInputSource inputSource = null)
        {
            using (RaisePointerDraggedPerfMarker.Auto())
            {
                pointerEventData.Initialize(pointer, inputAction, handedness, inputSource);

                HandlePointerEvent(pointerEventData, OnPointerDraggedEventHandler);
            }
        }

        #endregion Pointer Dragged

        #region Pointer Click

        public static readonly ExecuteEvents.EventFunction<IMixedRealityPointerHandler> OnInputClickedEventHandler =
                delegate (IMixedRealityPointerHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<MixedRealityPointerEventData>(eventData);
                    handler.OnPointerClicked(casted);
                };

        /// <inheritdoc />
        private static readonly ProfilerMarker RaisePointerClickedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePointerClicked");

        public void RaisePointerClicked(IMixedRealityPointer pointer, MixedRealityInputAction inputAction, int count, Handedness handedness = Handedness.None, IMixedRealityInputSource inputSource = null)
        {
            using (RaisePointerClickedPerfMarker.Auto())
            {
                // Create input event
                pointerEventData.Initialize(pointer, inputAction, handedness, inputSource, count);

                HandleClick();
            }
        }

        private void HandleClick()
        {
            // Pass handler through HandleEvent to perform modal/fallback logic
            HandlePointerEvent(pointerEventData, OnInputClickedEventHandler);

            // NOTE: In Unity UI, a "click" happens on every pointer up, so we have RaisePointerUp call the PointerHandler.
        }

        #endregion Pointer Click

        #region Pointer Up

        public static readonly ExecuteEvents.EventFunction<IMixedRealityPointerHandler> OnPointerUpEventHandler =
            delegate (IMixedRealityPointerHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<MixedRealityPointerEventData>(eventData);
                handler.OnPointerUp(casted);
            };

        private static readonly ProfilerMarker RaisePointerUpPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePointerUp");

        /// <inheritdoc />
        public void RaisePointerUp(IMixedRealityPointer pointer, MixedRealityInputAction inputAction, Handedness handedness = Handedness.None, IMixedRealityInputSource inputSource = null)
        {
            using (RaisePointerUpPerfMarker.Auto())
            {
                pointerEventData.Initialize(pointer, inputAction, handedness, inputSource);

                HandlePointerEvent(pointerEventData, OnPointerUpEventHandler);

                pointer.IsFocusLocked = false;
            }
        }

        #endregion Pointer Up

        #endregion Pointers

        #region Generic Input Events

        #region Input Down

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler> OnInputDownEventHandler =
            delegate (IMixedRealityInputHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                handler.OnInputDown(casted);
            };

        public static readonly ExecuteEvents.EventFunction<IMixedRealityBaseInputHandler> OnInputDownWithActionEventHandler =
            delegate (IMixedRealityBaseInputHandler handler, BaseEventData eventData)
            {
                var inputData = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                Debug.Assert(inputData.MixedRealityInputAction != MixedRealityInputAction.None);

                if (handler is IMixedRealityInputHandler inputHandler && !inputHandler.IsNull())
                {
                    inputHandler.OnInputDown(inputData);
                }

                if (handler is IMixedRealityInputActionHandler actionHandler && !actionHandler.IsNull())
                {
                    actionHandler.OnActionStarted(inputData);
                }
            };

        private static readonly ProfilerMarker RaiseOnInputDownPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseOnInputDown");

        /// <inheritdoc />
        public void RaiseOnInputDown(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction)
        {
            using (RaiseOnInputDownPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, true);

                // Create input event
                inputEventData.Initialize(source, handedness, inputAction);

                // Pass handler through HandleEvent to perform modal/fallback logic
                if (inputEventData.MixedRealityInputAction == MixedRealityInputAction.None)
                {
                    HandleEvent(inputEventData, OnInputDownEventHandler);
                }
                else
                {
                    HandleEvent(inputEventData, OnInputDownWithActionEventHandler);
                }
            }
        }

        #endregion Input Down

        #region Input Up

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler> OnInputUpEventHandler =
            delegate (IMixedRealityInputHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                handler.OnInputUp(casted);
            };

        public static readonly ExecuteEvents.EventFunction<IMixedRealityBaseInputHandler> OnInputUpWithActionEventHandler =
            delegate (IMixedRealityBaseInputHandler handler, BaseEventData eventData)
            {
                var inputData = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                Debug.Assert(inputData.MixedRealityInputAction != MixedRealityInputAction.None);

                if (handler is IMixedRealityInputHandler inputHandler && !inputHandler.IsNull())
                {
                    inputHandler.OnInputUp(inputData);
                }

                if (handler is IMixedRealityInputActionHandler actionHandler && !actionHandler.IsNull())
                {
                    actionHandler.OnActionEnded(inputData);
                }
            };

        private static readonly ProfilerMarker RaiseOnInputUpPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseOnInputUp");

        /// <inheritdoc />
        public void RaiseOnInputUp(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction)
        {
            using (RaiseOnInputUpPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, false);

                // Create input event
                inputEventData.Initialize(source, handedness, inputAction);

                // Pass handler through HandleEvent to perform modal/fallback logic
                if (inputEventData.MixedRealityInputAction == MixedRealityInputAction.None)
                {
                    HandleEvent(inputEventData, OnInputUpEventHandler);
                }
                else
                {
                    HandleEvent(inputEventData, OnInputUpWithActionEventHandler);
                }
            }
        }

        #endregion Input Up

        #region Float Input Changed

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler<float>> OnFloatInputChanged =
            delegate (IMixedRealityInputHandler<float> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<float>>(eventData);
                handler.OnInputChanged(casted);
            };

        private static readonly ProfilerMarker RaiseFloatInputChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseFloatInputChanged");

        /// <inheritdoc />
        public void RaiseFloatInputChanged(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction, float inputValue)
        {
            using (RaiseFloatInputChangedPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, inputValue);

                // Create input event
                floatInputEventData.Initialize(source, handedness, inputAction, inputValue);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(floatInputEventData, OnFloatInputChanged);
            }
        }

        #endregion Float Input Changed

        #region Input Position Changed

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler<Vector2>> OnTwoDoFInputChanged =
            delegate (IMixedRealityInputHandler<Vector2> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector2>>(eventData);
                handler.OnInputChanged(casted);
            };

        private static readonly ProfilerMarker RaisePositionInputChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePositionInputChanged");

        /// <inheritdoc />
        public void RaisePositionInputChanged(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction, Vector2 inputPosition)
        {
            using (RaisePositionInputChangedPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, inputPosition);

                // Create input event
                vector2InputEventData.Initialize(source, handedness, inputAction, inputPosition);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(vector2InputEventData, OnTwoDoFInputChanged);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler<Vector3>> OnPositionInputChanged =
            delegate (IMixedRealityInputHandler<Vector3> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector3>>(eventData);
                handler.OnInputChanged(casted);
            };

        /// <inheritdoc />
        public void RaisePositionInputChanged(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction, Vector3 position)
        {
            using (RaisePositionInputChangedPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, position);

                // Create input event
                positionInputEventData.Initialize(source, handedness, inputAction, position);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(positionInputEventData, OnPositionInputChanged);
            }
        }

        #endregion Input Position Changed

        #region Input Rotation Changed

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler<Quaternion>> OnRotationInputChanged =
            delegate (IMixedRealityInputHandler<Quaternion> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Quaternion>>(eventData);
                handler.OnInputChanged(casted);
            };

        private static readonly ProfilerMarker RaiseRotationInputChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseRotationInputChanged");

        /// <inheritdoc />
        public void RaiseRotationInputChanged(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction, Quaternion rotation)
        {
            using (RaiseRotationInputChangedPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, rotation);

                // Create input event
                rotationInputEventData.Initialize(source, handedness, inputAction, rotation);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(positionInputEventData, OnRotationInputChanged);
            }
        }

        #endregion Input Rotation Changed

        #region Input Pose Changed

        public static readonly ExecuteEvents.EventFunction<IMixedRealityInputHandler<MixedRealityPose>> OnPoseInputChanged =
            delegate (IMixedRealityInputHandler<MixedRealityPose> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<MixedRealityPose>>(eventData);
                handler.OnInputChanged(casted);
            };

        private static readonly ProfilerMarker RaisePoseInputChangedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaisePoseInputChanged");

        /// <inheritdoc />
        public void RaisePoseInputChanged(IMixedRealityInputSource source, Handedness handedness, MixedRealityInputAction inputAction, MixedRealityPose inputData)
        {
            using (RaisePoseInputChangedPerfMarker.Auto())
            {
                inputAction = ProcessRules(inputAction, inputData);

                // Create input event
                poseInputEventData.Initialize(source, handedness, inputAction, inputData);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(poseInputEventData, OnPoseInputChanged);
            }
        }

        #endregion Input Pose Changed

        #endregion Generic Input Events

        #region Gesture Events

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler> OnGestureStarted =
            delegate (IMixedRealityGestureHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                handler.OnGestureStarted(casted);
            };

        public static readonly ExecuteEvents.EventFunction<IMixedRealityBaseInputHandler> OnGestureStartedWithAction =
            delegate (IMixedRealityBaseInputHandler handler, BaseEventData eventData)
            {
                var inputData = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                Debug.Assert(inputData.MixedRealityInputAction != MixedRealityInputAction.None);

                if (handler is IMixedRealityGestureHandler gestureHandler && !gestureHandler.IsNull())
                {
                    gestureHandler.OnGestureStarted(inputData);
                }

                if (handler is IMixedRealityInputActionHandler actionHandler && !actionHandler.IsNull())
                {
                    actionHandler.OnActionStarted(inputData);
                }
            };

        private static readonly ProfilerMarker RaiseGestureStartedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseGestureStarted");

        /// <inheritdoc />
        public void RaiseGestureStarted(IMixedRealityController controller, MixedRealityInputAction action)
        {
            using (RaiseGestureStartedPerfMarker.Auto())
            {
                action = ProcessRules(action, true);
                inputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action);

                if (action == MixedRealityInputAction.None)
                {
                    HandleEvent(inputEventData, OnGestureStarted);
                }
                else
                {
                    HandleEvent(inputEventData, OnGestureStartedWithAction);
                }
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler> OnGestureUpdated =
            delegate (IMixedRealityGestureHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                handler.OnGestureUpdated(casted);
            };

        private static readonly ProfilerMarker RaiseGestureUpdatedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseGestureUpdated");

        /// <inheritdoc />
        public void RaiseGestureUpdated(IMixedRealityController controller, MixedRealityInputAction action)
        {
            action = ProcessRules(action, true);
            inputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action);
            HandleEvent(inputEventData, OnGestureUpdated);
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Vector2>> OnGestureVector2PositionUpdated =
                delegate (IMixedRealityGestureHandler<Vector2> handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector2>>(eventData);
                    handler.OnGestureUpdated(casted);
                };

        /// <inheritdoc />
        public void RaiseGestureUpdated(IMixedRealityController controller, MixedRealityInputAction action, Vector2 inputData)
        {
            using (RaiseGestureUpdatedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                vector2InputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(vector2InputEventData, OnGestureVector2PositionUpdated);

            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Vector3>> OnGesturePositionUpdated =
            delegate (IMixedRealityGestureHandler<Vector3> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector3>>(eventData);
                handler.OnGestureUpdated(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureUpdated(IMixedRealityController controller, MixedRealityInputAction action, Vector3 inputData)
        {
            using (RaiseGestureUpdatedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                positionInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(positionInputEventData, OnGesturePositionUpdated);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Quaternion>> OnGestureRotationUpdated =
            delegate (IMixedRealityGestureHandler<Quaternion> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Quaternion>>(eventData);
                handler.OnGestureUpdated(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureUpdated(IMixedRealityController controller, MixedRealityInputAction action, Quaternion inputData)
        {
            using (RaiseGestureUpdatedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                rotationInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(rotationInputEventData, OnGestureRotationUpdated);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<MixedRealityPose>> OnGesturePoseUpdated =
            delegate (IMixedRealityGestureHandler<MixedRealityPose> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<MixedRealityPose>>(eventData);
                handler.OnGestureUpdated(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureUpdated(IMixedRealityController controller, MixedRealityInputAction action, MixedRealityPose inputData)
        {
            using (RaiseGestureUpdatedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                poseInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(poseInputEventData, OnGesturePoseUpdated);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler> OnGestureCompleted =
            delegate (IMixedRealityGestureHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                handler.OnGestureCompleted(casted);
            };

        public static readonly ExecuteEvents.EventFunction<IMixedRealityBaseInputHandler> OnGestureCompletedWithAction =
            delegate (IMixedRealityBaseInputHandler handler, BaseEventData eventData)
            {
                var inputData = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                Debug.Assert(inputData.MixedRealityInputAction != MixedRealityInputAction.None);

                if (handler is IMixedRealityGestureHandler gestureHandler && !gestureHandler.IsNull())
                {
                    gestureHandler.OnGestureCompleted(inputData);
                }

                if (handler is IMixedRealityInputActionHandler actionHandler && !actionHandler.IsNull())
                {
                    actionHandler.OnActionEnded(inputData);
                }
            };

        private static readonly ProfilerMarker RaiseGestureCompletedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseGestureCompleted");

        /// <inheritdoc />
        public void RaiseGestureCompleted(IMixedRealityController controller, MixedRealityInputAction action)
        {
            using (RaiseGestureCompletedPerfMarker.Auto())
            {
                action = ProcessRules(action, false);
                inputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action);

                if (action == MixedRealityInputAction.None)
                {
                    HandleEvent(inputEventData, OnGestureCompleted);
                }
                else
                {
                    HandleEvent(inputEventData, OnGestureCompletedWithAction);
                }
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Vector2>> OnGestureVector2PositionCompleted =
                delegate (IMixedRealityGestureHandler<Vector2> handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector2>>(eventData);
                    handler.OnGestureCompleted(casted);
                };

        /// <inheritdoc />
        public void RaiseGestureCompleted(IMixedRealityController controller, MixedRealityInputAction action, Vector2 inputData)
        {
            using (RaiseGestureCompletedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                vector2InputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(vector2InputEventData, OnGestureVector2PositionCompleted);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Vector3>> OnGesturePositionCompleted =
            delegate (IMixedRealityGestureHandler<Vector3> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Vector3>>(eventData);
                handler.OnGestureCompleted(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureCompleted(IMixedRealityController controller, MixedRealityInputAction action, Vector3 inputData)
        {
            using (RaiseGestureCompletedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                positionInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(positionInputEventData, OnGesturePositionCompleted);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<Quaternion>> OnGestureRotationCompleted =
            delegate (IMixedRealityGestureHandler<Quaternion> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<Quaternion>>(eventData);
                handler.OnGestureCompleted(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureCompleted(IMixedRealityController controller, MixedRealityInputAction action, Quaternion inputData)
        {
            using (RaiseGestureCompletedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                rotationInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(rotationInputEventData, OnGestureRotationCompleted);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler<MixedRealityPose>> OnGesturePoseCompleted =
            delegate (IMixedRealityGestureHandler<MixedRealityPose> handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<MixedRealityPose>>(eventData);
                handler.OnGestureCompleted(casted);
            };

        /// <inheritdoc />
        public void RaiseGestureCompleted(IMixedRealityController controller, MixedRealityInputAction action, MixedRealityPose inputData)
        {
            using (RaiseGestureCompletedPerfMarker.Auto())
            {
                action = ProcessRules(action, inputData);
                poseInputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action, inputData);
                HandleEvent(poseInputEventData, OnGesturePoseCompleted);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityGestureHandler> OnGestureCanceled =
                delegate (IMixedRealityGestureHandler handler, BaseEventData eventData)
                {
                    var casted = ExecuteEvents.ValidateEventData<InputEventData>(eventData);
                    handler.OnGestureCanceled(casted);
                };

        private static readonly ProfilerMarker RaiseGestureCanceledPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseGestureCanceled");

        /// <inheritdoc />
        public void RaiseGestureCanceled(IMixedRealityController controller, MixedRealityInputAction action)
        {
            using (RaiseGestureCanceledPerfMarker.Auto())
            {
                action = ProcessRules(action, false);
                inputEventData.Initialize(controller.InputSource, controller.ControllerHandedness, action);
                HandleEvent(inputEventData, OnGestureCanceled);
            }
        }

        #endregion Gesture Events

        #region Speech Keyword Events

        public static readonly ExecuteEvents.EventFunction<IMixedRealitySpeechHandler> OnSpeechKeywordRecognizedEventHandler =
            delegate (IMixedRealitySpeechHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<SpeechEventData>(eventData);
                handler.OnSpeechKeywordRecognized(casted);
            };

        public static readonly ExecuteEvents.EventFunction<IMixedRealityBaseInputHandler> OnSpeechKeywordRecognizedWithActionEventHandler =
            delegate (IMixedRealityBaseInputHandler handler, BaseEventData eventData)
            {
                var speechData = ExecuteEvents.ValidateEventData<SpeechEventData>(eventData);
                Debug.Assert(speechData.MixedRealityInputAction != MixedRealityInputAction.None);

                if (handler is IMixedRealitySpeechHandler speechHandler && !speechHandler.IsNull())
                {
                    speechHandler.OnSpeechKeywordRecognized(speechData);
                }

                if (handler is IMixedRealityInputActionHandler actionHandler && !actionHandler.IsNull())
                {
                    actionHandler.OnActionStarted(speechData);
                    actionHandler.OnActionEnded(speechData);
                }
            };

        private static readonly ProfilerMarker RaiseSpeechCommandRecognizedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseSpeechCommandRecognized");

        /// <inheritdoc />
        public void RaiseSpeechCommandRecognized(IMixedRealityInputSource source, RecognitionConfidenceLevel confidence, TimeSpan phraseDuration, DateTime phraseStartTime, SpeechCommands command)
        {
            using (RaiseSpeechCommandRecognizedPerfMarker.Auto())
            {
                // Create input event
                speechEventData.Initialize(source, confidence, phraseDuration, phraseStartTime, command);

                FocusProvider?.OnSpeechKeywordRecognized(speechEventData);

                // Pass handler through HandleEvent to perform modal/fallback logic
                if (command.Action == MixedRealityInputAction.None)
                {
                    HandleEvent(speechEventData, OnSpeechKeywordRecognizedEventHandler);
                }
                else
                {
                    HandleEvent(speechEventData, OnSpeechKeywordRecognizedWithActionEventHandler);
                }
            }
        }

        #endregion Speech Keyword Events

        #region Dictation Events

        public static readonly ExecuteEvents.EventFunction<IMixedRealityDictationHandler> OnDictationHypothesisEventHandler =
            delegate (IMixedRealityDictationHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<DictationEventData>(eventData);
                handler.OnDictationHypothesis(casted);
            };

        private static readonly ProfilerMarker RaiseDictationHypothesisPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseDictationHypothesis");

        /// <inheritdoc />
        public void RaiseDictationHypothesis(IMixedRealityInputSource source, string dictationHypothesis, AudioClip dictationAudioClip = null)
        {
            using (RaiseDictationHypothesisPerfMarker.Auto())
            {
                // Create input event
                dictationEventData.Initialize(source, dictationHypothesis, dictationAudioClip);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(dictationEventData, OnDictationHypothesisEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityDictationHandler> OnDictationResultEventHandler =
            delegate (IMixedRealityDictationHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<DictationEventData>(eventData);
                handler.OnDictationResult(casted);
            };

        private static readonly ProfilerMarker RaiseDictationResultPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseDictationResult");

        /// <inheritdoc />
        public void RaiseDictationResult(IMixedRealityInputSource source, string dictationResult, AudioClip dictationAudioClip = null)
        {
            using (RaiseDictationResultPerfMarker.Auto())
            {
                // Create input event
                dictationEventData.Initialize(source, dictationResult, dictationAudioClip);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(dictationEventData, OnDictationResultEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityDictationHandler> OnDictationCompleteEventHandler =
            delegate (IMixedRealityDictationHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<DictationEventData>(eventData);
                handler.OnDictationComplete(casted);
            };

        private static readonly ProfilerMarker RaiseDictationCompletePerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseDictationComplete");

        /// <inheritdoc />
        public void RaiseDictationComplete(IMixedRealityInputSource source, string dictationResult, AudioClip dictationAudioClip)
        {
            using (RaiseDictationCompletePerfMarker.Auto())
            {
                // Create input event
                dictationEventData.Initialize(source, dictationResult, dictationAudioClip);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(dictationEventData, OnDictationCompleteEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityDictationHandler> OnDictationErrorEventHandler =
            delegate (IMixedRealityDictationHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<DictationEventData>(eventData);
                handler.OnDictationError(casted);
            };

        private static readonly ProfilerMarker RaiseDictationErrorPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseDictationError");

        /// <inheritdoc />
        public void RaiseDictationError(IMixedRealityInputSource source, string dictationResult, AudioClip dictationAudioClip = null)
        {
            using (RaiseDictationErrorPerfMarker.Auto())
            {
                // Create input event
                dictationEventData.Initialize(source, dictationResult, dictationAudioClip);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(dictationEventData, OnDictationErrorEventHandler);
            }
        }

        #endregion Dictation Events

        #region Hand Events

        public static readonly ExecuteEvents.EventFunction<IMixedRealityHandJointHandler> OnHandJointsUpdatedEventHandler =
            delegate (IMixedRealityHandJointHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>>>(eventData);

                handler.OnHandJointsUpdated(casted);
            };

        private static readonly ProfilerMarker RaiseHandJointsUpdatedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseHandJointsUpdated");

        public void RaiseHandJointsUpdated(IMixedRealityInputSource source, Handedness handedness, IDictionary<TrackedHandJoint, MixedRealityPose> jointPoses)
        {
            using (RaiseHandJointsUpdatedPerfMarker.Auto())
            {
                // Create input event
                jointPoseInputEventData.Initialize(source, handedness, MixedRealityInputAction.None, jointPoses);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(jointPoseInputEventData, OnHandJointsUpdatedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityHandMeshHandler> OnHandMeshUpdatedEventHandler =
            delegate (IMixedRealityHandMeshHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<InputEventData<HandMeshInfo>>(eventData);

                handler.OnHandMeshUpdated(casted);
            };

        private static readonly ProfilerMarker RaiseHandMeshUpdatedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseHandMeshUpdated");

        public void RaiseHandMeshUpdated(IMixedRealityInputSource source, Handedness handedness, HandMeshInfo handMeshInfo)
        {
            using (RaiseHandMeshUpdatedPerfMarker.Auto())
            {
                // Create input event
                handMeshInputEventData.Initialize(source, handedness, MixedRealityInputAction.None, handMeshInfo);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(handMeshInputEventData, OnHandMeshUpdatedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityTouchHandler> OnTouchStartedEventHandler =
            delegate (IMixedRealityTouchHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<HandTrackingInputEventData>(eventData);
                handler.OnTouchStarted(casted);
            };

        private static readonly ProfilerMarker RaiseOnTouchStartedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseOnTouchStarted");

        /// <inheritdoc />
        public void RaiseOnTouchStarted(IMixedRealityInputSource source, IMixedRealityController controller, Handedness handedness, Vector3 touchPoint)
        {
            using (RaiseOnTouchStartedPerfMarker.Auto())
            {
                // Create input event
                handTrackingInputEventData.Initialize(source, controller, handedness, touchPoint);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(handTrackingInputEventData, OnTouchStartedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityTouchHandler> OnTouchCompletedEventHandler =
            delegate (IMixedRealityTouchHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<HandTrackingInputEventData>(eventData);
                handler.OnTouchCompleted(casted);
            };


        private static readonly ProfilerMarker RaiseOnTouchCompletedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseOnTouchCompleted");

        /// <inheritdoc />
        public void RaiseOnTouchCompleted(IMixedRealityInputSource source, IMixedRealityController controller, Handedness handedness, Vector3 touchPoint)
        {
            using (RaiseOnTouchCompletedPerfMarker.Auto())
            {
                // Create input event
                handTrackingInputEventData.Initialize(source, controller, handedness, touchPoint);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(handTrackingInputEventData, OnTouchCompletedEventHandler);
            }
        }

        public static readonly ExecuteEvents.EventFunction<IMixedRealityTouchHandler> OnTouchUpdatedEventHandler =
            delegate (IMixedRealityTouchHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<HandTrackingInputEventData>(eventData);
                handler.OnTouchUpdated(casted);
            };


        private static readonly ProfilerMarker RaiseOnTouchUpdatedPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.RaiseOnTouchUpdated");

        /// <inheritdoc />
        public void RaiseOnTouchUpdated(IMixedRealityInputSource source, IMixedRealityController controller, Handedness handedness, Vector3 touchPoint)
        {
            using (RaiseOnTouchUpdatedPerfMarker.Auto())
            {
                // Create input event
                handTrackingInputEventData.Initialize(source, controller, handedness, touchPoint);

                // Pass handler through HandleEvent to perform modal/fallback logic
                HandleEvent(handTrackingInputEventData, OnTouchUpdatedEventHandler);
            }
        }

        #endregion Hand Events

        #endregion Input Events

        #region Rules

        private static readonly ProfilerMarker ProcessRulesInternalPerfMarker = new ProfilerMarker("[MRTK] MixedRealityInputSystem.ProcessRules_Internal");

        private static MixedRealityInputAction ProcessRules_Internal<T1, T2>(MixedRealityInputAction inputAction, T1[] inputActionRules, T2 criteria) where T1 : struct, IInputActionRule<T2>
        {
            using (ProcessRulesInternalPerfMarker.Auto())
            {
                for (int i = 0; i < inputActionRules.Length; i++)
                {
                    if (inputActionRules[i].BaseAction == inputAction && inputActionRules[i].Criteria.Equals(criteria))
                    {
                        if (inputActionRules[i].RuleAction == inputAction)
                        {
                            Debug.LogError("Input Action Rule cannot be the same as the rule's Base Action!");
                            return inputAction;
                        }

                        if (inputActionRules[i].BaseAction.AxisConstraint != inputActionRules[i].RuleAction.AxisConstraint)
                        {
                            Debug.LogError("Input Action Rule doesn't have the same Axis Constraint as the Base Action!");
                            return inputAction;
                        }

                        return inputActionRules[i].RuleAction;
                    }
                }

                return inputAction;
            }
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, bool criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesDigital?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesDigital, criteria);
            }

            return inputAction;
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, float criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesSingleAxis?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesSingleAxis, criteria);
            }

            return inputAction;
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, Vector2 criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesDualAxis?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesDualAxis, criteria);
            }

            return inputAction;
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, Vector3 criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesVectorAxis?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesVectorAxis, criteria);
            }

            return inputAction;
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, Quaternion criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesQuaternionAxis?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesQuaternionAxis, criteria);
            }

            return inputAction;
        }

        private MixedRealityInputAction ProcessRules(MixedRealityInputAction inputAction, MixedRealityPose criteria)
        {
            if (CurrentInputActionRulesProfile != null && CurrentInputActionRulesProfile.InputActionRulesPoseAxis?.Length > 0)
            {
                return ProcessRules_Internal(inputAction, CurrentInputActionRulesProfile.InputActionRulesPoseAxis, criteria);
            }

            return inputAction;
        }

        #endregion Rules
    }
}
