// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A Data Consumer that represents a collection of items. 
    /// </summary>
    /// 
    /// <remarks>
    /// Each item in the collection can be of arbitrary type(s) and complexity, but there is an assumption that each item
    /// contains the same types of information and organized in the same way.  Collections can be nested as well. 
    /// 
    /// KeyPaths within a collection item are typically "local" or "view" key paths that are treated as relative paths
    /// that will then be combined with the fully resolved (absolute) keypath of the specific item in the collection container.
    /// 
    /// When a change in a collection is received, this object will instantiate game objects for each collection item
    /// using the specified prefab.  That prefab can contain any number of its own Data Consumers. Note that the data consumers
    /// in an object should not be assigned a data source for any purpose other than testing. These data sources will be replaced when
    /// the prefab is instantiated and modified via data from the data source associated with this data consumer, 
    /// whenever a request for one of the list items is made.
    /// </remarks>

    public class DataConsumerCollection : DataConsumerGOBase, IDataConsumerCollection
    {
        [Tooltip("Path of collection in the originating data source. For collections within collections, this would be relative to outer collection.")]
        [SerializeField]
        protected string collectionKeyPath;

        [Tooltip("The prefab to instantiate for each member of this collection.")]
        [SerializeField]
        protected GameObject itemPrefab;

        [Tooltip("If it exists, an optional key path for an id field that can be used to optimize data fetching.")]
        [SerializeField]
        protected string uniqueIdKeyPath;

        [Tooltip("Replace all data when any data has changed. If all data is occassionally re-fetched, then it may be more efficient to just start over.")]
        [SerializeField]
        protected bool replaceAllOnChange;

        [Tooltip("Collection Item Placer that determines how to present the collection. The default item placer moves each item by the specified item offset.")]
        [SerializeField]
        protected DataCollectionItemPlacerGOBase itemPlacer;


        protected Dictionary<string, GameObject> _idToGameObjectLookup = new Dictionary<string, GameObject>();
        protected IDataObjectPool _dataObjectPool = new DataObjectPool();

        /// <summary>
        /// Called by DataConsumerGOBase to initialize this data consumer at the optimal point 
        /// in the initialization sequence.
        /// </summary>
        protected override void InitializeDataConsumer()
        {
            FindNearestCollectionItemPlacer();

        }

        /// <summary>
        /// Search through game object hierarchy for the nearest IDataCollectionItemPlacer implementation.
        /// </summary>
        /// 
        /// <remarks>
        /// This protected method is unique to collection related Data Consumers. An Item Placer is used to
        /// place game objects (usually a prefab), into the viewers experience when all or a subset
        /// of a collection are requested from the data source.  These are usually inserted into
        /// a managed collection that offers scrolling and other list related UX features.</remarks>
        protected void FindNearestCollectionItemPlacer()
        {
            if (itemPlacer == null)
            {
                itemPlacer = GetComponentInParent(typeof(IDataCollectionItemPlacer)) as DataCollectionItemPlacerGOBase;
            }
            itemPlacer?.SetDataConsumerCollection(this);
        }


        /// <summary>
        /// Return the component types managed by this Data Consumer.
        /// </summary>
        /// 
        /// <remarks>
        /// Normally this returns Unity specific component types, but this one only needs to manage itself.</remarks>
        /// <returns></returns>
        protected override Type[] GetComponentTypes()
        {

            Type[] types = { typeof(DataConsumerCollection) };
            return types;
        }


        /// <summary>
        /// Report whether this data consumer should manage components in any of its child game objects.
        /// </summary>
        /// 
        /// <remarks>
        /// A typical collection only manifests in a single list, so the default behavior is false, but can
        /// be overridden for handling more complex scenarios.
        /// </remarks>
        /// <returns>True = manage DataConsumerCollection objects in this GO's children game objects. False = only manage this one.</returns>
        protected override bool ManageChildren()
        {
            return false;
        }


        /// <summary>
        /// For all components managed by this data consumer, add keypath(s) to listen for.
        /// </summary>
        /// 
        /// <remarks>
        /// For a collection, the only keypath typically is the keypath explicitly assigned to this collection by the inspector. 
        /// This keypath is for the collection itself in the data source. For example, if this list is designed to show an address
        /// book of contacts, and the JSON data for the data source looks like this:
        /// 
        ///    { 
        ///       "data_result" : 
        ///         {
        ///            "result_code" : 0,
        ///            "result_message" : "Success",
        ///            "address_books" : {
        ///               "primary_contacts" :
        ///                  {
        ///                     "type" : "business",
        ///                     "persons" : 
        ///                        [
        ///                           { "first_name": "Jennifer", "last_name" : "Robbins", "email_address" : "jennifer_robbins@contoso.com" },
        ///                           { "first_name": "Kierra", "last_name" : "Schaffer", "email_address" : "kierra_schaffer@contoso.com" },
        ///                           { "first_name": "Alvaro", "last_name" : "McGee", "email_address" : "alvaro_mcgee@contoso.com" }
        ///                        ]
        ///                  }
        ///               }
        ///            }
        ///        }
        ///     }
        ///           
        /// then the key path would be: "data_result.address_books.primary_contacts.persons"
        /// 
        /// </remarks>
        ///
        /// <param name="componentType">Which component type is being provided. Only a DataConsumerCollection in this case.</param>
        /// <param name="component">The component of that type, typically the game object on which this script exists.</param>
        /// 
        protected override void AddVariableKeyPathsForComponent(Type componentType, Component component)
        {
            if (collectionKeyPath != null)
            {
                AddKeyPathListener(collectionKeyPath);
            }
            else
            {
                Debug.LogError("DataConsumerCollection does not specify a collectionKeyPath. No listener registered.");
            }
        }

        /// <summary>
        /// An associated data source is reporting that data has changed.
        /// </summary>
        /// 
        /// <remarks>
        /// The only key path typically reported is the keypath provided by AddVariableKeyPathsForComponent, which is the
        /// key path of the collection itself in the data source.
        /// 
        /// </remarks>
        /// 
        /// <param name="dataSource">The data source reporting a data change in the collection.</param>
        /// <param name="resolvedKeyPath">Fully resolved key path after key mapping and disambiguating any parent collections.</param>
        /// <param name="localKeyPath">the originally provided local key path, usually the one provided in the Unity inspector.</param>
        /// <param name="newValue">An object that represents the collection.</param>
        /// <param name="dataChangeType">The nature of the data change.</param>
        protected override void ProcessDataChanged(IDataSource dataSource, string resolvedKeyPath, string localKeyPath, object newValue, DataChangeType dataChangeType )
        {
            if (itemPrefab != null)
            {
                if (localKeyPath == collectionKeyPath)
                {
                    itemPlacer?.NotifyCollectionDataChanged(dataChangeType);
                }
            }
        }

        /// <summary>
        /// Request the specified range of items and provide them to the item placer.
        /// </summary>
        /// 
        /// <remarks>
        /// See RequestCollectionItems() method of IDataConsumer interface.
        /// </remarks>

        public void RequestCollectionItems(IDataCollectionItemPlacer itemPlacer, int indexRangeStart, int indexRangeCount, string requestId)
        {
            itemPlacer.StartPlacement();
            StartCoroutine(InstantiatePrefabs(itemPlacer, indexRangeStart, indexRangeCount, requestId));
            itemPlacer.EndPlacement();
        }


        /// <summary>
        /// Instantiate the specified prefabs and provide to the item placer.
        /// </summary>
        /// 
        /// <remarks>
        /// For the specified range, instantiate prefabs as a CoRoutine and provide them one at a time to the specified item placer.
        /// 
        /// Any prefab data consumers in the prefab are automatically connected to the same data source as the collection itself, and
        /// its key path prefix is set to the full resolve prefix for this collection combined with the array index position of the item in the
        /// array.
        /// </remarks>
        /// 
        /// <param name="itemPlacer">Item placer to receive the specified range of prefabs.</param>
        /// <param name="indexRangeStart">Zero based start of the range to instantiate.</param>
        /// <param name="indexRangeCount">Number of list items to instantiate.</param>
        /// <param name="requestId">Arbitrary unique request id that will be provided to the item placer.</param>
        /// 
        /// <returns>IEnumerator required for a CoRoutine.</returns>
        protected IEnumerator InstantiatePrefabs(IDataCollectionItemPlacer itemPlacer, int indexRangeStart, int indexRangeCount, string requestId)
        {
            IEnumerable<string> collectionItemsKeyPaths = _dataSource.GetCollectionKeyPathRange(collectionKeyPath, indexRangeStart, indexRangeCount);

            yield return null;

            int itemIndex = indexRangeStart;

            foreach (string itemKeyPath in collectionItemsKeyPaths)
            {

                GameObject childPrefab = GetPrefabInstance();

                UpdatePrefabDataConsumers(childPrefab, itemKeyPath);

                yield return null;

                itemPlacer?.PlaceItem(requestId, indexRangeStart, indexRangeCount, itemIndex++, childPrefab);

                // TODO: add logic to yield after a specified # of milliseconds since some
                //       items can be fabricated faster than others
                yield return null;
            }

        }


        /// <summary>
        /// Return the number of items in the collection at this key path.
        /// </summary>
        /// 
        /// <remarks>
        /// Return the collection size at the time of this call. Note that no attempt is made to track changes in collection makeup or size
        /// during the course of processing a collection.
        /// 
        /// </remarks>
        /// <returns>Collection size.</returns>
        public int GetCollectionItemCount()
        {
            return _dataSource.GetCollectionCount(collectionKeyPath);
        }


        protected GameObject GetPrefabInstance()
        {
            GameObject newObject;

            if (_dataObjectPool.IsEmpty())
            {
                newObject = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity, transform);
            } 
            else
            {
                newObject = _dataObjectPool.GetObjectFromPool() as GameObject;
            }

            newObject.transform.parent = transform;
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.SetActive(true);
            return newObject;
        }


        /// <summary>
        /// Report that the specified game object (prefab) is no longer visible and can be recycled.
        /// </summary>
        /// 
        /// <remarks>
        /// This is the mechanism for returning a prefab to a pool of availabe prefabs so that they
        /// can be repopulated with new embedded data, but otherwise are ready-to-go.
        /// </remarks>
        /// 
        /// <param name="itemIndex">Index of the game object being returned.</param>
        /// <param name="itemGO">The game object itself.</param>
        public void ReturnGameObjectForReuse(int itemIndex, GameObject itemGO )
        {
            itemGO.transform.localPosition = Vector3.zero;
            itemGO.transform.localRotation = Quaternion.identity;
            itemGO.SetActive(false);

            Component[] dataConsumers = itemGO.GetComponentsInChildren(typeof(DataConsumerGOBase));

            // DataConsumerGOBase[] dataConsumers = prefab.GetComponentsInChildren(typeof(DataConsumerGOBase)) as DataConsumerGOBase[];
            foreach (Component component in dataConsumers)
            {
                DataConsumerGOBase dataConsumer = component as DataConsumerGOBase;

                dataConsumer.Detach();
            }

            if (_dataObjectPool.ReturnObjectToPool(itemGO))
            {
                itemGO.transform.parent = null;
            }
            else
            {
                Destroy(itemGO);
            }
        }


        /// <summary>
        /// Update all IDataConsumer Components in the prefab GO hierarchy.
        /// </summary>
        /// 
        /// <remarks>
        /// Make sure the data consumers in a view prefab are associated with the
        /// correct keypath and the datasource that will provide data.
        /// 
        /// 
        /// </remarks>
        /// 
        /// <param name="prefab"></param>
        /// <param name="collectionItemKeyPathPrefix"></param>
        protected void UpdatePrefabDataConsumers(GameObject prefab, string collectionItemKeyPathPrefix)
        {
            Component[] dataConsumers = prefab.GetComponentsInChildren(typeof(DataConsumerGOBase));

            prefab.name = collectionItemKeyPathPrefix;

            // DataConsumerGOBase[] dataConsumers = prefab.GetComponentsInChildren(typeof(DataConsumerGOBase)) as DataConsumerGOBase[];
            foreach (Component component in dataConsumers)
            {
                DataConsumerGOBase dataConsumer = component as DataConsumerGOBase;

                dataConsumer.Attach(DataSource, DataController, collectionItemKeyPathPrefix);
            }

        }

    }

}
