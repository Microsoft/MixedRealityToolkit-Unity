// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Base class for Data Sources that are accessible through a Game Object MonoBehaviour proxy 
    /// </summary>
    /// 
    /// <remarks>
    /// 
    /// This class encapsulates a Data Source object so that it can exist as a MonoBehaviour, which
    /// is important for using the inspector to connect Data Consumers to a Data Source.
    /// 
    /// By implementing this as a proxy, most of code for any Data Source concrete implmementation
    /// can exist as pure C# with no Unity dependencies.
    /// 
    /// </remarks>

    public abstract class DataSourceGOBase : MonoBehaviour, IDataSource, IDataSourceProvider
    {
        [Tooltip("Optional DataKeyPathMapper that translates between local view key paths and data source key paths. This is useful for re-using prefabs.")]
        [SerializeField]
        private DataKeyPathMapperGODictionary keyPathMapper;

        protected IDataSource _dataSource;

        protected void Awake()
        {
            _dataSource = AllocateDataSource();
        }

        protected void Start()
        {
            if (keyPathMapper != null)
            {
                _dataSource?.SetDataKeyPathMapper(keyPathMapper as IDataKeyPathMapper);
            }
            InitializeDataSource();
        }

        public bool IsEnabled()
        {
            return enabled;
        }

        /// <summary>
        /// Allocate the correct data source
        /// </summary>
        /// <remarks> 
        /// 
        /// Attach whatever data source is desired here in your derived class if not already allocated.
        /// This method passes most IDataSource calls directly to the IDataSource provided in your
        /// overridden method.
        /// 
        /// Further initialization can occur when InitializeDataSource() is called.
        /// </remarks>
        public virtual IDataSource AllocateDataSource()
        {
            // Override this method to allocate an appropriate data source if not already allocated.
            return null;
        }


        /// <summary>
        /// IDataSourceProvider method to get this data source.
        /// </summary>
        /// <remarks>

        /// Attach whatever data source is desired here in your derived class if not already allocated.  
        /// Further initialization can occur when InitializeDataSource() is called.
        ///
        /// NOTE: This does not return _dataSource because then any logic in the subclass of this class will
        /// never execute for any methods that overrides an IDataSource interface method.
        /// </remarks>
        public virtual IDataSource GetDataSource()
        {
            // This should be overridden to allocate an appropriate data source if not already allocated.
            return this;
        }


        protected virtual void InitializeDataSource()
        {
            // override for additional initialization.
        }

        public string ResolveKeyPath(string resolvedKeyPathPrefix, string localKeyPath)
        {
            return _dataSource?.ResolveKeyPath(resolvedKeyPathPrefix, localKeyPath);
        }

        public void SetDataKeyPathMapper( IDataKeyPathMapper keyPathMapper )
        {
             _dataSource?.SetDataKeyPathMapper(keyPathMapper);

        }
    
        public virtual object GetValue(string resolvedKeyPath)
        {

            return _dataSource?.GetValue(resolvedKeyPath);
        }


        public virtual void SetValue(string resolvedKeyPath, object newValue)
        {
                _dataSource?.SetValue(resolvedKeyPath, newValue);
        }


        public void AddDataConsumerListener( string resolvedKeyPath, IDataConsumer dataConsumer)
        {
            _dataSource?.AddDataConsumerListener(resolvedKeyPath, dataConsumer);
        }


        public void RemoveDataConsumerListener(string resolvedKeyPath, IDataConsumer dataConsumer)
        {
                _dataSource?.RemoveDataConsumerListener(resolvedKeyPath, dataConsumer);
        }

        public void DataChangeSetBegin()
        {
            _dataSource?.DataChangeSetBegin();
        }

        public void DataChangeSetEnd()
        {
             _dataSource?.DataChangeSetEnd();
        }


        // Dictionary data source does not support collections
        public virtual bool IsCollectionAtKeyPath(string resolvedKeyPath)
        {
            return _dataSource?.IsCollectionAtKeyPath(resolvedKeyPath) ?? false;
        }

        public virtual int GetCollectionCount(string resolvedKeyPath)
        {
           return  _dataSource?.GetCollectionCount(resolvedKeyPath) ?? 0;
        }

        public virtual string GetNthCollectionKeyPathAt(string resolvedKeyPath, int n)
        {
            return _dataSource?.GetNthCollectionKeyPathAt(resolvedKeyPath, n);
        }

        public IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            return _dataSource?.GetCollectionKeyPathRange(resolvedKeyPath, rangeStart, rangeCount );
        }

        public void NotifyDataChanged( string resolvedKeyPath, object newValue, DataChangeType dataChangeType, bool isAtomicChange )
        {
            _dataSource?.NotifyDataChanged(resolvedKeyPath, newValue, dataChangeType, isAtomicChange);
        }

        public void NotifyAllChanged()
        {
            _dataSource?.NotifyAllChanged();
        }
    }
}
