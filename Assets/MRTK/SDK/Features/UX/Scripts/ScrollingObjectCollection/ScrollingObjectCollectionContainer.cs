using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    ///     Container for all renderers and colliders used in the ScrollingObjectCollection.
    ///     It's main purpose is to find and cache those Components.
    ///     Thanks to that we don't have to run GetComponentsInChildren every frame. 
    /// </summary>
    public class ScrollingObjectCollectionContainer : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] children;

        /// <summary>
        ///     Dictionary containing ScrollingObjectCollection's items child renderers.
        /// </summary>
        public Dictionary<GameObject, Renderer[]> ItemRenderersMap { get; private set; } 
            = new Dictionary<GameObject, Renderer[]>();

        /// <summary>
        ///     Dictionary containing ScrollingObjectCollection's items child colliders.
        /// </summary>
        public Dictionary<GameObject, Collider[]> ItemCollidersMap { get; private set; } 
            = new Dictionary<GameObject, Collider[]>();

        /// <summary>
        ///     Position of the Container.
        /// </summary>
        public Vector3 LocalPosition
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        private GameObject[] Children
        {
            get
            {
                var childCount = transform.childCount;
                if (children == null || children.Length != childCount)
                {
                    children = new GameObject[childCount];
                    for (var i = 0; i < children.Length; ++i)
                        children[i] = transform.GetChild(i).gameObject;
                }

                return children;
            }
        }

        public void UpdateChildren()
        {
            ItemRenderersMap = new Dictionary<GameObject, Renderer[]>();
            ItemCollidersMap = new Dictionary<GameObject, Collider[]>();
            OnTransformChildrenChanged();
        }
        
        private void OnTransformChildrenChanged()
        {
            foreach (var child in Children)
            {
                AddItem(child, ItemRenderersMap);
                AddItem(child, ItemCollidersMap);
            }
        }
        
        private static void AddItem<T>(GameObject child, IDictionary<GameObject, T[]> collection) where T : Component
        {
            if (!child)
            {
                if (collection.ContainsKey(child))
                    collection.Remove(child);
                return;
            }

            var componentsInChildren = child.GetComponentsInChildren<T>(true);
            if (collection.ContainsKey(child))
            {
                collection[child] = componentsInChildren;
                return;
            }

            collection.Add(child, componentsInChildren);
        }
    }
}