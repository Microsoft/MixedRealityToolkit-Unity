// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Rendering;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// An abstract primitive component to animate and visualize a clipping primitive that can be 
    /// used to drive per pixel based clipping.
    /// </summary>
    [ExecuteAlways]
    [HelpURL("https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/rendering/clipping-primitive")]
    public abstract class ClippingPrimitive : MonoBehaviour, IMaterialInstanceOwner
    {
        private const int InitialCollectionSize = 256;

        /// <summary>
        /// The renderer(s) that should be affected by the primitive. This collection is a copy of internal HashSet.
        /// </summary>
        private readonly Dictionary<Renderer, MaterialInstance> renderers =
            new Dictionary<Renderer, MaterialInstance>(InitialCollectionSize);

        [Tooltip("The renderer(s) that should be affected by the primitive.")]
        [SerializeField]
        private List<Renderer> renderersCache = new List<Renderer>(InitialCollectionSize);
        public enum Side
        {
            Inside = 1,
            Outside = -1
        }

        [Tooltip("Which side of the primitive to clip pixels against.")]
        [SerializeField]
        protected Side clippingSide = Side.Inside;

        /// <summary>
        /// The renderer(s) that should be affected by the primitive.
        /// </summary>
        public Side ClippingSide
        {
            get => clippingSide;
            set => clippingSide = value;
        }

        [SerializeField]
        [Tooltip("Toggles whether the primitive will use the Camera OnPreRender event")]
        private bool useOnPreRender;

        [SerializeField, Tooltip("Controls clipping features on the shared materials rather than material instances.")]
        private bool applyToSharedMaterial = false;

        /// <summary>
        /// Toggles whether the clipping features will apply to shared materials or material instances (default).
        /// </summary>
        /// <remarks>
        /// Applying to shared materials will allow for GPU instancing to batch calls between Renderers that interact with the same clipping primitives.
        /// </remarks>
        public bool ApplyToSharedMaterial
        {
            get => applyToSharedMaterial;
            set
            {
                if (value != applyToSharedMaterial)
                {
                    if (renderers.Count > 0)
                    {
                        throw new InvalidOperationException("Cannot change material applied to after renderers have been added.");
                    }
                    applyToSharedMaterial = value;
                }
            }
        }

        /// <summary>
        /// Toggles whether the primitive will use the Camera OnPreRender event.
        /// </summary>
        /// <remarks>
        /// This is especially helpful if you're trying to clip dynamically created objects that may be added to the scene after LateUpdate such as OnWillRender 
        /// </remarks>
        public bool UseOnPreRender
        {
            get => useOnPreRender;
            set
            {
                if (cameraMethods == null)
                {
                    cameraMethods = CameraCache.Main.gameObject.EnsureComponent<CameraEventRouter>();
                }

                if (value)
                {
                    cameraMethods.OnCameraPreRender += OnCameraPreRender;
                }
                else
                {
                    cameraMethods.OnCameraPreRender -= OnCameraPreRender;
                }

                useOnPreRender = value;
            }
        }

        protected abstract string Keyword { get; }
        protected abstract string ClippingSideProperty { get; }

        protected MaterialPropertyBlock materialPropertyBlock;

        private int clippingSideID;
        private CameraEventRouter cameraMethods;

        private Material[] AcquireMaterials(Renderer renderer, bool instance = true)
        {
            if (applyToSharedMaterial)
            {
                return renderer.sharedMaterials;
            }
            else
            {
                return renderer.EnsureComponent<MaterialInstance>().AcquireMaterials(this, instance);
            }
        }

        private bool isDirty;

        /// <summary>
        /// Keeping track of any field, property or transformation changes to optimize material property block setting.
        /// </summary>
        public bool IsDirty
        {
            get => isDirty;
            set => isDirty = value;
        }

        /// <summary>
        /// Public access for the renderer(s) that should be affected by the primitive.
        /// </summary>
        public List<Renderer> Renderers => renderersCache;

        /// <summary>
        /// Adds a renderer to the list of objects this clipping primitive clips.
        /// </summary>
        /// <param name="renderer">The renderer to add.</param>
        public void AddRenderer(Renderer renderer)
        {
            if (renderer != null)
            {
                if (!renderers.ContainsKey(renderer))
                {
                    var materialInstance = renderer.EnsureComponent<MaterialInstance>();
                    if (!materialInstance)
                    {
                        return;
                    }

                    renderers.Add(renderer, materialInstance);

                    var materials = materialInstance.AcquireMaterials(this);
                    ToggleClippingFeature(materials, gameObject.activeInHierarchy);
                    IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Removes a renderer to the list of objects this clipping primitive clips.
        /// </summary>
        public void RemoveRenderer(Renderer renderer)
        {
            if (renderer != null)
            {
                renderers.TryGetValue(renderer, out MaterialInstance materialInstance);
                renderers.Remove(renderer);

                if (materialInstance != null)
                {
                    // There is no need to acquire new instances if ones do not already exist since we are 
                    // in the process of removing.
                    ToggleClippingFeature(materialInstance.AcquireMaterials(this, false), false);
                    materialInstance.ReleaseMaterial(this);
                }
            }
        }

        /// <summary>
        /// Removes all renderers in the list of objects this clipping primitive clips.
        /// </summary>
        public void ClearRenderers()
        {
            if (renderers.Count > 0)
            {
                renderersCache.AddRange(renderers.Keys);
                foreach (var renderer in renderersCache)
                {
                    RemoveRenderer(renderer);
                }

                renderersCache.Clear();
            }
        }

        /// <summary>
        /// Returns a copy of the current list of renderers.
        /// </summary>
        /// <returns>The current list of renderers.</returns>
        public IEnumerable<Renderer> GetRenderersCopy()
        {
            return new List<Renderer>(renderers.Keys);
        }

        #region MonoBehaviour Implementation

        protected void OnEnable()
        {
            Initialize();
            UpdateRenderers();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update += EditorUpdate;
            }
#endif

            ToggleClippingFeature(true);

            if (useOnPreRender)
            {
                cameraMethods = CameraCache.Main.gameObject.EnsureComponent<CameraEventRouter>();
                cameraMethods.OnCameraPreRender += OnCameraPreRender;
            }
        }

        protected void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif

            UpdateRenderers();
            ToggleClippingFeature(false);

            if (cameraMethods != null)
            {
                cameraMethods.OnCameraPreRender -= OnCameraPreRender;
            }
        }

#if UNITY_EDITOR
        // We need this class to be updated once per frame even when in edit mode. Ideally this would 
        // occur after all other objects are updated in LateUpdate(), but because the ExecuteInEditMode 
        // attribute only invokes Update() we handle edit mode updating here and runtime updating 
        // in LateUpdate().
        protected void EditorUpdate()
        {
            Initialize();
            UpdateRenderers();
        }
#endif

        protected void LateUpdate()
        {
            // Deferring the LateUpdate() call to OnCameraPreRender()
            if (!useOnPreRender)
            {
                UpdateRenderers();
            }
        }

        protected void OnCameraPreRender(CameraEventRouter router)
        {
            // Only subscribed to via UseOnPreRender property setter
            UpdateRenderers();
        }

        protected void OnDestroy()
        {
            ClearRenderers();
        }

        #endregion MonoBehaviour Implementation

        #region IMaterialInstanceOwner Implementation

        /// <inheritdoc />
        public void OnMaterialChanged(MaterialInstance materialInstance)
        {
            if (materialInstance != null)
            {
                ToggleClippingFeature(materialInstance.AcquireMaterials(this), gameObject.activeInHierarchy);
            }

            UpdateRenderers();
        }

        #endregion IMaterialInstanceOwner Implementation

        protected virtual void Initialize()
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            clippingSideID = Shader.PropertyToID(ClippingSideProperty);
        }

        protected virtual void UpdateRenderers()
        {
            if (renderers.Count <= 0)
            {
                return;
            }

            CheckTransformChange();
            if (!IsDirty)
            {
                return;
            }

            BeginUpdateShaderProperties();

            renderersCache.AddRange(renderers.Keys);
            foreach (var cachedRenderer in renderersCache)
            {
                if (Application.isPlaying && cachedRenderer == null)
                {
                    RemoveRenderer(cachedRenderer);
                    continue;
                }

                cachedRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(clippingSideID, (float) clippingSide);
                UpdateShaderProperties(materialPropertyBlock);
                cachedRenderer.SetPropertyBlock(materialPropertyBlock);
            }
            
            renderersCache.Clear();
            
            EndUpdateShaderProperties();
            IsDirty = false;
        }

        protected virtual void BeginUpdateShaderProperties() { }
        protected abstract void UpdateShaderProperties(MaterialPropertyBlock materialPropertyBlock);
        protected virtual void EndUpdateShaderProperties() { }

        protected void ToggleClippingFeature(bool keywordOn)
        {
            if (renderers.Count > 0)
                foreach (var cachedRenderer in renderers.Keys)
                {
                    if (cachedRenderer != null)
                    {
                        ToggleClippingFeature(AcquireMaterials(cachedRenderer), keywordOn);
                    }
                }
        }

        protected void ToggleClippingFeature(Material[] materials, bool keywordOn)
        {
            if (materials != null)
            {
                foreach (var material in materials)
                {
                    ToggleClippingFeature(material, keywordOn);
                }
            }
        }

        protected void ToggleClippingFeature(Material material, bool keywordOn)
        {
            if (material != null)
            {
                if (keywordOn)
                {
                    material.EnableKeyword(Keyword);
                }
                else
                {
                    material.DisableKeyword(Keyword);
                }
            }
        }

        private void CheckTransformChange()
        {
            if (transform.hasChanged)
            {
                IsDirty = true;
                transform.hasChanged = false;
            }
        }
    }
}
