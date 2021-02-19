// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.Toolkit.Rendering
{
    /// <summary>
    /// The MaterialInstance behavior aides in tracking instance material lifetime and automatically destroys instanced materials for the user. 
    /// This utility component can be used as a replacement to <see href="https://docs.unity3d.com/ScriptReference/Renderer-material.html">Renderer.material</see> or 
    /// <see href="https://docs.unity3d.com/ScriptReference/Renderer-materials.html">Renderer.materials</see>. When invoking Unity's Renderer.material(s), Unity 
    /// automatically instantiates new materials. It is the caller's responsibility to destroy the materials when a material is no longer needed or the game object is 
    /// destroyed. The MaterialInstance behavior helps avoid material leaks and keeps material allocation paths consistent during edit and run time.
    /// </summary>
    [HelpURL("https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/rendering/material-instance")]
    [ExecuteAlways, RequireComponent(typeof(Renderer))]
    [AddComponentMenu("Scripts/MRTK/Core/MaterialInstance")]
    public class MaterialInstance : MonoBehaviour
    {
        /// <summary>
        /// Returns the first instantiated Material assigned to the renderer, similar to <see href="https://docs.unity3d.com/ScriptReference/Renderer-material.html">Renderer.material</see>. 
        /// If any owner is specified the instanced material(s) will not be released until all owners are released. When a material
        /// is no longer needed ReleaseMaterial should be called with the matching owner.
        /// </summary>
        /// <param name="owner">An optional owner to track instance ownership.</param>
        /// <returns>The first instantiated Material.</returns>
        public Material AcquireMaterial(Object owner = null, bool instance = true)
        {
            if (owner != null)
            {
                materialOwners.Add(owner);
            }

            if (instance)
            {
                AcquireInstances();
            }

            if (instanceMaterials?.Length > 0)
            {
                return instanceMaterials[0];
            }

            return null;
        }

        /// <summary>
        /// Returns all the instantiated materials of this object, similar to <see href="https://docs.unity3d.com/ScriptReference/Renderer-materials.html">Renderer.materials</see>. 
        /// If any owner is specified the instanced material(s) will not be released until all owners are released. When a material
        /// is no longer needed ReleaseMaterial should be called with the matching owner.
        /// </summary>
        /// <param name="owner">An optional owner to track instance ownership.</param>
        /// <param name="instance">Should this acquisition attempt to instance materials?</param>
        /// <returns>All the instantiated materials.</returns>
        public Material[] AcquireMaterials(Object owner = null, bool instance = true)
        {
            if (owner != null)
            {
                materialOwners.Add(owner);
            }

            if (instance)
            {
                AcquireInstances();
            }

            return instanceMaterials;
        }

        /// <summary>
        /// Relinquishes ownership of a material instance. This should be called when a material is no longer needed
        /// after acquire ownership with AcquireMaterial(s).
        /// </summary>
        /// <param name="owner">The same owner which originally acquire ownership via AcquireMaterial(s).</param>
        /// <param name="instance">Should this acquisition attempt to instance materials?</param>
        /// <param name="autoDestroy">When ownership count hits zero should the MaterialInstance component be destroyed?</param>
        public void ReleaseMaterial(Object owner, bool autoDestroy = true)
        {
            materialOwners.Remove(owner);

            if (autoDestroy && materialOwners.Count == 0)
            {
                DestroySafe(this);

                // OnDestroy not called on inactive objects
                if (!gameObject.activeInHierarchy)
                {
                    RestoreRenderer();
                }
            }
        }

        /// <summary>
        /// Returns the first instantiated Material assigned to the renderer, similar to <see href="https://docs.unity3d.com/ScriptReference/Renderer-material.html">Renderer.material</see>.
        /// </summary>
        public Material Material => AcquireMaterial();

        /// <summary>
        /// Returns all the instantiated materials of this object, similar to <see href="https://docs.unity3d.com/ScriptReference/Renderer-materials.html">Renderer.materials</see>.
        /// </summary>
        public Material[] Materials => AcquireMaterials();

        private Renderer CachedRenderer
        {
            get
            {
                if (cachedRenderer == null)
                {
                    cachedRenderer = GetComponent<Renderer>();
                    cachedSharedMaterials = cachedRenderer.sharedMaterials;

                    return cachedRenderer;
                }

                return cachedRenderer;
            }
        }

        private Material[] CachedSharedMaterials
        {
            get => cachedSharedMaterials;
            set
            {
                cachedSharedMaterials = value;
                cachedRenderer.sharedMaterials = value;
            }
        }


        private Renderer cachedRenderer = null;

        [SerializeField, HideInInspector]
        private Material[] defaultMaterials = null;
        private Material[] instanceMaterials = null;
        private Material[] cachedSharedMaterials = null;
        private bool initialized = false;
        private bool materialsInstanced = false;
        private readonly HashSet<Object> materialOwners = new HashSet<Object>();

        private const string instancePostfix = " (Instance)";

        #region MonoBehaviour Implementation

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            // If the materials get changed via outside of MaterialInstance.
            var sharedMaterials = CachedSharedMaterials;

            if (!MaterialsMatch(sharedMaterials, instanceMaterials))
            {
                // Re-create the material instances.
                var newDefaultMaterials = new Material[sharedMaterials.Length];
                var min = Math.Min(newDefaultMaterials.Length, defaultMaterials.Length);

                // Copy the old defaults.
                for (var i = 0; i < min; ++i)
                {
                    newDefaultMaterials[i] = defaultMaterials[i];
                }

                // Patch in the new defaults.
                for (var i = 0; i < newDefaultMaterials.Length; ++i)
                {
                    var material = sharedMaterials[i];

                    if (!IsInstanceMaterial(material))
                    {
                        newDefaultMaterials[i] = material;
                    }
                }

                defaultMaterials = newDefaultMaterials;
                CreateInstances();

                // Notify owners of the change.
                foreach (var owner in materialOwners)
                {
                    (owner as IMaterialInstanceOwner)?.OnMaterialChanged(this);
                }
            }
        }

        private void OnDestroy()
        {
            RestoreRenderer();
        }

        private void RestoreRenderer()
        {
            if (CachedRenderer != null && defaultMaterials != null)
            {
                CachedSharedMaterials = defaultMaterials;
            }

            DestroyMaterials(instanceMaterials);
            instanceMaterials = null;
        }

        #endregion MonoBehaviour Implementation


        private void Initialize()
        {
            if (!initialized && CachedRenderer != null)
            {
                // Cache the default materials if ones do not already exist.
                if (!HasValidMaterial(defaultMaterials))
                {
                    defaultMaterials = CachedSharedMaterials;
                }
                else if (!materialsInstanced) // Restore the clone to its initial state.
                {
                    CachedSharedMaterials = defaultMaterials;
                }

                initialized = true;
            }
        }

        private void AcquireInstances()
        {
            if (CachedRenderer != null)
            {
                if (!MaterialsMatch(CachedSharedMaterials, instanceMaterials))
                {
                    CreateInstances();
                }
            }
        }

        private void CreateInstances()
        {
            // Initialize must get called to set the defaultMaterials in case CreateInstances get's invoked before Awake.
            Initialize();

            DestroyMaterials(instanceMaterials);
            instanceMaterials = InstanceMaterials(defaultMaterials);

            if (CachedRenderer != null && instanceMaterials != null)
            {
                CachedSharedMaterials = instanceMaterials;
            }

            materialsInstanced = true;
        }

        private static bool MaterialsMatch(Material[] a, Material[] b)
        {
            if (a?.Length != b?.Length)
            {
                return false;
            }

            for (int i = 0; i < a?.Length; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Material[] InstanceMaterials(Material[] source)
        {
            if (source == null)
            {
                return null;
            }

            var output = new Material[source.Length];

            for (var i = 0; i < source.Length; ++i)
            {
                if (source[i] != null)
                {
                    if (IsInstanceMaterial(source[i]))
                    {
                        Debug.LogWarning($"A material ({source[i].name}) which is already instanced was instanced multiple times.");
                    }

                    output[i] = new Material(source[i]);
                    output[i].name += instancePostfix;
                }
            }

            return output;
        }

        private static void DestroyMaterials(Material[] materials)
        {
            if (materials != null)
            {
                for (var i = 0; i < materials.Length; ++i)
                {
                    DestroySafe(materials[i]);
                }
            }
        }

        private static bool IsInstanceMaterial(Material material)
        {
            return ((material != null) && material.name.Contains(instancePostfix));
        }

        private static bool HasValidMaterial(Material[] materials)
        {
            if (materials != null)
            {
                foreach (var material in materials)
                {
                    if (material != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void DestroySafe(Object toDestroy)
        {
            if (toDestroy != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(toDestroy);
                }
                else
                {
#if UNITY_EDITOR
                    // Let Unity handle unload of unused assets if lifecycle is transitioning from editor to play mode
                    // Defering the call during this transition would destroy reference only after play mode Awake, leading to possible broken material references on TMPro objects
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (toDestroy != null)
                            {
                                DestroyImmediate(toDestroy);
                            }
                        };
                    }
#endif
                }
            }
        }
    }
}