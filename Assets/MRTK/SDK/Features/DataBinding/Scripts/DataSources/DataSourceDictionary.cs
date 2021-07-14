// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using Microsoft.MixedReality.Toolkit.Utilities;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// Data Source that stores arbitrary information in a Dictionary where
    /// the dictionary key is the keypath and the value is any object that
    /// can be used by some data consumer.
    /// 
    /// Any change to a value in this dictionary will cause any data consumers that have
    /// registered to listen to its key path will be notified of a data change.
    /// 
    /// This simple data source is great for providing a simple list of data to data consumers dynamically at run-time.
    /// </summary>
    /// 

    public class DataSourceDictionary : DataSourceBase
    {
        protected Dictionary<string, object> _dataDictionary = new Dictionary<string, object>();

        public void SetDataSourceDictionary(Dictionary<string, object> dataDictionary)
        {
            _dataDictionary = dataDictionary;
        }


        public override object GetValueInternal(string resolvedKeyPath)
        {
            if (_dataDictionary.ContainsKey(resolvedKeyPath))
            {
                return _dataDictionary[resolvedKeyPath];
            }
            else
            {
                return null;
            }
        }


        public override void SetValueInternal(string resolvedKeyPath, object newValue)
        {
            _dataDictionary[resolvedKeyPath] = newValue;
        }

    } // End of class DataSourceDictionary

} // End of namespace Microsoft.MixedReality.Toolkit.Data
