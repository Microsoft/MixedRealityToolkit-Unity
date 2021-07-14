// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Specify the nature of the data change in notifications
    /// </summary>
 
    public enum DataChangeType
    {
        DatumAdded,                     // A new datum (of arbitrary complexity) came into being that did not exist prior to this
        DatumModified,                  // An existing datum has been modified
        DatumRemoved,                   // An existing datum has been removed
        CollectionItemAdded,            // A new item (of arbitrary complexity) has been added to a collection
        CollectionItemRemoved           // An item in a list has been removed from a collection
    }


    /// <summary>
    ///  Interface for all data sources. A data source is any managed set of data
    ///  that can be used to populate a data view via a data consumer. This data
    ///  can be static or dynamic, with any changes being reported to any data consumers
    ///  who have registered to receive change notifications.
    ///  
    /// Key concepts:
    /// 
    /// Key Path (string) - a unique accessor to specific data items within a data set.
    ///   Although a Key Path can be any unique identifier per data item, all
    ///   current impmlementations use the concept of a logical user readable
    ///   specifier that indicates the navigational position of the data of interest
    ///   relative to the entire data set. It is modelled on javascript's concept of 
    ///   lists, dictionaries and primitives, such that keypaths are correct javascript
    ///   statements for accessing data that can be represented in JSON. The advantage 
    ///   of this approach is that it correlates very well with both JSON and XML, 
    ///   which are the two most prevalent means of transfering information from back-end services.
    ///   
    ///   Example key paths:
    ///   
    ///   temperature
    ///   contacts[10].firstName
    ///   contacts
    ///   contacts[10].addresses[3].city
    ///   [10].title
    ///   kingdom.animal.mammal.aardvark.diet.foodtypes.termites
    ///  
    ///   Given that a key path is an arbitrary string with no required taxonomy, the actual
    ///   data specifiers could be any method of describing what data to retrieve. XML's XPath is an
    ///   example of a viable key path schema. As long as key paths provided by the data consumer
    ///   are consistent with the keypaths expected by the data source, everything will work. 
    ///   Furthermore Key Path Mappers can be implemented to translate between different schemas.
    ///   
    ///   Resolving a keypath means combining two keypaths: 1) a keypath describes how to access a specific
    ///   subset of a larger dataset, such as one entry in a list of many entries, and 2)
    ///   a partial (relative) keypath that represents just a datum within that list or map entry.
    ///   This makes it possible to treat a subset of the data in such a way that it does
    ///   not matter where in a larger data set hierarchy it actually exists. The most critical
    ///   use of this ability is to describe the data of a single entry in a list without worrying about which
    ///   entry in that list the current instance is referencing.
    ///   
    /// Key Path Mapper (IDataKeyPathMapper) - A dependency injected helper that can map
    ///   between namespaces used in a data set and name spaces used in a view object, like
    ///   a prefab meant to be re-usable with different data sources, like a contact list.
    ///   
    /// Data Consumer (IDataConsumer) - An object that knows how to consume information being managed by
    ///   a data source and use that data to populate data views.
    ///   
    /// Data Consumer Listener - Data Consumers that have registered to be notified of any changes to a specific
    ///   data item in a dataset. Whenever the data specified has changed (or suspected to have changed), the 
    ///   Data Consumer(s) will be notified.
    ///   
    /// Collection - Any subset of the data source that is a repeated array of the same types of information. This
    ///   special consideration of the strucure of the data is critical for populating list views where items 
    ///   are presented in a list.  Data sources and data consumers can support nested lists, such as a list of keywords
    ///   associated with each photo in a list of photos. The keypath for the keywords would be relative to the photo, 
    ///   and the keypath for the photos would be relative to the list, and the keypath of the list would be relative 
    ///   to either the nearest parent list, or the root of the data set.
    ///   
    /// </summary>
    public interface IDataSource
    {

        /// <summary>
        /// Set a keyPath mapper
        /// </summary>
        /// 
        /// <remarks>
        /// If the view (data consumer) uses generic keyPaths that are not the same as the data source, then this
        /// mapper will translate between data and view keyPaths. This allows a generic view to be used with a wide
        /// variety of data sources without modification.
        /// </remarks>
        ///
        /// <param name="dataKeyPathMapper"s>Data keyPath mapper to assign.</param>

        void SetDataKeyPathMapper(IDataKeyPathMapper dataKeyPathMapper);



        /// <summary>
        /// </summary>
        /// 
        /// <remarks>
        /// Given a resolved (data source namespace) keyPath, and a local (data consumer namespace) keypath,
        /// return a fully resolved data source keyPath for that data item.
        /// 
        /// This resolvedKeyPathPrefix can be "" (empty string) if the localKeyPath represents an
        /// absolute location in the data set. If the localKeyPath is a member of one item in
        /// an array of items, then the prefix will be that of the data container itself containing 
        /// the array
        /// 
        /// If the specified item can not be found in the data source, null is returned.
        /// </remarks>
        /// 
        /// <param name="resolvedkeyPathPrefix">The data path prefix.</param>
        /// <param name="locakKeyPath">The local keyPat to resolve. Note that this may be mapped by a DataKeyPathMapper</param>
        ///
        /// <returns>A string tha can be used to map to a view data consumer.</returns>
        string ResolveKeyPath(string resolvedkeyPathPrefix, string locakKeyPath);


        /// <summary>
        /// Get the value associted with the specified keyPath and prefix
        /// </summary>
        /// 
        /// <remarks>
        /// The returned value can be of any arbitrary complexity or data type from primitives to complex nesting of
        /// dictionaries and arrays, to image or audio data.
        /// 
        /// The fullyQualifiedPrefix is provided for lists (or collection of lists) to find the correct instance of a specific local keypath.
        /// If not processing a collection, this can be either null or "". It is considered fully qualified becuase it has been resolved
        /// to the namespaces of the data source where each subcomponent may have been mapped from a view keyPath to a data keyPath.
        /// This means that the path may not be recognizeable on casual observation relative to the keyPath variables
        /// embedded in views and subviews.
        /// 
        /// Note that a keyPath for a collection is useful for regenerating an entire collection. If the object is a collection,
        /// GetValue() returns an IEnumberable<string> that can be used to enumerate the keypaths of all children in the collection.
        /// 
        /// </remarks>
        ///
        /// <param name="resolvedKeyPath">Prefix for a collection</param>
        /// <returns>object of arbitrary data type or complexity, but will most often be a primitive type.</returns>
        object GetValue(string resolvedKeyPath);


        /// <summary>
        /// Set the value associated with the specified keyPath
        /// </summary>
        /// 
        /// <remarks>
        /// This is currently useful when the data embedding is being used for local, realtime changes of presented information.
        /// 
        /// The fullyQualifiedPrefix is provided for lists (or collection of lists) to find the correct instance of a specific local keypath.
        /// If not processing a collection, this can be either null or "". It is considered fully qualified becuase it has been resolved
        /// to the namespaces of the data source where each subcomponent may have been mapped from a view keyPath to a data keyPath.
        /// This means that the path may not be recognizeable on casual observation relative to the keyPath variables
        /// embedded in views and subviews.
        /// 
        /// Although not implemented yet, it could also be used to persist changes to a back-end data store of some kind,
        /// such as might be useful for a data entry form.
        /// </remarks>

        void SetValue(string resolvedKeyPath, object value);


        /// <summary>
        /// Add a new data consumer listener for the specified keyPath
        /// </summary>
        /// 
        /// <remarks>
        /// Specify a listener for any particular keyPath, such as when the data represented by that keyPath changes, 
        /// the NotifyDataChanged() method of the IDataConsumer will be called. 
        /// </remarks>
        /// 
        /// <param name="resolvedKeyPath">The keyPath for the data to monitor for changes.</param>
        /// <param name="dataConsumer">The data consumer that is to be notified.</param>


        void AddDataConsumerListener(string resolvedKeyPath, IDataConsumer dataConsumer);


        /// <summary>
        /// Remove a data consumer listener for the specified keyPath
        /// </summary>
        /// 
        /// <remarks>
        /// Remove a previously added listener for the specified keyPath such that no
        /// further notifications will be provided. Note that this will not impact
        /// other data consumers that may have also registered listeners.
        /// </remarks>
        /// 
        /// <param name="resolvedKeyPath">The keyPath to stop monitoring.</param>
        /// <param name="dataConsumer">The data consumer to remove for that keyPath.</param>

        void RemoveDataConsumerListener(string resolvedKeyPath, IDataConsumer dataConsumer);

        /// <summary>
        /// Determine if  keyPath represents a data collection
        /// </summary>
        /// 
        /// <remarks>
        /// When a data consumer is notified of a data change at the specified keyPath, this
        /// can be used to determine if that object represents a collection. The collection can
        /// be of any type that can be accessed reliably via an index value. This is useful for
        /// auto-generating and presenting a collection that contains variable data, 
        /// like a collection of users, or playlists.
        /// </remarks>
        /// 
        /// <param name="resolvedKeyPath">The keyPath of data to be checked.</param>
        /// <param name="dataConsumer">The data consumer to remove for that keyPath.</param>
        /// 
        /// <returns>boolean indication of whether that keyPath represents a collection.</returns>

        bool IsCollectionAtKeyPath(string resolvedKeyPath);


        /// <summary>
        /// If a keyPath represents a collection, return the number of items in that collection.
        /// </summary>
        /// 
        /// <remarks>
        /// This can be used by a data consumer to pre-allocate resources to receive the
        /// collection, and also to determine the valid index range of 0 to N-1 when using
        /// GetNthCollectionKayPathAt() to retrieve entries in the collection.
        /// </remarks>
        /// 
        /// <param name="keyPath">The keyPath of data to be checked.</param>
        /// 
        /// <returns>integer size of the collection. An empty collection is valid.</returns>        
        int  GetCollectionCount(string resolvedKeyPath);


        /// <summary>
        /// If a keyPath represents a collection, return the nth entry of that collection.
        /// </summary>
        /// 
        /// <remarks>
        /// The returned entry can be of arbitrary type and complexity and may even itself contain
        /// collections.  
        /// </remarks>
        /// 
        /// <param name="resolvedKeyPath">The keyPath of the collection itself.</param>
        /// 
        /// <returns>integer size of the collection. An empty collection is valid.</returns>        
        string GetNthCollectionKeyPathAt(string resolvedKeyPath, int n);


        /// <summary>
        /// Get an enumerable collection of keyPath strings for the specified collection index range. 
        /// </summary>
        /// 
        /// <remarks>
        /// This is useful for list virtualization where you only fetch the subportion of the list that is
        /// currently visible.
        /// </remarks>
        /// 
        /// <param name="resolvedKeyPath">The keyPath of the collection itself.</param>
        /// <param name="rangeStart">The index of the first element of the desired range.</param>
        /// <param name="rangeCount">The number of entries to return, (or fewer if end of collection is reached).</param>


        IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount);


        /// <summary>
        /// Begin a data change set. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// The default behavior is to notify all registered data consumers that a change set has begun.
        /// 
        /// This is mainly exposed in this interface to allow for nested data sets where a
        /// super data set has child data sets with unknown concrete implementations, and 
        /// wants to synchronize set boundaries.
        /// </remarks>
        void DataChangeSetBegin();

        /// <summary>
        /// End a data change set. 
        /// </summary>
        /// 
        /// <remarks>
        /// The default behavior is to notify all registered data consumers that a change set has begun.
        ///
        /// This is mainly exposed in this interface to allow for nested data sets where a
        /// super data set has child data sets with unknown concrete implementations, and 
        /// wants to synchronize set boundaries.
        /// </remarks>

        void DataChangeSetEnd();

        /// <summary>
        /// Inform data source that its data has changed 
        /// </summary>
        /// <remarks>
        /// This is used in situations where SetValue() is not being used to modify
        /// the managed data, and the data source is not otherwise aware that the data
        /// has changed. This directly propagates the notification to all data consumers
        /// who are listening for this specific keyPath.
        /// </remarks>
        /// 
        /// <param name="keyPath"></param>
        /// <param name="newValue"></param>
        /// <param name="changeType"></param>
        /// <param name="isAtomicChange">Is this the only change notification in a set of related changes.</param>


        void NotifyDataChanged(string keyPath, object newValue, DataChangeType changeType, bool isAtomicChange );

        /// <summary>
        /// Notify all listeners that data has changed. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// This method is useful for batching multiple changes before notifying data consumers. An example situation
        /// is when initially populating a static dataset before notifying dataconsumers of the availability of data. Eg:
        /// 
        /// SetValue( keypath1, value1 );
        ///     ...
        /// SetValue( keypathn, valuen );
        /// DataChangeSetBegin();
        /// NotifyAllChanged();
        /// DataChangeSetEnd();
        /// 
        /// Note that this currently will be less efficient than triggering changes on individual data items as they occur.
        /// In the current implementation, all listeners will be notified even if that particular data items has not
        /// actually been changed. In future versions, a list will be maintained of all changed items to make this
        /// option more efficient.
        /// </remarks>

        void NotifyAllChanged();
    }
}
