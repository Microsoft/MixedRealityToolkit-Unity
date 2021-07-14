// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data node that implements the IDataNode interface.
    /// 
    /// It's used by DataSourceObjects to implement a structured data set.
    /// </summary>

    public class DataNodeObject : IDataNode
    {

        protected object _value;

        protected DataNodeType _type;

        public DataNodeObject()
        {
            _type = DataNodeType.Unassigned;
        }


        public DataNodeObject(DataNodeType type, object value = null)
        {
            SetNodeType(type, value);
            _type = type;
        }


        public void SetNodeType(DataNodeType newNodeType, object value = null)
        {
            _type = newNodeType;

            switch (newNodeType)
            {
                case DataNodeType.Array: _value = new List<IDataNode>() as object; break;
                case DataNodeType.Map: _value = new Dictionary<string, IDataNode>() as object; break;
                default: _value = value; break;
            }
        }


        public bool IsArray()
        {
            return _type == DataNodeType.Array;
        }


        public bool IsMap()
        {
            return _type == DataNodeType.Map;
        }

        public bool IsUnassigned()
        {
            return _type == DataNodeType.Unassigned;
        }


        public virtual IDataNode GetNodeByIndex(int n)
        {
            return GetArray()?[n] ?? null;
        }


        public virtual IDataNode GetNodeByKey(string key)
        {
            if (IsMap() && GetMap().ContainsKey(key))
            {
                return GetMap()[key];
            }
            else
            {
                return null;
            }
        }


        public virtual int GetCollectionCount()
        {
            if (IsMap())
            {
                return GetMap().Count;
            }
            else if (IsArray())
            {
                return GetArray().Count;
            }
            else
            {
                return 0;
            }
        }


        public IEnumerable<string> GetMapKeys()
        {
            return GetMap()?.Keys ?? null;
        }


        public object GetValue()
        {
            return _value;
        }


        public void SetValue(object newValue)
        {
            _value = newValue;
        }


        public DataNodeType GetNodeType()
        {
            return _type;
        }


        public void AddToArray(IDataNode dataNode)
        {
            if (IsArray())
            {
                GetArray().Add(dataNode);
            }
        }


        public void AddToMap(string key, IDataNode dataNode)
        {
            if (IsMap())
            {
                GetMap()[key] = dataNode;
            }
        }


        protected Dictionary<string, IDataNode> GetMap()
        {
            return _value as Dictionary<string, IDataNode>;
        }

        protected List<IDataNode> GetArray()
        {
            return _value as List<IDataNode>;
        }
    }
}
