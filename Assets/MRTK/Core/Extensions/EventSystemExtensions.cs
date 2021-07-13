// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Physics;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit
{
    /// <summary>
    /// Extension methods for Unity's EventSystem 
    /// </summary>
    public static class EventSystemExtensions
    {
        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();
        private static readonly RaycastResultComparer RaycastResultComparer = new RaycastResultComparer();

        private static readonly ProfilerMarker RaycastPerfMarker = new ProfilerMarker("[MRTK] EventSystemExtensions.Raycast");

        /// <summary>
        /// Executes a raycast all and returns the closest element.
        /// Fixes the current issue with Unity's raycast sorting which does not consider separate canvases.
        /// </summary>
        /// <remarks>
        /// Takes an optional RaycastResultComparer, which will be used to select the highest priority raycast result.
        /// </remarks>
        /// <returns>RaycastResult if hit, or an empty RaycastResult if nothing was hit</returns>
        public static RaycastResult Raycast(this EventSystem eventSystem, PointerEventData pointerEventData, LayerMask[] layerMasks, RaycastResultComparer raycastResultComparer = null)
        {
            using (RaycastPerfMarker.Auto())
            {
                eventSystem.RaycastAll(pointerEventData, RaycastResults);
                return PrioritizeRaycastResult(layerMasks, raycastResultComparer);
            }
        }

        private static readonly ProfilerMarker PrioritizeRaycastResultPerfMarker = new ProfilerMarker("[MRTK] EventSystemExtensions.PrioritizeRaycastResult");

        /// <summary>
        /// Sorts the available Raycasts in to a priority order for query.
        /// </summary>
        /// <param name="priority">The layer mask priority.</param>
        /// <returns><see cref="RaycastResult"/></returns>
        private static RaycastResult PrioritizeRaycastResult(LayerMask[] priority, RaycastResultComparer raycastResultComparer)
        {
            using (PrioritizeRaycastResultPerfMarker.Auto())
            {
                // If not specified, default to the in-box RaycastResultComparer.
                if (raycastResultComparer == null)
                {
                    raycastResultComparer = RaycastResultComparer;
                }

                ComparableRaycastResult maxResult = default(ComparableRaycastResult);

                for (var i = 0; i < RaycastResults.Count; i++)
                {
                    if (RaycastResults[i].gameObject == null) { continue; }

                    var layerMaskIndex = RaycastResults[i].gameObject.layer.FindLayerListIndex(priority);
                    if (layerMaskIndex == -1) { continue; }

                    var result = new ComparableRaycastResult(RaycastResults[i], layerMaskIndex);

                    if (maxResult.RaycastResult.module == null || raycastResultComparer.Compare(maxResult, result) < 0)
                    {
                        maxResult = result;
                    }
                }

                return maxResult.RaycastResult;
            }
        }

        /// <summary>
        /// Bubbles up an event to the parents of the root game object if the event data is not already used.
        /// </summary>
        /// <typeparam name="T">The EventFunction type.</typeparam>
        /// <param name="root">Events start executing on the parent of this game object.</param>
        /// <param name="eventData">Data associated with the Executing event.</param>
        /// <param name="callbackFunction">Function to execute on the gameObject components.</param>
        /// <returns>GameObject that handled the event</returns>
        public static GameObject ExecuteHierarchyUpward<T>(GameObject root, BaseEventData eventData, ExecuteEvents.EventFunction<T> callbackFunction) where T : IEventSystemHandler
        {
            if (!eventData.used && root != null)
            {
                var parent = root.transform.parent;

                if (parent != null)
                {
                    return ExecuteEvents.ExecuteHierarchy(parent.gameObject, eventData, callbackFunction);
                }
            }

            return null;
        }
    }
}
