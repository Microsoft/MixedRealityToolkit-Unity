// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// Base implementation of the IDataSource interface. This base class
    /// contains no Unity specific code and can be used as the base for 
    /// implementing other concrate data source classes.
    /// 
    /// To make any data source available as a MonoBehavior, see
    /// DataSourceGOBase.
    /// 
    /// </summary>

    public class DataSourceBase : IDataSource
    {

        protected IDataKeyPathMapper _dataKeyPathMapper = null;

        protected Dictionary<string, List<IDataConsumer>> _keyPathToDataConsumers = new Dictionary<string, List<IDataConsumer>>();
        protected HashSet<IDataConsumer> _dataConsumers = new HashSet<IDataConsumer>();
        protected Regex _findArrayIndexRegex = new Regex( @"\[(\d+)\]" );


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        public void SetDataKeyPathMapper(IDataKeyPathMapper keyPathMapper)
        {
            _dataKeyPathMapper = keyPathMapper;
        }


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        public string ResolveKeyPath(string resolvedKeyPathPrefix, string localKeyPath)
        {
            if (resolvedKeyPathPrefix == null)
            {
                resolvedKeyPathPrefix = "";
            }

            if (_dataKeyPathMapper != null)
            {
                localKeyPath = _dataKeyPathMapper.GetDataKeyPathFromViewKeyPath(localKeyPath);
            }


            if (IsCollectionAtKeyPath(resolvedKeyPathPrefix))
            {
                // TODO: How do we want to handle multidimentional arrays, eg. threedmodels[10].transform.position[1][2]
                //       adding a '.' would not work between [1] and [2]
                return resolvedKeyPathPrefix + '[' + localKeyPath + ']';
            }
            else if (resolvedKeyPathPrefix != "")
            {
                return resolvedKeyPathPrefix + '.' + localKeyPath;
            }
            else
            {
                return localKeyPath;
            }
        }

        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public object GetValue(string resolvedKeyPath)
        {
            if (IsDataSourceAvailable())
            {
                return GetValueInternal(resolvedKeyPath);
            } else
            {
                return null;
            }
        }

        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public void SetValue(string resolvedKeyPath, object newValue)
        {
            if (IsDataSourceAvailable())
            {
                SetValueInternal(resolvedKeyPath, newValue);
                NotifyDataChanged(resolvedKeyPath, newValue, DataChangeType.DatumModified, false);
            }
        }

 
        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public virtual void DataChangeSetBegin()
        {
            foreach (IDataConsumer dataConsumer in _dataConsumers)
            {
                dataConsumer.DataChangeSetBegin(this);

            }
        }

        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public virtual void DataChangeSetEnd()
        {
            foreach (IDataConsumer dataConsumer in _dataConsumers)
            {
                dataConsumer.DataChangeSetEnd(this);
            }
        }

        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public void AddDataConsumerListener(string resolvedKeyPath, IDataConsumer dataConsumer)
        {

            if (!_keyPathToDataConsumers.ContainsKey(resolvedKeyPath))
            {
                _keyPathToDataConsumers[resolvedKeyPath] = new List<IDataConsumer>();
            }

            _keyPathToDataConsumers[resolvedKeyPath].Add(dataConsumer);
            _dataConsumers.Add(dataConsumer);

            // TODO: This is for dynamically added collection items, but could cause
            //       unintented side effects. Need better solution.
            object value = GetValue(resolvedKeyPath);
            if ( value != null)
            {
                dataConsumer.DataChangeSetBegin(this);
                dataConsumer.NotifyDataChanged(this, resolvedKeyPath, value, DataChangeType.DatumAdded);
                dataConsumer.DataChangeSetEnd(this);
            }

        }


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>

        public virtual void RemoveDataConsumerListener(string resolvedKeyPath, IDataConsumer dataConsumer)
        {
            if (_keyPathToDataConsumers.ContainsKey(resolvedKeyPath))
            {
                _keyPathToDataConsumers[resolvedKeyPath].Remove(dataConsumer);
            }

            //TODO: remove dataconsumer from _dataConsumeer if no more listeners left
        }



        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        // Dictionary data source does not support collections
        public virtual bool IsCollectionAtKeyPath(string resolvedKeyPath)
        {
            return false;
        }


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        public virtual int GetCollectionCount(string resolvedKeyPath)
        {
            return 0;
        }


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        public virtual string GetNthCollectionKeyPathAt(string resolvedKeyPath, int n)
        {
            return null;
        }


        /// <summary>
        /// Implements IDataSource method. See IDataSource.cs for more information
        /// </summary>
        public virtual IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            return null;
        }


        public virtual object GetValueInternal(string resolvedKeyPath)
        {
            // override to return actual value from your custom data source
            return null;
        }

        public virtual void SetValueInternal(string resolvedKeyPath, object newValue)
        {
            // Default provided since not all data sources need to support setting of values
            //
            // NOTE: Remember to call NotifyDataChanged when overriding this method
            // NotifyDataChanged(keyPath, newValue, DataChangeType.DatumModified);
        }


        public void NotifyDataChanged(string resolvedKeyPath, object newValue, DataChangeType changeType, bool isAtomicChange )
        {
            if (_keyPathToDataConsumers.ContainsKey(resolvedKeyPath))
            {
                if ( isAtomicChange )
                {
                    DataChangeSetBegin();   // TODO: optimize to only send to consumers that will actually be notified
                }

                List<IDataConsumer> dataConsumers = _keyPathToDataConsumers[resolvedKeyPath];
                foreach (IDataConsumer dataConsumer in dataConsumers)
                {
                    dataConsumer.NotifyDataChanged(this, resolvedKeyPath, newValue, changeType);
                }

                if (isAtomicChange )
                {
                    DataChangeSetEnd();  // TODO: optimize to only send to consumers that will actually be notified
                }
            }
        }


        public void NotifyAllChanged()
        {
            DataChangeSetBegin();

            foreach ( KeyValuePair<string,List<IDataConsumer>> dataConsumersKeyValue in _keyPathToDataConsumers)
            {
                List<IDataConsumer> dataConsumers = dataConsumersKeyValue.Value;

                foreach (IDataConsumer dataConsumer in dataConsumers)
                {
                    dataConsumer.NotifyDataChanged(this, dataConsumersKeyValue.Key, GetValue(dataConsumersKeyValue.Key), DataChangeType.DatumModified );
                }
            }

            DataChangeSetEnd();

        }


        public string GetDeindexedKeyPath(string resolvedKeyPath)
        {
            if (resolvedKeyPath != "")
            {
                MatchCollection matches = _findArrayIndexRegex.Matches(resolvedKeyPath);

                // TODO: find more efficient way to do this with fewer allocs
                for (int idx = matches.Count - 1; idx >= 0; idx--)
                {
                    Group group = matches[idx].Groups[1];

                    resolvedKeyPath = resolvedKeyPath.Remove(group.Index, group.Value.Length);
                }
            }
            return resolvedKeyPath;
        }

      
        protected virtual bool IsDataSourceAvailable()
        {
            // override if an async datasource may not yet have finished loading
            return true;
        }

    }
}
