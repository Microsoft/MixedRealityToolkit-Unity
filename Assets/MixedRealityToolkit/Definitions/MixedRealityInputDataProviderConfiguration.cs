﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    [Serializable]
    public struct MixedRealityInputDataProviderConfiguration : IMixedRealityServiceConfiguration
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
        [Implements(typeof(IPlatformSupport), TypeGrouping.ByNamespaceFlat)]
        private SystemType[] runtimePlatform;

        /// <inheritdoc />
        private IPlatformSupport[] supportedPlatforms;

        /// <inheritdoc />
        public IPlatformSupport[] SupportedPlatforms
        {
            get
            {
                if (supportedPlatforms == null)
                {
                    supportedPlatforms = runtimePlatform.Convert();
                }

                return supportedPlatforms;
            }
        }

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
        /// <param name="profile">The configuration profile for the data provider.</param>
        public MixedRealityInputDataProviderConfiguration(
            SystemType componentType,
            string componentName,
            uint priority,
            IPlatformSupport[] runtimePlatform,
            BaseMixedRealityProfile profile)
        {
            this.componentType = componentType;
            this.componentName = componentName;
            this.priority = priority;
            this.runtimePlatform = runtimePlatform.Convert();
            this.supportedPlatforms = runtimePlatform;
            deviceManagerProfile = profile;
        }
    }
}