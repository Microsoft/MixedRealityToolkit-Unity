// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Interface for all data consumers.  A data consumer is the intermediary between a data source and a view used to
    /// present information.  The view will typically be a prefab where specific pieces of information identify which
    /// datum will be used to modify that presented information at runtime. 
    /// 
    /// Key Concepts:
    /// 
    /// Key Path (string) - See IDataSource for more information. A key path identifies a specific datum in the data source.
    /// That datum may be a primitive, or an entire data subset.
    /// 
    /// Change Set - A grouping of data changes.  All data source notifications of changed data will be within a Change Set.
    /// This allows for multiple related changes to be grouped into one atomic update of a data view. This is critical when
    /// multiple data items influence a single piece of presented information, like a single sentence with 3 pieces of variable
    /// information.
    /// 
    /// Data Change Notifications - All changes to a data source are provide via change notifications. Ever data consumer can
    /// sign up to listenf or any number of data changes to specific data items within the data source. Also mulitple consumers
    /// can listen to the same data source.
    /// 
    /// For data that represents a collection (eg. list) of items, a data consumer can request specific subsets of that list.
    /// This allows for pagination and for virtualization, which is critical for supporting large data sets.
    /// </summary>
    public interface IDataConsumer
    {
        /// <summary>
        /// Prepare this data consumer for use in active, visible state, typically associated with one prefab in a list. Called when re-using a prefab that's in an object pool.
        /// </summary>
        /// <param name="dataSource">Which data source to attach to for this re-use of the object</param>
        /// <param name="resolvedKeyPathPrefix">The path prefix for this object to be appended to front of all local keypaths.</param>
        void Attach(IDataSource dataSource, IDataController controller, string resolvedKeyPathPrefix);


        /// <summary>
        /// Detach this data consumer and prepare to return to object pool
        /// </summary>


        void Detach();


        /// <summary>
        /// Get all registered keyPaths
        /// </summary>
        /// <remarks>
        /// Get all key paths this data consumer wishes to process. A key path is a specifier of a unique data item (of arbitrary type and complexity) in a data source.
        /// </remarks>
        ///
        /// <returns>IEnumerable for iterating through the returned string keyPaths.</returns>

        IEnumerable<string> GetDataKeyPaths();

        /// <summary>
        /// Mark beginning of a related set of data changes.
        /// </summary>
        /// <remarks>
        /// This bracketing of data change sets is particularly useful when a single asset, like a text string, is impacted by more than one piece of changed data, like
        /// a sentence that has two embedded variables.
        /// </remarks>
        /// 
        /// <param name="dataSource">The data source that is starting a data change set.</param>


        void DataChangeSetBegin(IDataSource dataSource);


        /// <summary>
        /// Notification of a data change.
        /// </summary>
        /// <remarks>
        /// For each registered keyPath, this will be called if the data at that keyPath has changed.  If the keyPath represents a complex data structure, like an
        /// entire collection of entries, in concept it will notify if any of that data has changed.  This currently is not optimized and will be called if there is
        /// a wholesale update of the dataset, but not necessarily detecting if there are actually any changes in the subset represented by this keyPath.
        /// </remarks>
        /// <param name="dataSource">The data source that sent this notification.</param>
        /// <param name="keyPath">The resolved keyPath for the data that has changed.</param>
        /// <param name="newValue">The new value for the data represented by the keyPath.</param>

        void NotifyDataChanged(IDataSource dataSource, string keyPath, object newValue, DataChangeType changeType );


        /// <summary>
        /// Mark beginning of a related set of data changes.
        /// </summary>
        /// <remarks>
        /// This bracketing of data change sets is particularly useful when a single asset, like a text string, is impacted by more than one piece of changed data, like
        /// a sentence that has two embedded variables.
        /// </remarks>
        /// 
        /// <param name="dataSource">The data source that is ending a data change set.</param>


        void DataChangeSetEnd(IDataSource dataSource);

    }
}
