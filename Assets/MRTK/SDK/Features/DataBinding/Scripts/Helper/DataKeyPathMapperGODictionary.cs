// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A simple data key path mapper that can be assigned to a data source in the Unity inspector.
    /// 
    /// TODO: consider refactoring using SerializableDictionary class once core utilities are separable.
    /// 
    /// </summary>
    /// 
    public class DataKeyPathMapperGODictionary : MonoBehaviour, IDataKeyPathMapper
    {
        [Serializable]
        public class ViewToDataKeypathMap
        {
            [Tooltip("Local view key path associated with a data consumer, typically used in a prefab.")]
            [SerializeField] public string viewKeyPath;

            [Tooltip("Key path in data source to be mapped to. This will be a relative path if referencing data within an array")]
            [SerializeField] public string dataKeyPath;
        }

        [Tooltip("A collection of mappings between a view (consumer) key path and a data source key path.")]
        [SerializeField]
        private ViewToDataKeypathMap[] viewKeypathToDataKeypathMapper;

        public IDataKeyPathMapper DataKeyPathMapper { get { return _dataKeyPathMapperDictionary; } }

        protected DataKeyPathMapperDictionary _dataKeyPathMapperDictionary = new DataKeyPathMapperDictionary();


        void Awake()
        {
            foreach(ViewToDataKeypathMap v2dKeyPath in viewKeypathToDataKeypathMapper )
            {
                _dataKeyPathMapperDictionary.AddKeyMapping(v2dKeyPath.viewKeyPath, v2dKeyPath.dataKeyPath);
            }
        }

        public string GetDataKeyPathFromViewKeyPath(string viewKeyPath)
        {
            return _dataKeyPathMapperDictionary?.GetDataKeyPathFromViewKeyPath(viewKeyPath);
        }

        public string GetViewKeyPathFromDataKeyPath(string dataKeyPath)
        {
            return _dataKeyPathMapperDictionary?.GetViewKeyPathFromDataKeyPath(dataKeyPath);
        }
    }
}