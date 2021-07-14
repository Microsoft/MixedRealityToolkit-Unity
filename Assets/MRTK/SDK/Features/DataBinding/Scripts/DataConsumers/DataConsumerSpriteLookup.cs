// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Given a value from a data source, use that value to look up the correct Sprite
    /// specified in the Unity inspector list. That Sprite is then associated
    /// with any SpriteRenderer being managed by this object.
    /// </summary>
    /// 
    /// <remarks>
    /// 
    /// TODO: Allow for a default sprite if no look up can be found.
    /// 
    /// </remarks>
    /// 
    [Serializable]
    public class DataConsumerSpriteLookup : DataConsumerGOBase
    {
        [Serializable]
        public struct ValueToSpriteInfo
        {
            [Tooltip("Value from the data source to be mapped to a sprite.")]
            [SerializeField] public string Value;

            [Tooltip("Sprite to map to for this value.")]
            [SerializeField] public Sprite Sprite;
        }

        [Tooltip("Manage sprites in child game objects as well as this one.")]
        [SerializeField] private bool manageChildren = true;

        [Tooltip("Key path within the data source for the value used for sprite lookup.")]
        [SerializeField] private string keyPath;

        [Tooltip("List of value-to-sprite mappings.")]
        [SerializeField] private ValueToSpriteInfo[] valueToSpriteLookup;


        protected SpriteRenderer _spriteRenderer;


        protected override Type[] GetComponentTypes()
        {

            Type[] types = { typeof(SpriteRenderer) };
            return types;
        }


        protected override bool ManageChildren()
        {
            return manageChildren;
        }


        protected override void AddVariableKeyPathsForComponent(Type componentType, Component component)
        {
            _spriteRenderer = component as SpriteRenderer;
            AddKeyPathListener(keyPath);
        }


        protected override void ProcessDataChanged(IDataSource dataSource, string resolvedKeyPath, string localKeyPath, object newValue, DataChangeType dataChangeType)
        {
            if (localKeyPath == keyPath)
            {
                string value = newValue.ToString();

                foreach ( ValueToSpriteInfo v2si in valueToSpriteLookup)
                {
                    if ( value == v2si.Value )
                    {
                        _spriteRenderer.sprite = v2si.Sprite;
                        break;
                    }
                }
            }
        }

    }
}
