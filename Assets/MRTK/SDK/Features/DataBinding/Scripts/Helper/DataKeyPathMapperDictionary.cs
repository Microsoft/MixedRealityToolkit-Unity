// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A simple key path mapper that can associated a local key path with a data key path.
    /// 
    /// To allow for the existence of common data presentation prefabs that can be reused,
    /// such as a address book entry, a photo album entry, or even a simple list of text entries, 
    /// there is a need to decouple namespaces between these prefab "views" and the data sources 
    /// that will be used to populate the view.
    /// 
    /// The data source field names for the various potential sources of similar information 
    /// (eg. contact list, or photo album entries), are unlikely to be consistent across
    /// data sources, but the desire is to be able to map them to curated prefab views
    /// that can present this information in compelling ways.
    /// 
    /// This class has no Unity specific dependencies. To use this in the context of Unity
    /// components and editor inspector, see DataKeyPathMapperGODictionary.
    /// 
    /// </summary>
    /// 
    public class DataKeyPathMapperDictionary : IDataKeyPathMapper
    {
        protected Dictionary<string, string> _viewToDataKeyPathLookup = new Dictionary<string, string>();
        protected Dictionary<string, string> _dataToViewKeyPathLookup = new Dictionary<string, string>();

        public void AddKeyMapping(string viewKeyPath, string dataKeyPath)
        {
            _viewToDataKeyPathLookup[viewKeyPath] = dataKeyPath;
            _dataToViewKeyPathLookup[dataKeyPath] = viewKeyPath;
        }


        public void RemoveKeyMapping(string viewKeyPath, string dataKeyPath)
        {
            _viewToDataKeyPathLookup.Remove(viewKeyPath);
            _dataToViewKeyPathLookup.Remove(dataKeyPath);
        }


        public string GetDataKeyPathFromViewKeyPath(string viewKeyPath)
        {
            if ( _viewToDataKeyPathLookup.ContainsKey( viewKeyPath ) )
            {
                return _viewToDataKeyPathLookup[viewKeyPath];
            } else
            {
                return viewKeyPath;
            }
        }

        public string GetViewKeyPathFromDataKeyPath(string dataKeyPath)
        {
            if (_viewToDataKeyPathLookup.ContainsKey(dataKeyPath))
            {
                return _dataToViewKeyPathLookup[dataKeyPath];
            }
            else
            {
                return dataKeyPath;
            }
        }
    }
}
