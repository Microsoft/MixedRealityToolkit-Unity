// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data pool of objects that can be used for object re-use.
    /// 
    /// This is designed to reduce memory allocations and hence
    /// the frequency and duration of garbage collections. It can
    /// also reduce the instantiation time of a prefab when populating
    /// large lists.
    /// </summary>
    /// 
    public class DataObjectPool : IDataObjectPool
    {
        const int DefaultPoolSize = 50;

        protected Queue<object> _objectPoolObjects = new Queue<object>();
        protected int _poolMaximumSize = DefaultPoolSize;

        public DataObjectPool(int poolSize = DefaultPoolSize)
        {
            _poolMaximumSize = poolSize;
        }

        public void SetMaximumPoolSize(int maxSize, bool resizeNow)
        {
            _poolMaximumSize = maxSize;
            if (resizeNow)
            {
                while (_objectPoolObjects.Count > maxSize)
                {
                    _objectPoolObjects.Dequeue();
                }
            }
        }

        public bool IsEmpty()
        {
            return _objectPoolObjects.Count == 0;
        }


        public bool ReturnObjectToPool(object objectToReturn)
        {
            if (_objectPoolObjects.Count <= _poolMaximumSize)
            {
                _objectPoolObjects.Enqueue(objectToReturn);
                return true;
            } else
            {
                return false;
            }
        }

        public object GetObjectFromPool()
        {
            if (_objectPoolObjects.Count > 0)
            {
                return _objectPoolObjects.Dequeue();
            }
            else
            {
                return null;
            }
        }
    }
}
