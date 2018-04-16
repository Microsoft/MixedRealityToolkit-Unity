﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Internal.Interfaces;
using Microsoft.MixedReality.Toolkit.Internal.Managers;
using Microsoft.MixedReality.Toolkit.Internal.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.InputSystem.Utilities
{
    /// <summary>
    /// Helper class for setting up canvases for use in the MRTK.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class CanvasHelper : MonoBehaviour
    {
        /// <summary>
        /// The canvas this helper script is targeting.
        /// </summary>
        public Canvas Canvas { get; set; }

        private void Awake()
        {
            if (Canvas == null)
            {
                Canvas = GetComponent<Canvas>();
            }
        }

        private void Start()
        {
            DebugUtilities.DebugAssert(Canvas != null);

            if (Canvas.isRootCanvas && Canvas.renderMode == RenderMode.WorldSpace)
            {
                Canvas.worldCamera = MixedRealityManager.Instance.GetManager<IMixedRealityInputSystem>().FocusProvider.UIRaycastCamera;
            }
        }
    }
}
