// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;



using Microsoft.MixedReality.Toolkit.Utilities;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data source that manages structured data similar to what can be
    /// represented in JSON or XML, in particulary collections, maps and primitives. 
    /// 
    /// It can also manage arbitrary binary data or other complex data types, in the
    /// event that a data consumer exists that can consumer the data type, such as JPEG or WAV.
    /// 
    /// This is a standalone data set that can be populated directly via
    /// SetValue() instead of depending on an external data source like a JSON or XML
    /// data stream. 
    /// 
    /// Note that SetValue() will automatically fabricate any missing structure such
    /// that it can be the sole means of populating even the most complex data set.
    /// Note that it is possible to inaadvertently create large lists, such as by only 
    /// creating the 1000th element of a collection.
    /// 
    /// 
    ///         DataChangeSetBegin();

    ///         SetValue("images[0].id", "0001");
    ///         SetValue("images[0].title", "Stone Mountain and lone tree");
    ///         SetValue("images[0].description", "Taken while rock climbing on Stone Mountain in NC.");
    ///         SetValue("images[0].imageUrl", "http://michaelinfo.com/test/images/0001.jpeg");

    ///         SetValue("images[1].id", "0002");
    ///         SetValue("images[1].title", "Brain Celosia flower");
    ///         SetValue("images[1].description", "Found this amazing flower in NC.");
    ///         SetValue("images[1].imageUrl", "http://michaelinfo.com/test/images/0002.jpeg");
    ///         DataChangeSetEnd();

    /// TODO: Refactor DataSourceJson to use this as its base implementation.
    /// 
    /// </summary>
    /// 
    public class DataSourceObjects : DataSourceBase
    {
        protected static readonly string CollectionElementkeyPathPrefixFormat = "{0}[{1:d}]";
        protected static readonly string DictionaryElementkeyPathPrefixFormat = "{0}[{1}]";


        protected static readonly string ArrayTokenPattern = @"^\s*\[\s*([a-zA-Z0-9\-_]*?)\s*\]";
        protected static readonly string KeyTokenPattern = @"^\s*([a-zA-Z0-9\-_]+?)(?:[.\[]|$)";

        protected readonly Regex _arrayTokenRegex = new Regex(ArrayTokenPattern);
        protected readonly Regex _keyTokenRegex = new Regex(KeyTokenPattern);

        protected DataNodeObject _rootNode;

        protected Dictionary<string, IDataNode> _keyPathToNodeLookup = new Dictionary<string, IDataNode>();


        public DataSourceObjects()
        {
            _rootNode = new DataNodeObject(DataNodeType.Map);
        }

        public DataSourceObjects(DataNodeType rootNodeType, object value = null)
        {
            _rootNode = new DataNodeObject(rootNodeType, value);
        }


         public override bool IsCollectionAtKeyPath(string resolvedKeyPath)
        {
            return KeyPathToNode(resolvedKeyPath)?.IsArray() ?? false;
        }

        public override int GetCollectionCount(string resolvedKeyPath)
        {
            return KeyPathToNode(resolvedKeyPath)?.GetCollectionCount() ?? 0;
        }

        public override string GetNthCollectionKeyPathAt(string resolvedKeyPath, int n)
        {
            IDataNode dataNode = KeyPathToNode(resolvedKeyPath);
            if (dataNode?.IsArray() ?? false && n < dataNode?.GetCollectionCount())
            {
                return string.Format(CollectionElementkeyPathPrefixFormat, resolvedKeyPath, 0, int.MaxValue);
            }
            else
            {
                return null;
            }
        }

        public override void SetValueInternal( string resolvedKeyPath, object value )
        {
            // navigate path, creating any missing structure
            IDataNode dataNode = KeyPathToNode(resolvedKeyPath, true);
            if ( dataNode != null )
            {
                dataNode.SetValue(value);
            }
        }

        public override IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            if (IsDataSourceAvailable())
            {
                IDataNode dataNode = KeyPathToNode(resolvedKeyPath);
                if (dataNode != null)
                {
                    if (dataNode.IsArray())
                    {
                        return GetValueAsArrayKeyPathsOptimized(dataNode, resolvedKeyPath, rangeStart, rangeCount);
                    }
                }
            }
            return null;
        }

        public override object GetValueInternal(string resolvedKeyPath)
        {
            if (IsDataSourceAvailable())
            {
                IDataNode dataNode = KeyPathToNode(resolvedKeyPath);
                if (dataNode != null)
                {
                    if (dataNode.IsArray())
                    {
                        return GetValueAsArrayKeyPaths(dataNode, resolvedKeyPath, 0, dataNode.GetCollectionCount());
                    }
                    else if (dataNode.IsMap())
                    {
                        return GetValueAsDictionaryKeyPaths(dataNode, resolvedKeyPath);
                    }
                    else
                    {
                        return dataNode.GetValue();
                    }
                }
            }

            return null;
        }


        protected IEnumerable<string> GetValueAsArrayKeyPathsOptimized(IDataNode arrayNode, string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            return new KeyPathEnumerable(resolvedKeyPath, rangeStart, rangeCount) as IEnumerable<string>;
        }


        protected IEnumerable<string> GetValueAsArrayKeyPaths(IDataNode arrayNode, string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            int rangeMax = rangeStart + rangeCount;
            int collectionCount = arrayNode.GetCollectionCount();

            if (rangeMax > collectionCount)
            {
                rangeMax = collectionCount;
            }


            List<string> keyPaths = new List<string>();
            for (int idx = rangeStart; idx < rangeMax; idx++)
            {
                keyPaths.Add(string.Format(CollectionElementkeyPathPrefixFormat, resolvedKeyPath, idx));
            }
            return keyPaths as IEnumerable<string>;

        }

        protected IEnumerable<string> GetValueAsDictionaryKeyPaths(IDataNode mapNode, string resolvedKeyPath)
        {
            List<string> keyPaths = new List<string>();
            foreach (string key in mapNode.GetMapKeys())
            {
                keyPaths.Add(string.Format(DictionaryElementkeyPathPrefixFormat, resolvedKeyPath, key));
            }
            return keyPaths as IEnumerable<string>;
        }




        protected IDataNode KeyPathToNode(string resolvedKeyPath, bool createIfMissing = false)
        {
            // walk down a simple path like:
            //      [1].name
            //      address.street
            //      addresses[1].street
            //      contacts[2].email_addresses[2]
            //      matrix[1][2]

            if (_keyPathToNodeLookup.ContainsKey(resolvedKeyPath))
            {
                return _keyPathToNodeLookup[resolvedKeyPath];
            }
            else
            {
                IDataNode currentNode = _rootNode;

                string keyPath = resolvedKeyPath;

                while (keyPath != null && keyPath != "")
                {
                    int amountToSkip = 0;
                    MatchCollection arrayMatches = _arrayTokenRegex.Matches(keyPath);
                    if (arrayMatches.Count > 0)
                    {
                        string arrayIndexText = arrayMatches[0].Groups[1].Value;
                        int arrayIndex = int.Parse(arrayIndexText);

                        if ( createIfMissing )
                        {
                            if (currentNode.IsUnassigned())
                            {
                                currentNode.SetNodeType(DataNodeType.Array);
                            }

                            while (arrayIndex >= currentNode.GetCollectionCount())
                            {
                                currentNode.AddToArray(new DataNodeObject());
                            }
                        }

                        if (currentNode.IsArray())
                        {
                            currentNode = currentNode.GetNodeByIndex(arrayIndex);
                        }
                        else
                        {
                            currentNode = null;
                        }
                        amountToSkip = arrayMatches[0].Value.Length;

                    }
                    else
                    {
                        MatchCollection keyMatches = _keyTokenRegex.Matches(keyPath);
                        if (keyMatches.Count > 0)
                        {
                            string key = keyMatches[0].Groups[1].Value;

                            if (createIfMissing )
                            {
                                if ( currentNode.IsUnassigned())
                                {
                                    currentNode.SetNodeType(DataNodeType.Map);
                                }

                                if (currentNode.GetNodeByKey(key) == null)
                                {
                                    currentNode.AddToMap(key, new DataNodeObject());
                                }
                            }

                            if (currentNode.IsMap())
                            {
                                currentNode = currentNode.GetNodeByKey(key);
                            }
                            else
                            {
                                currentNode = null;
                            }

                            amountToSkip = key.Length;
                        }
                    }

                    if (keyPath.Length > amountToSkip && keyPath[amountToSkip] == '.')
                    {
                        amountToSkip++;
                    }
                    if (amountToSkip > 0)
                    {
                        keyPath = keyPath.Substring(amountToSkip);
                    }
                    else
                    {
                        currentNode = null;
                        break;      // was not a valid path piece
                    }
                }

                if (currentNode != null)
                {
                    _keyPathToNodeLookup[resolvedKeyPath] = currentNode;
                }
                return currentNode;
            }
        }

        protected override bool IsDataSourceAvailable()
        {
            return _rootNode != null;
        }
    } // End of class DataSourceObjects

} // End of namespace Microsoft.MixedReality.Toolkit.Data


