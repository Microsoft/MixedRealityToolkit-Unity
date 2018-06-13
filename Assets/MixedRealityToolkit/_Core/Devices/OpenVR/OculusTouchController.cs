﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Internal.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Internal.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Internal.Interfaces;
using Microsoft.MixedReality.Toolkit.Internal.Interfaces.InputSystem;
using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Internal.Devices.OpenVR
{
    // TODO
    public struct OculusTouchController : IMixedRealityController
    {
        public OculusTouchController(ControllerState controllerState, Handedness controllerHandedness, IMixedRealityInputSource inputSource, Dictionary<DeviceInputType, IInteractionMapping> interactions = null) : this()
        {
            ControllerState = controllerState;
            ControllerHandedness = controllerHandedness;
            InputSource = inputSource;
            Interactions = interactions ?? new Dictionary<DeviceInputType, IInteractionMapping>();
        }

        /// <inheritdoc />
        public ControllerState ControllerState { get; }

        /// <inheritdoc />
        public Handedness ControllerHandedness { get; }

        /// <inheritdoc />
        public IMixedRealityInputSource InputSource { get; }

        /// <inheritdoc />
        public Dictionary<DeviceInputType, IInteractionMapping> Interactions { get; }

        /// <inheritdoc />
        public void SetupInputSource<T>(T state)
        {
            // TODO
        }

        /// <inheritdoc />
        public void UpdateInputSource<T>(T state)
        {
            //TODO
        }
    }
}
