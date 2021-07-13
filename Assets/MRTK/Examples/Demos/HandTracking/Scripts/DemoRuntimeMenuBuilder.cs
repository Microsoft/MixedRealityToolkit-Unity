// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Demo script that dynamically adds and removes buttons from a GridObjectCollection.
    /// </summary>
    [RequireComponent(typeof(GridObjectCollection))]
    public class DemoRuntimeMenuBuilder : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Button prefab that will be used to fill the dynamic GridObjectCollection.")]
        private ButtonConfigHelper buttonPrefab = null;

        private GridObjectCollection gridObjectCollection;

        private void Start()
        {
            gridObjectCollection = this.GetComponent<GridObjectCollection>();
        }

        void Update()
        {
            int numberButtons = (int)Mathf.PingPong(Time.realtimeSinceStartup, 6) + 1;

            if (numberButtons != gridObjectCollection.transform.childCount)
            {
                RebuildButtonCollection(numberButtons);
            }
        }

        private void RebuildButtonCollection(int numberButtons)
        {
            while (transform.childCount > numberButtons)
            {
                GameObject.DestroyImmediate(transform.GetChild(transform.childCount-1).gameObject);
            }

            while (transform.childCount < numberButtons)
            {
                var button = GameObject.Instantiate(buttonPrefab, transform);
                button.MainLabelText = string.Format("Button {0}", transform.childCount);
                button.SetQuadIcon(button.IconSet.QuadIcons[transform.childCount]);
            }
            gridObjectCollection.UpdateCollection();
        }
    }
}