﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControlTypes;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControl
{
    /// <summary>
    /// Configuration for <see cref="Links"/> used in <see cref="BoundsControl"/>
    /// This class provides all data members needed to create a link of a bounds control
    /// </summary>
    [CreateAssetMenu(fileName = "LinksConfiguration", menuName = "Mixed Reality Toolkit/Experimental/Bounds Control/Links Configuration")]
    public class LinksConfiguration : ScriptableObject
    {
        #region Serialized Properties
        [SerializeField]
        [Tooltip("Material used for wireframe display")]
        private Material wireframeMaterial;

        /// <summary>
        /// Material used for wireframe display
        /// </summary>
        public Material WireframeMaterial
        {
            get { return wireframeMaterial; }
            set
            {
                if (wireframeMaterial != value)
                {
                    wireframeMaterial = value;
                    TrySetDefaultMaterial();
                    //configurationChanged.Invoke();
                    // update materials
                    wireFrameChanged.Invoke(WireframeChangedEventType.MATERIAL);
                }
            }
        }

        [SerializeField]
        [Tooltip("Radius for wireframe edges")]
        private float wireframeEdgeRadius = 0.001f; 

        /// <summary>
        /// Radius for wireframe edges
        /// </summary>
        public float WireframeEdgeRadius
        {
            get { return wireframeEdgeRadius; }
            set
            {
                if (wireframeEdgeRadius != value)
                {
                    wireframeEdgeRadius = value;
                    wireFrameChanged.Invoke(WireframeChangedEventType.RADIUS);
                }
            }
        }

        [SerializeField]
        [Tooltip("Shape used for wireframe display")]
        private WireframeType wireframeShape = WireframeType.Cubic;

        /// <summary>
        /// Shape used for wireframe display
        /// </summary>
        public WireframeType WireframeShape
        {
            get { return wireframeShape; }
            set
            {
                if (wireframeShape != value)
                {
                    wireframeShape = value;
                    wireFrameChanged.Invoke(WireframeChangedEventType.SHAPE);
                }
            }
        }

        [SerializeField]
        [Tooltip("Show a wireframe around the bounds control when checked. Wireframe parameters below have no effect unless this is checked")]
        private bool showWireframe = true;

        /// <summary>
        /// Show a wireframe around the bounds control when checked. Wireframe parameters below have no effect unless this is checked
        /// </summary>
        public bool ShowWireFrame
        {
            get { return showWireframe; }
            set
            {
                if (showWireframe != value)
                {
                    showWireframe = value;
                    wireFrameChanged.Invoke(WireframeChangedEventType.VISIBILITY);
                }
            }
        }

        #endregion Serialized Properties

        internal enum WireframeChangedEventType
        {
            VISIBILITY,
            RADIUS,
            SHAPE,
            MATERIAL
        }
        internal class WireFrameEvent : UnityEvent<WireframeChangedEventType> { }
        internal WireFrameEvent wireFrameChanged = new WireFrameEvent();

        public void Awake()
        {
            TrySetDefaultMaterial();
        }

        private void TrySetDefaultMaterial()
        {
            if (wireframeMaterial == null)
            {
                wireframeMaterial = VisualUtils.CreateDefaultMaterial();
            }
        }
    }
}
