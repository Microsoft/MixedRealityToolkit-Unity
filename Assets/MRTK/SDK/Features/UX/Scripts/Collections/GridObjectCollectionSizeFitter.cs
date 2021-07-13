// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    [ExecuteInEditMode]
    public class GridObjectCollectionSizeFitter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The GridObjectColletion to fit")]
        private GridObjectCollection gridObjectCollection = null;

        [SerializeField]
        [Range(0.001f, .5f)]
        [Tooltip("Scale factor for the border. Stored in local Z scale.")]
        private float borderScale = 0.02f;

        [SerializeField]
        [Range(0, .1f)]
        [Tooltip("Total margin added to left and right")]
        private float horizontalMargin = 0.0f;

        [SerializeField]
        [Range(0, .1f)]
        [Tooltip("Total margin added to top and bottom")]
        private float verticalMargin = 0.0f;

        [SerializeField]
        [Range(0.001f, .1f)]
        [Tooltip("Distance behind GridObjectCollection")]
        private float distance = 0.001f;

        private void Start()
        {
            if (gridObjectCollection)
            {
                gridObjectCollection.OnCollectionUpdated = (BaseObjectCollection box) => UpdateLayout();
                UpdateLayout();
            }
        }

        private void OnValidate()
        {
            if (gridObjectCollection)
            {
                gridObjectCollection.OnCollectionUpdated = (BaseObjectCollection box) => UpdateLayout();
                UpdateLayout();
            }
        }

        private void UpdateLayout()
        {
            if (gridObjectCollection)
            {
                transform.localPosition = new Vector3(gridObjectCollection.Center.x, gridObjectCollection.Center.y, gridObjectCollection.Distance + distance);
                transform.localScale = new Vector3(gridObjectCollection.Width + horizontalMargin, gridObjectCollection.Height + verticalMargin, borderScale);
            }
        }
    }
}
