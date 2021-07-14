// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Interface for one node in a hierarchical organization of data comprised of
    /// lists, maps (dictionaries), and objects of arbitrary type and complexity
    /// such as native data primitives (int, boolean, float), or even an object as 
    /// complex as a jpeg byte stream.
    /// 
    /// This is used by a data source to organize information that matches the
    /// defacto standard key path format that's modeled on JSON/javascript.
    /// 
    /// </summary>
    public enum DataNodeType
    {
        Unassigned,
        Null,
        Array,
        Map,
        Other       // primitives, strings, or complex objects not navigable.
        
    }


    public interface IDataNode
    {

        /// <summary>
        /// Is this node an array of other nodes?
        /// </summary>
        /// 
        /// <returns>true if an array, false otherwise.</returns>
        bool IsArray();

        /// <summary>
        /// Is this node a map (dictionary) to other nodes?
        /// </summary>
        /// 
        /// <returns>true if a map, false otherwise.</returns>
        bool IsMap();

        /// <summary>
        /// Is this node created but has not yet been assigned
        /// a data type?
        /// </summary>
        /// <remarks>
        /// This unassigned state is used during the creation of nodes process
        /// where you declare the existence of an object before
        /// knowing what type it should be.
        /// </remarks>
        /// 
        /// <returns>true if unassigned, false otherwise.</returns>
        bool IsUnassigned();

        /// <summary>
        /// Return the nth child node of an array node.
        /// </summary>
        /// 
        /// <param name="n">Which child node to return.</param>
        /// 
        /// <returns>The nth child node if this is an array node, or null otherwise.</returns>
        IDataNode GetNodeByIndex(int n);

        /// <summary>
        /// Return the child node of a map node by its key.
        /// </summary>
        /// 
        /// <param name="key">The key specifying which child node to return.</param>
        /// 
        /// <returns>The child node for specified key, if this is a map node and the key exists, or null otherwise.</returns>
        IDataNode GetNodeByKey(string key);

        /// <summary>
        /// Get the value associated with this node.
        /// </summary>
        /// 
        /// <remarks>
        /// If this node is an array node, the value is an IEnumerable<string> object that
        /// contains all the keypaths of its children. Note that this is not the usual way of
        /// retrieving items in an array since paging is not possible. See
        /// IEnumerable<string> GetCollectionKeyPathRange() for a better method of 
        /// accessing members of a collection.
        /// 
        /// If this node is a map node, the value is an IEnumerable<string> object
        /// that contians the full keypath for every key in the map.
        /// </remarks>
        /// 
        /// <returns>The value associated with this node of arbitrary type and complexity</returns>
        object GetValue();

        /// <summary>
        /// Set the value associated with this node.
        /// </summary>
        /// 
        /// <remarks>
        /// The value can be of arbitrary type and complexity.
        /// 
        /// Note that it is not possible to set the value of a map or
        /// array node, in which case this will do nothing.</remarks>
        /// 
        /// <param name="value">The value to be associated with this node.</param>
        void SetValue(object value);

        /// <summary>
        /// Get the type for this node.
        /// </summary>
        /// 
        /// <remarks>
        /// An alternative way to determine the type is by using the IsArray(), IsMap() and IsUnassigned()
        /// methods.
        /// </remarks>
        /// 
        /// <returns>The type for this node.</returns>
        DataNodeType GetNodeType();

        /// <summary>
        /// Set the type, and the associated value for this node.
        /// </summary>
        /// 
        /// <remarks>
        /// The primary use of this is to convert an unassigned node to 
        /// a node of a specific type.
        /// 
        /// If not unassigned, any previous value is lost. If the new node type
        /// is an array or a map, then these will be initialized to
        /// be empty collections even if they were already of those
        /// types.
        /// 
        /// If the new type is an array or a map, the value argument is ignored
        /// even if not null.
        /// </remarks>
        /// 
        /// <param name="newNodeType">The new type for this node</param>
        /// <param name="value">The value to be associated with this node.</param>
        /// 
        void SetNodeType(DataNodeType newNodeType, object value = null);

        /// <summary>
        /// If this node is an array or a map, return the number of entries.
        /// </summary>
        /// 
        /// <returns>Return size of the collection, or 0 of not a collection.</returns>
        int GetCollectionCount();

        /// <summary>
        /// If this node is a map, return an enumeration of keys.
        /// </summary>
        /// <returns>A string enumeration of the keys.</returns>
        IEnumerable<string> GetMapKeys();

        /// <summary>
        /// If this is an array node, add a new node to the end, otherwise no-op.
        /// </summary>
        /// 
        /// <param name="dataNode">The node to add</param>
        void AddToArray(IDataNode dataNode);

        /// <summary>
        /// If this is a map node, add a new key and the associated node, otherwise no-op.
        /// </summary>
        /// <param name="key">The key to add to this map</param>
        /// <param name="dataNode">The data node to associate with the specified key.</param>
        void AddToMap(string key, IDataNode dataNode);

    }
}
