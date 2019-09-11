﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.Toolkit
{
    /// <summary>
    /// Abstract class for core MRTK system with functionality defined for managing and accessing IMixedRealityDataProviders
    /// </summary>
    public abstract class BaseDataProviderAccessCoreSystem : BaseCoreSystem, IMixedRealityDataProviderAccess
    {
        private List<IMixedRealityDataProvider> dataProviders = new List<IMixedRealityDataProvider>();

        protected bool isDestroyed = false;

        /// <inheritdoc />
        public override void Destroy()
        {
            isDestroyed = true;
            base.Destroy();
        }

        public override void Reset()
        {
            base.Reset();

            foreach(var provider in dataProviders)
            {
                provider.Reset();
                if (isDestroyed) { break; }
            }
        }

        /// <inheritdoc />
        public override void Enable()
        {
            base.Enable();

            foreach (var provider in dataProviders)
            {
                provider.Enable();
                if (isDestroyed) { break; }
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            foreach (var provider in dataProviders)
            {
                provider.Update();
                if (isDestroyed) { break; }
            }
        }

        /// <inheritdoc />
        public override void LateUpdate()
        {
            base.LateUpdate();

            foreach (var provider in dataProviders)
            {
                provider.LateUpdate();
                if (isDestroyed) { break; }
            }
        }

        public BaseDataProviderAccessCoreSystem(
            IMixedRealityServiceRegistrar registrar,
            BaseMixedRealityProfile profile = null) : base(registrar, profile)
        {
        }

        #region IMixedRealityDataProviderAccess Implementation

        /// <inheritdoc />
        public virtual IReadOnlyList<IMixedRealityDataProvider> GetDataProviders()
        {
            return dataProviders.AsReadOnly();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<T> GetDataProviders<T>() where T : IMixedRealityDataProvider
        {
            List<T> selected = new List<T>();

            foreach (var provider in dataProviders)
            {
                if (provider is T)
                {
                    selected.Add((T)provider);
                }
            }

            return selected;
        }

        /// <inheritdoc />
        public virtual IMixedRealityDataProvider GetDataProvider(string name)
        {
            foreach (var provider in dataProviders)
            {
                if (provider.Name == name)
                {
                    return provider;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public virtual T GetDataProvider<T>(string name = null) where T : IMixedRealityDataProvider
        {
            foreach (var provider in dataProviders)
            {
                if (provider is T)
                {
                    if (name == null || provider.Name == name)
                    {
                        return (T)provider;
                    }
                }
            }

            return default(T);
        }

        #endregion IMixedRealityDataProviderAccess Implementation

        /// <summary>
        /// Registers a data provider of the specified type.
        /// </summary>
        /// <typeparam name="T">The interface type of the data provider to be registered.</typeparam>
        /// <returns>True if the data provider was successfully registered, false otherwise.</returns>
        protected bool RegisterDataProvider<T>(
            Type concreteType,
            SupportedPlatforms supportedPlatforms = (SupportedPlatforms)(-1),
            params object[] args) where T : IMixedRealityDataProvider
        {
#if !UNITY_EDITOR
            if (!Application.platform.IsPlatformSupported(supportedPlatforms))
#else
            if (!EditorUserBuildSettings.activeBuildTarget.IsPlatformSupported(supportedPlatforms))
#endif
            {
                return false;
            }

            if (concreteType == null)
            {
                Debug.LogError($"Unable to register {typeof(T).Name} service with a null concrete type.");
                return false;
            }

            if (!typeof(IMixedRealityDataProvider).IsAssignableFrom(concreteType))
            {
                Debug.LogError($"Unable to register the {concreteType.Name} data provider. It does not implement {typeof(IMixedRealityDataProvider)}.");
                return false;
            }

            T dataProviderInstance;

            try
            {
                dataProviderInstance = (T)Activator.CreateInstance(concreteType, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to register the {concreteType.Name} data provider: {e.GetType()} - {e.Message}");

                // Failures to create the concrete type generally surface as nested exceptions - just logging
                // the top level exception itself may not be helpful. If there is a nested exception (for example,
                // null reference in the constructor of the object itself), it's helpful to also surface those here.
                if (e.InnerException != null)
                {
                    Debug.LogError("Underlying exception information: " + e.InnerException);
                }
                return false;
            }

            return RegisterDataProvider(dataProviderInstance);
        }

        /// <summary>
        /// Registers a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The interface type of the data provider to be registered.</typeparam>
        /// <param name="dataProviderInstance">An instance of the data provider to be registered.</param>
        protected bool RegisterDataProvider<T>(T dataProviderInstance) where T : IMixedRealityDataProvider
        {
            if (dataProviderInstance == null)
            {
                Debug.LogWarning($"Unable to add a {dataProviderInstance.Name} data provider with a null instance.");
                return false;
            }

            dataProviders.Add(dataProviderInstance);
            dataProviderInstance.Initialize();

            return true;
        }

        /// <summary>
        /// Unregisters a data provider of the specified type.
        /// </summary>
        /// <typeparam name="T">The interface type of the data provider to be unregistered.</typeparam>
        /// <param name="name">The name of the data provider to unregister.</param>
        /// <returns>True if the data provider was successfully unregistered, false otherwise.</returns>
        /// <remarks>If the name argument is not specified, the first instance will be unregistered</remarks>
        protected bool UnregisterDataProvider<T>(string name = null) where T : IMixedRealityDataProvider
        {
            T dataProviderInstance = GetDataProvider<T>(name);

            if (dataProviderInstance == null) { return false; }

            return UnregisterDataProvider(dataProviderInstance);
        }

        /// <summary>
        /// Unregisters a data provider.
        /// </summary>
        /// <typeparam name="T">The interface type of the data provider to be unregistered.</typeparam>
        /// <param name="service">The specific data provider instance to unregister.</param>
        /// <returns>True if the data provider was successfully unregistered, false otherwise.</returns>
        protected bool UnregisterDataProvider<T>(T dataProviderInstance) where T : IMixedRealityDataProvider
        {
            if (dataProviderInstance == null)
            {
                return false;
            }

            if (dataProviders.Contains(dataProviderInstance))
            {
                dataProviders.Remove(dataProviderInstance);

                dataProviderInstance.Disable();
                dataProviderInstance.Destroy();

                return true;
            }

            return false;
        }

    }
}
