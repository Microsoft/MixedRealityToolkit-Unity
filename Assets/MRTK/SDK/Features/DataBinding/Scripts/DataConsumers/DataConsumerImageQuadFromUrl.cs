// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A data consumer that will fetch any supported image type from a URL
    /// and associated with a Quad MeshRenderer component  being managed by this
    /// object.
    /// </summary>
    
    [Serializable]
    public class DataConsumerImageQuadFromUrl : DataConsumerImageTextureFromUrl
    {
        [Tooltip("(Optional) Specific mesh renderer to populate with a retrieved image. If not specified, first MeshRenderer found in this or children will be used.")]
        [SerializeField] 
        private MeshRenderer imageQuadMeshRenderer;

        [Tooltip("(Optional) Default material to use for showing image as a texture.")]
        [SerializeField]
        private Material defaultMaterial;

        protected override Type[] GetComponentTypes()
        {

            Type[] types = { typeof(MeshRenderer) };
            return types;
        }


        protected override void InitializeForComponent(Type componentType, Component component)
        {
            if (imageQuadMeshRenderer == null)
            {
                // No specific MeshRenderer specified, so assign the one that was found for us.

                imageQuadMeshRenderer = component as MeshRenderer;
            }

            if (defaultMaterial != null )
            {
                imageQuadMeshRenderer.material = defaultMaterial;
            }
        }

 

        protected override void PlaceImageTexture(Texture2D imageTexture)
        {
            imageQuadMeshRenderer.material.mainTexture = imageTexture;

        }

    }
}
