// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    public abstract class BaseObjectCollection : MonoBehaviour
    {
        /// <summary>
        /// Action called when collection is updated
        /// </summary>
        public Action<BaseObjectCollection> OnCollectionUpdated { get; set; }

        [HideInInspector]
        [SerializeField]
        private List<ObjectCollectionNode> nodeList = new List<ObjectCollectionNode>();

        /// <summary>
        /// List of objects with generated data on the object.
        /// </summary>
        protected List<ObjectCollectionNode> NodeList
        {
            get { return nodeList; }
        }

        /// <summary>
        /// Read only list of objects with generated data on the object.
        /// </summary>
        public IReadOnlyList<ObjectCollectionNode> NodeListReadOnly
        {
            get { return nodeList.AsReadOnly<ObjectCollectionNode>(); }
        }

        [Tooltip("Whether to include space for inactive transforms in the layout")]
        [SerializeField]
        private bool ignoreInactiveTransforms = true;

        /// <summary>
        /// Whether to include space for inactive transforms in the layout
        /// </summary>
        public bool IgnoreInactiveTransforms
        {
            get { return ignoreInactiveTransforms; }
            set { ignoreInactiveTransforms = value; }
        }

        [Tooltip("Type of sorting to use")]
        [SerializeField]
        private CollationOrder sortType = CollationOrder.None;

        /// <summary>
        /// Type of sorting to use.
        /// </summary>
        public CollationOrder SortType
        {
            get { return sortType; }
            set { sortType = value; }
        }

        /// <summary>
        /// Rebuilds / updates the collection layout.
        /// Update collection is called from the editor button on the inspector.
        /// </summary>
        public virtual void UpdateCollection()
        {
            PruneEmptyNodes();

            // Check when children change and adjust
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(child, "ObjectCollection modify transform");
#endif // UNITY_EDITOR
                if (!ContainsNode(child) && (child.gameObject.activeSelf || !IgnoreInactiveTransforms))
                {
                    NodeList.Add(new ObjectCollectionNode { Name = child.name, Transform = child });
                }
            }

            SortNodes();

            LayoutChildren();

            OnCollectionUpdated?.Invoke(this);
        }

        /// <summary>
        /// Sorts NodeList based on <see cref="SortType"/>
        /// </summary>
        protected void SortNodes()
        {
            switch (SortType)
            {
                case CollationOrder.ChildOrder:
                    NodeList.Sort((c1, c2) => (c1.Transform.GetSiblingIndex().CompareTo(c2.Transform.GetSiblingIndex())));
                    break;

                case CollationOrder.Alphabetical:
                    NodeList.Sort((c1, c2) => (string.CompareOrdinal(c1.Name, c2.Name)));
                    break;

                case CollationOrder.AlphabeticalReversed:
                    NodeList.Sort((c1, c2) => (string.CompareOrdinal(c1.Name, c2.Name)));
                    NodeList.Reverse();
                    break;

                case CollationOrder.ChildOrderReversed:
                    NodeList.Sort((c1, c2) => (c1.Transform.GetSiblingIndex().CompareTo(c2.Transform.GetSiblingIndex())));
                    NodeList.Reverse();
                    break;
            }
        }

        /// <summary>
        /// Checks for empty nodes and removes them
        /// </summary>
        protected void PruneEmptyNodes()
        {
            // Check for empty nodes and remove them
            var emptyNodes = new List<ObjectCollectionNode>();

            for (int i = 0; i < NodeList.Count; i++)
            {
                if (NodeList[i].Transform == null || (IgnoreInactiveTransforms && !NodeList[i].Transform.gameObject.activeSelf) || NodeList[i].Transform.parent == null || !(NodeList[i].Transform.parent.gameObject == gameObject))
                {
                    emptyNodes.Add(NodeList[i]);
                }
            }

            // Now delete the empty nodes
            for (int i = 0; i < emptyNodes.Count; i++)
            {
                NodeList.Remove(emptyNodes[i]);
            }

            emptyNodes.Clear();
        }

        /// <summary>
        /// Check if a node exists in the NodeList.
        /// </summary>
        /// <param name="node">The Transform belonging to the <see cref="ObjectCollectionNode"/></param>
        /// <returns>true when <paramref name="node"/> belongs to an element of the list.</returns>
        protected bool ContainsNode(Transform node)
        {
            if (node == null)
            {
                return false;
            }
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (NodeList[i].Transform == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a node exists in the NodeList.
        /// </summary>
        /// <param name="node">The Transform belonging to <see cref="ObjectCollectionNode"/></param>
        /// <param name="nodeIndex">The index of the element in <see cref="NodeList"/></param>
        /// <returns>true when <paramref name="node"/> belongs to an element of the list.</returns>
        public bool ContainsNode(Transform node, out int nodeIndex)
        {
            nodeIndex = 0;
            if (node == null)
            {
                return false;
            }
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (NodeList[i].Transform == node)
                {
                    nodeIndex = i;
                    return true;
                }
            }

            return false;
        }

        public void OnValidate()
        {
            UpdateCollection();
        }

        /// <summary>
        /// Implement for laying out all children when UpdateCollection is called.
        /// </summary>
        protected abstract void LayoutChildren();
    }
}