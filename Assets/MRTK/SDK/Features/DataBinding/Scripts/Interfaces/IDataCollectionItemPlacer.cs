// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Interface for placing items from a list into the gameobject heirarchy in a meaningful way.
    /// </summary>
    /// 
    /// <remarks>
    /// An object implementing this interface can be dependency injected into the DataConsumerCollection (or
    /// similar) Component via the Unity inspector.
    /// 
    /// In addition to controlling how a list is visualized, it can also be used to manage list pagination, virtualization
    /// and predictive prefetching.
    /// 
    /// When a list has changed, this object will be notified. It can then request a range of game objects to be provided for placement,
    /// where each game object has been modified to reflect the data associated with that entry in the list. The game object will
    /// typically be a prefab of arbitrary copmlexity.
    /// 
    /// </remarks>
    public interface IDataCollectionItemPlacer
    {

        /// <summary>
        /// set the data consumer for a collection that can provide gameobjects to be placed. This will normally be set by the data consumer
        /// that was assigned this placer during intialization phase.
        /// </summary>
        /// 
        /// <param name="dataConsumerCollection">Data consumer for a collection that will provide game objects to place.</param>
        void SetDataConsumerCollection(IDataConsumerCollection dataConsumerCollection);

        /// <summary>
        /// Start placement for the requested range of game objects.
        /// 
        /// This can be used to reset any state information in preparation for receiving the new items.
        /// 
        /// All PlaceItem() method calls will occur within StartPlacement() and EndPlacement() method calls.
        /// </summary>
        void StartPlacement();

        /// <summary>
        /// Place a game object into the experience previously requested via IDataConsumer.RequestCollectionItems()
        /// </summary>
        /// 
        /// <remarks>
        /// All calls to this method will be within a StartPlacement() and an EndPlacement() call.
        /// 
        /// The request id will always be the request Id provided to the  IDataConsumer.RequestCollectionItems()
        /// method that triggered the calls to this method. This can be used to associate additional data in a
        /// complex scenario where there may be more than one data consumer associated with this item placer.
        /// 
        /// Note that the range start and range count could have been provided via state data associated with the
        /// requestId, but to simplify the most common uses, it is provided explicitly.
        /// </remarks>
        /// 
        /// <param name="requestId">The request id provided to RequestCollectionItems() </param>
        /// <param name="indexRangeStart">The start of the range previously requested.</param>
        /// <param name="indexRangeCount">The range count previously requested.</param>
        /// <param name="itemIndex">The absolute item index in the data source array.</param>
        /// <param name="itemGO">The game object created using  the data at the specified item index.</param>
        void PlaceItem(string requestId, int indexRangeStart, int indexRangeCount, int itemIndex, GameObject itemGO);


        /// <summary>
        /// End placement for the requested range of game objects
        /// </summary>
        void EndPlacement();

        /// <summary>
        /// Notification that the collection being managed by the data consumer for this object has changed.
        /// </summary>
        /// <remarks>
        /// This method will typically trigger a IDataConsumer.RequestCollectionItems() method call to
        /// request all or a subset of the items being managed in the collection.  This back and forth
        /// allows for precise control of paging and virtualization to optimze the presentation of
        /// information.</remarks>
        ///
        /// <param name="dataChangeType">The nature of the data change, typically CollectionItemAdded, or CollectionItemRemoved.</param>
        void NotifyCollectionDataChanged(DataChangeType dataChangeType);

    }
}
