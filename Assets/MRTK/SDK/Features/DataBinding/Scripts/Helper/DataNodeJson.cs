// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using SimpleJSON;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A SimpleJSON JSONNode that implements the IDataNode interface so that it can
    /// be used with the DataSourceObjects class instead of being custom to JSON.
    /// 
    /// </summary>
    /// 

    /****

    public class DataNodeJson : JSONNode, IDataNode
    {
        public DataNodeJson()
        {
        }


        public bool IsDataArray()
        {
            return IsArray;
        }

        public bool IsDataMap()
        {
            return IsObject;
        }

        public object GetValue()
        {
            return Value;
        }

        public void SetValue( object value )
        {
            Value = value.ToString();
        }

        public IDataNode GetNodeByIndex( int n )
        {
            return this[n] as IDataNode;
        }

        public IDataNode GetNodeByKey( string key)
        {
            return this[key] as IDataNode;
        }

        public DataNodeType GetNodeType()
        {
            switch (Tag)
            {
                default:
                case JSONNodeType.None: return DataNodeType.Unassigned;
                case JSONNodeType.Array: return DataNodeType.Array;
                case JSONNodeType.Object: return DataNodeType.Map;
                case JSONNodeType.String: return DataNodeType.String;
                case JSONNodeType.Number: return DataNodeType.Number;
                case JSONNodeType.NullValue: return DataNodeType.Null;
                case JSONNodeType.Boolean: return DataNodeType.Boolean;
            }
        }

        public IDataNode CreateEmptyArray()
        {
            return 
        }


        public IDataNode AddToArray(object value = null)
        {
            Add(object);
        }


        public IDataNode AddToMap(string key, DataNodeType nodeType, object value = null)
        {

        }
    }

    ***/
 
}
