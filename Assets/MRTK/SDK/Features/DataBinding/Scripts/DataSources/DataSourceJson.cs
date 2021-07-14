// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SimpleJSON;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A data source that can parse a JSON text stream and notify any data consumers
    /// listening for changes.
    /// 
    /// This data source has on Unity specific dependencies.
    /// 
    /// TODO: refactor this to use DataSourceObjects.  Should be a fairly straight refactor since
    ///       DataSourceObjects was derived from this source code. See DataNodeJson
    /// 
    /// TODO: Add support for SetValue() and ability to report changes to a back-end
    ///       services for persisting those changes. This would then allow for 
    ///       data entry slates.
    ///      
    /// </summary>
    /// 

    public class DataSourceJson : DataSourceBase
    {
        private JSONNode _jsonRootNode = null;
        static readonly string CollectionElementkeyPathPrefixFormat = "{0}[{1:d}]";
        static readonly string DictionaryElementkeyPathPrefixFormat = "{0}[{1}]";

        static readonly string _arrayTokenPattern = @"^\s*\[\s*([a-zA-Z0-9\-_]*?)\s*\]";
        static readonly string _keyTokenPattern = @"^\s*([a-zA-Z0-9\-_]+?)(?:[.\[]|$)";

        protected readonly Regex _arrayTokenRegex = new Regex(_arrayTokenPattern);
        protected readonly Regex _keyTokenRegex = new Regex(_keyTokenPattern);

        protected Dictionary<string, JSONNode> _keyPathToJsonNodeLookup = new Dictionary<string, JSONNode>();
 

        public override bool IsCollectionAtKeyPath(string resolvedKeyPath)
        {
            return KeyPathToNode(resolvedKeyPath)?.IsArray ?? false;
        }

        public override int GetCollectionCount(string resolvedKeyPath)
        {
            return KeyPathToNode(resolvedKeyPath)?.Count ?? 0;
        }

        public override string GetNthCollectionKeyPathAt(string resolvedKeyPath, int n )
        {
            JSONNode jsonNode = KeyPathToNode(resolvedKeyPath);
            if (jsonNode?.IsArray ?? false && n < jsonNode?.Value.Length)
            {
                return String.Format(DataSourceJson.CollectionElementkeyPathPrefixFormat, resolvedKeyPath, 0, int.MaxValue);  
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            if (IsDataSourceAvailable())
            {
                JSONNode jsonNode = KeyPathToNode(resolvedKeyPath);
                if (jsonNode != null)
                {
                    if (jsonNode.IsArray)
                    {
                          return GetValueAsArrayKeyPaths(jsonNode, resolvedKeyPath, rangeStart, rangeCount);
                    }
                }
            }
            return null;
        }

        public override object GetValueInternal(string resolvedKeyPath)
        {
            if ( IsDataSourceAvailable() ) {
                JSONNode jsonNode = KeyPathToNode(resolvedKeyPath);
                if (jsonNode != null)
                {
                    if (jsonNode.IsArray)
                    {
                        return GetValueAsArrayKeyPaths(jsonNode, resolvedKeyPath, 0, jsonNode.Count);
                    }
                    else if (jsonNode.IsObject)
                    {
                        return GetValueAsDictionaryKeyPaths(jsonNode, resolvedKeyPath);
                    }
                    else
                    {
                        return jsonNode.Value;
                    }
                }
            }

            return null;
        }

        protected IEnumerable<string> GetValueAsArrayKeyPaths(JSONNode arrayNode, string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            // TODO: To optimize for large collections, this method should instantiate a class that can provide an IEnumerable<string> 
            //       but then only generates key paths as they are requested. This avoids creating large lists of key paths at once 
            //       that may never get used and will occupy an unnecessary amount of RAM.

            int rangeMax = rangeStart + rangeCount;
            if (rangeMax > arrayNode.Count)
            {
                rangeMax = arrayNode.Count;
            }


            List<string> keyPaths = new List<string>();
            for (int idx = rangeStart; idx < rangeMax; idx++)
            {
                keyPaths.Add(String.Format(DataSourceJson.CollectionElementkeyPathPrefixFormat, resolvedKeyPath, idx));
            }
            return keyPaths as IEnumerable<string>;

        }

        protected IEnumerable<string> GetValueAsDictionaryKeyPaths(JSONNode dictNode, string resolvedKeyPath)
        {
            // TODO: To optimize for large collections, this method should instantiate a class that can provide an IEnumerable<string> 
            //       but then only generates key paths as they are requested. This avoids creating large lists of key paths at once 
            //       that may never get used and will occupy an unnecessary amount of RAM.

            List<string> keyPaths = new List<string>();
            foreach (string key in dictNode.Keys)
            {
                keyPaths.Add(String.Format(DataSourceJson.DictionaryElementkeyPathPrefixFormat, resolvedKeyPath, key));
            }
            return keyPaths as IEnumerable<string>;
        }



        protected JSONNode KeyPathToNode(string resolvedKeyPath)
        {
            // walk down a simple path like:
            //      address.street
            //      addresses[1].street
            //      contacts[2].email_addresses[2]
            //      matrix[1][2]
            //
            // and find the specified node of the data set.

            if ( _keyPathToJsonNodeLookup.ContainsKey( resolvedKeyPath) )
            {
                return _keyPathToJsonNodeLookup[resolvedKeyPath];
            } else
            {
                JSONNode jsonNode = _jsonRootNode;

                string keyPath = resolvedKeyPath;

                while (keyPath != null && keyPath != "")
                {
                    int amountToSkip = 0;
                    MatchCollection arrayMatches = _arrayTokenRegex.Matches(keyPath);
                    if (arrayMatches.Count > 0)
                    {
                        string arrayIndexText = arrayMatches[0].Groups[1].Value;
                        int arrayIndex = int.Parse(arrayIndexText);

                        jsonNode = jsonNode[arrayIndex];
                        amountToSkip = arrayMatches[0].Value.Length;
                     }
                    else
                    {
                        MatchCollection keyMatches = _keyTokenRegex.Matches(keyPath);
                        if (keyMatches.Count > 0)
                        {
                            string key = keyMatches[0].Groups[1].Value;

                            jsonNode = jsonNode[key];
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
                        jsonNode = null;
                        break;      // was not a valid path piece
                    }
                }

                if (jsonNode != null)
                {
                    _keyPathToJsonNodeLookup[resolvedKeyPath] = jsonNode;
                }
                return jsonNode;
            }
        }

        public void UpdateFromJson(string jsonString)
        {
            DataChangeSetBegin();
            _keyPathToJsonNodeLookup.Clear();
            _jsonRootNode = JSON.Parse(jsonString);

            NotifyListeners();
            DataChangeSetEnd();
        }

        protected void NotifyListeners()
        {
            foreach( string keyPath in _keyPathToDataConsumers.Keys )
            {
                NotifyDataChanged(keyPath, GetValue(keyPath), DataChangeType.DatumModified, false);
            }
        }

        protected override bool IsDataSourceAvailable()
        {
            return _jsonRootNode != null;
        }

    }
}

