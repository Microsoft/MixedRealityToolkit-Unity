﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    [Serializable]
    public class MixedRealityInputDataProviderConfiguration : IMixedRealityServiceConfiguration
    {
        [SerializeField]
        [Implements(typeof(IMixedRealityInputDeviceManager), TypeGrouping.ByNamespaceFlat)]
        private SystemType componentType;

        /// <inheritdoc />
        public SystemType ComponentType => componentType;

        [SerializeField]
        private string componentName;

        /// <inheritdoc />
        public string ComponentName => componentName;

        [SerializeField]
        private uint priority;

        /// <inheritdoc />
        public uint Priority => priority;

        [SerializeField]
        [EnumFlags]
        private SupportedPlatforms runtimePlatform = (SupportedPlatforms)(-1);

        /// <inheritdoc />
        public SupportedPlatforms RuntimePlatform => runtimePlatform;

        [EnumFlags]
        [SerializeField]
        private SupportedApplicationModes runtimeModes = (SupportedApplicationModes)(-1);

        /// <inheritdoc />
        public SupportedApplicationModes RuntimeModes => runtimeModes;

        [SerializeField]
        private BaseMixedRealityProfile deviceManagerProfile;

        /// <summary>
        /// Device manager specific configuration profile.
        /// </summary>
        public BaseMixedRealityProfile DeviceManagerProfile => deviceManagerProfile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="componentType">The <see cref="Microsoft.MixedReality.Toolkit.Utilities.SystemType"/> of the data provider.</param>
        /// <param name="componentName">The friendly name of the data provider.</param>
        /// <param name="priority">The load priority of the data provider.</param>
        /// <param name="runtimePlatform">The runtime platform(s) supported by the data provider.</param>
        /// <param name="applicationModes">The runtime environment mode(s) supported by the data provider</param>
        /// <param name="profile">The configuration profile for the data provider.</param>
        public MixedRealityInputDataProviderConfiguration(
            SystemType componentType,
            string componentName,
            uint priority,
            SupportedPlatforms runtimePlatform,
            SupportedApplicationModes applicationModes,
            BaseMixedRealityProfile profile)
        {
            this.componentType = componentType;
            this.componentName = componentName;
            this.priority = priority;
            this.runtimePlatform = runtimePlatform;
            this.runtimeModes = applicationModes;
            this.deviceManagerProfile = profile;
        }
    }
}