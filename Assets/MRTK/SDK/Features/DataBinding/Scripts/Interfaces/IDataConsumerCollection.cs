// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Interface for all data consumers that manage collections.  
    /// </summary>
    public interface IDataConsumerCollection : IDataConsumer
    {


        /// <summary>
        /// Request the keyPaths for a range of collection items.
        /// </summary>
        /// <remarks>
        /// The individual items will be provided one at a time to PlaceItem() method of the calling itemPlacer. This allows data fetching and data presenting to occur
        /// in a psuedo parallel fashion.
        /// 
        /// This is used by an Item Placer to request only the subset of items in the collection that are currently relevant, usually those that are currently visible.
        /// </remarks>
        /// <param name="itemPlacer">The Item Placer making this request.</param>
        /// <param name="rangeStart">The zero-based start index of the range to retrieve.</param>
        /// <param name="rangeCount">The number of items to retrieve. If end of collection is reached, fewer items may be provided.</param>
        /// <param name="requestId">A request id that will be passed to the PlaceItem method.</param>
        ///
        /// TODO: Consider breaking out these Collection specific methods into separate interface and dealing wih Unity's lack of multiple inheritance

        void RequestCollectionItems(IDataCollectionItemPlacer itemPlacer, int rangeStart, int rangeCount, string requestId);


        int GetCollectionItemCount();


        void ReturnGameObjectForReuse(int itemIndex, GameObject itemGO);

    }
}
