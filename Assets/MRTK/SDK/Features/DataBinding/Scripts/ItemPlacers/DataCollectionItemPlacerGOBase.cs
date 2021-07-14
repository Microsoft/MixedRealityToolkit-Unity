// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data collection item placer base implementation that supports paging.
    /// 
    /// This is a base object that can be derived from to support more complex scenarios.
    /// 
    /// A typical item placer will populate a UX elemented designed to present lists or grids of
    /// items, and typically also supports paging and/or scrolling for larger lists.
    /// 
    /// TODO: Make a simpler GO base class that is used in DataConsumerCollection so that it is not
    /// assumed what functionality may be desired in an item placer.
    /// 
    /// </summary>

    
    public abstract class DataCollectionItemPlacerGOBase : MonoBehaviour, IDataCollectionItemPlacer
    {
        [Tooltip("This specifies the default page size unless GetItemCountPerPage() method is overridden.")]
        [SerializeField]
        protected int maximumVisibleItems = 50;

        [Tooltip("Optional request ID that is provided with every request for collection items to correlate the PlaceItem calls to the original request.")]
        [SerializeField]
        protected string requestId = "";

        [Tooltip("Enable this for placement strategies that depend on the order of child game objects instead of the index of the item.")]
        [SerializeField]
        protected bool keepGameObjectsInIndexOrder = true;

        [Tooltip("Enable this to advance full page count items even if this causes last page to be partially empty. ")]
        [SerializeField]
        protected bool lastPageCanBePartial =  true;

        protected int _firstVisibleItem = 0;
        protected int _numVisibleItems = 0;

        protected IDataConsumerCollection _dataConsumerCollection;

        protected enum State {
            Requested = 0,
            Visible,
            Prefetched,
            Removable,
            StashRemovable          // Temporarily stash removables that are still being fetched
        };


        protected class ItemInfo
        {
            public int itemIndex;
            public State state;
            public GameObject gameObject;

            public ItemInfo(int index, State theState, GameObject go)
            {
                itemIndex = index;
                state = theState;
                gameObject = go;
            }
        };


        protected Dictionary<State, Dictionary<int, ItemInfo>> _itemsByState = new Dictionary<State, Dictionary<int, ItemInfo>>();
        protected Dictionary<int, State> _itemStateByIndex = new Dictionary<int, State>();

        private void OnEnable()
        {
            foreach( State state in Enum.GetValues(typeof(State))) {
                _itemsByState[state] = new Dictionary<int, ItemInfo>();
            }
        }


        protected void QueueGameObjectsForRemoval(int firstItemIdx, int numItems)
        {
            int maxItem = firstItemIdx + numItems;
            for (int indexAsId = firstItemIdx; indexAsId < maxItem; indexAsId++)
            {
                ChangeItemState(indexAsId, State.Removable);
            }
        }

        protected ItemInfo FindItem( int indexAsId )
        {
            if (_itemStateByIndex.ContainsKey(indexAsId) )
            {
                State state = _itemStateByIndex[indexAsId];
                return _itemsByState[state][indexAsId];
            }

            return null;
        }


        protected void AddItem( State state, int indexAsId, GameObject go )
        {
            RemoveItem(indexAsId);

            _itemStateByIndex[indexAsId] = state;

            if (_itemsByState[state].ContainsKey(indexAsId) == false)
            {
                ItemInfo itemInfo = new ItemInfo(indexAsId, state, go);
                _itemsByState[state][indexAsId] = itemInfo;
            } 
            else
            {
                Debug.LogWarning("Item " + indexAsId + " is already in dictionary for CollectionItemPlacer.");
            }
        }

        protected void RemoveItem( int indexAsId)
        {
            if (_itemStateByIndex.ContainsKey(indexAsId))
            {
                State state = _itemStateByIndex[indexAsId];
                 
                _itemsByState[state].Remove(indexAsId);
                _itemStateByIndex.Remove(indexAsId);
            }
        }

        protected void UpdateItem( int indexAsId, State newState, GameObject newGO )
        {
            ItemInfo itemInfo = FindItem(indexAsId);
            if ( itemInfo != null)
            {
                itemInfo.gameObject = newGO;
                ChangeItemState(indexAsId, newState);
            }

        }

        protected void ChangeItemState( int indexAsId, State newState )
        {
            if (_itemStateByIndex.ContainsKey(indexAsId))
            {
                State oldState = _itemStateByIndex[indexAsId];
                ItemInfo itemInfo = _itemsByState[oldState][indexAsId];

                itemInfo.state = newState;
                _itemsByState[oldState].Remove(indexAsId);
                _itemsByState[newState][indexAsId] = itemInfo;

                _itemStateByIndex[indexAsId] = newState;
            }

        }

        /// <summary>
        /// Purge all game objects that have been queued for removal.
        /// </summary>
        /// <remarks>
        /// Normally items are removed as soon as they have been scrolled out of visibility, but
        /// to allow for transition effects, that default behavior for all scroll related methods
        /// can be delayed and the items can be manually purged with this method.
        /// 
        /// NOTE: if objects are not purged, they will indefinitely be referenced by this item placer,
        /// creating an effective memory leak.
        /// 
        /// This is useful for purging all previously visible items, such as
        /// after a transition effect, such as fade out, is done.
        /// </remarks>
        /// <param name="firstItemIdx">Index into collection of first item to purge.</param>
        /// <param name="itemCount">Number of items to purge.</param>
        public void PurgeAllRemovableGameObjects()
        {
            if ( _itemsByState.ContainsKey(State.Removable) )
            {
                Dictionary<int, ItemInfo> removableItems = _itemsByState[State.Removable];

                foreach (KeyValuePair<int, ItemInfo> entry in removableItems)
                {
                    ItemInfo itemInfo = entry.Value;
                    if (StashOrReturnItem(itemInfo))
                    {
                        _itemStateByIndex.Remove(itemInfo.itemIndex);
                    }
                }

                removableItems.Clear();
            }
        }

         /// <summary>
        /// Purge a range of game objects that have been queued for removal.
        /// </summary>
        /// <remarks>
        /// Normally items are removed as soon as they have been scrolled out of visibility, but
        /// to allow for transition effects, that default behavior for all scroll related methods
        /// can be delayed and the items can be manually purged with this method.
        /// 
        /// NOTE: if objects are not purged, they will indefinitely be referenced by this item placer,
        /// creating an effective memory leak.
        /// 
        /// This is useful for purging a series of previously visible items, such as
        /// after a transition effect, such as fade out, is done.
        /// </remarks>
        /// <param name="firstItemIdx">Index into collection of first item to purge.</param>
        /// <param name="itemCount">Number of items to purge.</param>
        public void PurgeRemovableGameObjectRange(int firstItemIdx, int itemCount)
        {
            if ( _itemsByState.ContainsKey(State.Removable))
            {
                int maxItemIdx = firstItemIdx + itemCount;
                Dictionary<int, ItemInfo> removableStateItems = _itemsByState[State.Removable];

                for (int idx = firstItemIdx; idx < maxItemIdx; idx++)
                {
                    if (removableStateItems.ContainsKey(idx))
                    {
                        ItemInfo itemInfo = removableStateItems[idx];

                        if (StashOrReturnItem(itemInfo))
                        {
                            RemoveItem(idx);
                        }
                    }
                }
            }

        }

        protected bool StashOrReturnItem(ItemInfo itemInfo)
        {
            if (itemInfo.state == State.Requested || itemInfo.gameObject == null)
            {
                ChangeItemState(itemInfo.itemIndex, State.StashRemovable);
                return false;
            }
            else
            {
                _dataConsumerCollection.ReturnGameObjectForReuse(itemInfo.itemIndex, itemInfo.gameObject);
                return true;
            }
        }



        /// <summary>
        /// Set the Data Consumer that is providing items for this item placer.
        /// </summary>
        /// <param name="removeExitingObjectsNow">Release objects going out of visibility back to object pool immediately.</param>
        /// <returns>Actual number of items scrolled.</returns>
        public void SetDataConsumerCollection(IDataConsumerCollection dataConsumerCollection)
        {
            _dataConsumerCollection = dataConsumerCollection;
        }


        /// <summary>
        /// Scroll forward one page if possible, or to end of list if partial page.
        /// </summary>
        public void ScrollNextPage()
        {
            Scroll(GetMaxVisibleItemCount());
        }

        /// <summary>
        /// Scroll back one page if possible, or to beginning if partial page.
        /// </summary>
        public void ScrollPreviousPage()
        {
            Scroll(-GetMaxVisibleItemCount());
        }


        /// <summary>
        /// Scroll forward one item if possible.
        /// </summary>
        public void ScrollNextItem()
        {
            Scroll(1);
        }

        /// <summary>
        /// Scroll back one item if possible.
        /// </summary>
        public void ScrollPreviousItem()
        {
            Scroll(-1);
        }



        /// <summary>
        /// Scroll by itemCount items forward or backward
        /// </summary>
        /// <remarks>
        /// Note if objects are not removed immediately, they must be removed later (such as after a transition effect)
        /// using PurgeAllRemovableGameObjects() or PurgeRemovableGameObjectRange()</remarks>
        /// <param name="itemCount">THe number of items to scroll. Negative=previous. Positive=next.</param>
        /// <param name="removeExitingObjectsNow">Remove no longer visible objects immediately and return back to object pool.</param>
        /// <returns>Actual number of items scrolled. Note: Always positive for previous or next.</returns>
        public int Scroll(int itemCount, bool removeExitingObjectsNow = true)
        {
            int actualScrollAmount;
            int firstItemToRequest;
            int numItemsToRequest;
            int newFirstVisibleItem = _firstVisibleItem;
            int firstItemToRemove;
            int firstItemToReposition;
            int numItemsToReposition;


            if (itemCount < 0)
            {
                // scroll previous

                actualScrollAmount = Math.Min(-itemCount, _firstVisibleItem);
                newFirstVisibleItem -= actualScrollAmount;
                firstItemToRequest = _firstVisibleItem - actualScrollAmount;
                numItemsToRequest = actualScrollAmount;
                firstItemToRemove = _firstVisibleItem + _numVisibleItems - actualScrollAmount;

                firstItemToReposition = _firstVisibleItem;
                numItemsToReposition = Math.Max(0, _numVisibleItems - actualScrollAmount);
            } 
            else
            {
                // scroll next

                int maxFirstVisibleItem;
                int totalItemCount = GetTotalItemCount();

                if (lastPageCanBePartial)
                {
                    maxFirstVisibleItem = totalItemCount - (totalItemCount % GetItemCountPerPage());
                }
                else
                {
                    maxFirstVisibleItem = totalItemCount - GetMaxVisibleItemCount();
                }

                actualScrollAmount = Math.Min(itemCount, maxFirstVisibleItem - _firstVisibleItem);

                firstItemToRemove = _firstVisibleItem;
                firstItemToRequest = _firstVisibleItem + Math.Max(_numVisibleItems, itemCount);
                newFirstVisibleItem += actualScrollAmount;

                numItemsToRequest = Math.Min(actualScrollAmount, totalItemCount - newFirstVisibleItem);
                firstItemToReposition = _firstVisibleItem + actualScrollAmount;
                numItemsToReposition = Math.Max(0, _numVisibleItems - actualScrollAmount);
            }

            if (actualScrollAmount > 0)
            {
                QueueGameObjectsForRemoval(firstItemToRemove, actualScrollAmount);
                _firstVisibleItem = newFirstVisibleItem;
                RequestItems( firstItemToRequest, numItemsToRequest);

                if (removeExitingObjectsNow )
                {
                    PurgeRemovableGameObjectRange(firstItemToRemove, actualScrollAmount);
                }


                // for a partial scroll, we need to reposition the ones that are still visible, but are now
                // potentially in a new location
                if (numItemsToReposition > 0)
                {
                    RepositionItems(firstItemToReposition, numItemsToReposition);
                }
            }


            return actualScrollAmount;
        }


        /// <summary>
        /// Are visible items at beginning of list
        /// </summary>
        /// <remarks>
        /// If at start of list, then scroll / page previous option is not available.</remarks>
        /// <returns></returns>
        public bool IsAtStart()
        {
            return _firstVisibleItem == 0;
        }


        /// <summary>
        /// Are visible items at the end of list
        /// </summary>
        /// <remarks>
        /// If at end of list, then scroll / page next option is not available.
        /// </remarks>
        /// <returns></returns>
        public bool IsAtEnd()
        {
            return _firstVisibleItem >= GetTotalItemCount() - GetMaxVisibleItemCount();
        }



        /// <summary>
        /// Get the list index of the first visible item
        /// </summary>
        /// <remarks>
        /// Note that this is the current logical start of visible items, but not all of the
        /// data may have been received yet.
        /// </remarks>
        /// <returns>Index number of first logically visible item.</returns>
        public int GetFirstVisibleItem()
        {
            return _firstVisibleItem;
        }

        /// <summary>
        /// Get current page number for first visible item.
        /// </summary>
        /// <returns>The page number (zero based)</returns>
        public int GetPageNumber()
        {
            return _firstVisibleItem / GetItemCountPerPage();
        }


        /// <summary>
        /// Get nominal number of items per logical page.
        /// </summary>
        /// <remarks>
        /// Note that the actual number if items may depend on the nature of the
        /// items (such as the mix of object sizes) or whether fewer items are available than
        /// can be shown.
        /// </remarks>
        /// <returns>The number if items per page.</returns>
        public virtual int GetItemCountPerPage()
        {
            return maximumVisibleItems;
        }

        /// <summary>
        /// Get the total number of items in the collection.
        /// </summary>
        /// <returns>Number of items in the collection.</returns>
        public virtual int GetTotalItemCount()
        {
            return _dataConsumerCollection?.GetCollectionItemCount() ?? 0;
        }


        #region IDataCollectionItemPlacer method implementations

        public virtual void StartPlacement() {
        }

        public virtual void PlaceItem(string requestId, int indexRangeStart, int indexRangeCount, int itemIndex, GameObject itemGO)
        {
            ItemInfo itemInfo = FindItem(itemIndex);

            if (keepGameObjectsInIndexOrder)
            {
                itemGO.transform.SetSiblingIndex(itemIndex - _firstVisibleItem);
            }

            State currentState;

            if (itemIndex >= _firstVisibleItem && itemIndex < _firstVisibleItem + GetMaxVisibleItemCount())
            {
                currentState = State.Visible;
            }
            else if (itemInfo.state == State.StashRemovable || itemInfo.state == State.Removable)
            {
                currentState = State.Removable;
            }
            else
            {
                currentState = State.Prefetched;
            }

            UpdateItem(itemIndex, currentState, itemGO);

            if (currentState == State.Visible) {
                PlaceVisibleItem( requestId, indexRangeStart, indexRangeCount, itemIndex, itemGO);
                itemGO.SetActive(true);
            }
            else if (currentState == State.Removable)
            {
                PurgeRemovableGameObjectRange(itemIndex, 1);
            }
        }


        protected virtual void RepositionItems(int firstIndexToReposition, int numItems )
        {
            Dictionary<int, ItemInfo> visibleStateItems = _itemsByState[State.Visible];
            for( int itemIndex = firstIndexToReposition; itemIndex < firstIndexToReposition + numItems; itemIndex++ )
            {
                if ( visibleStateItems.ContainsKey(itemIndex) )
                {
                    PlaceVisibleItem(requestId, firstIndexToReposition, numItems, itemIndex, visibleStateItems[itemIndex].gameObject);
                }
                else
                {
                    Debug.LogWarning("Item to reposition is not marked visible. Item #" + itemIndex);
                }

            }
        }


        public virtual void EndPlacement() {
            PredictivelyLoadItems();
        }



        public virtual void NotifyCollectionDataChanged(DataChangeType dataChangeType) {
            // default behavior is to ask for all items in the collection with empty string as request ID.
            if (dataChangeType == DataChangeType.CollectionItemAdded)
            {
                if (_numVisibleItems < GetMaxVisibleItemCount())
                {
                    // If adding items and num visible is not currently the maximum possible visible, then
                    // let's add the new item to what's visible.
                    int firstItem = _firstVisibleItem + _numVisibleItems;
                    int numItems = GetMaxVisibleItemCount() - _numVisibleItems;
                    _numVisibleItems += numItems;

                    RequestItems(firstItem, numItems);
                }
            }
            else
            {
                // For misc mods or deletions, it's safer to just re-fetch all at current scroll position since we don't
                // necessarily know where a deletion or modification occured.
                RequestVisibleItems();
            }
        }

        #endregion IDataCollectionItemPlacer method implementations

        #region Methods typically overridden

        /// <summary>
        /// Place a visible item into the experience
        /// </summary>
        /// <remarks>
        /// Except in situations where the mere addition of a game object to the collection invokes another
        /// script to manage placement, this class will normally be overridden in a subclass to actually do
        /// the insertion of the specified game object into the scene at the correct transform.  
        /// 
        /// The index range is provided in case this is useful for determining the relative location of this item
        /// to the total number of items requested.  Note that this is the requested items, which generally is not
        /// the entire collection.
        /// </remarks>
        /// <param name="requestId">Request ID provided at the time of the request.</param>
        /// <param name="indexRangeStart">The start of the index range of items requested in this batch.</param>
        /// <param name="indexRangeCount">The number of items requested in this batch</param>
        /// <param name="itemIndex">The index of this item.</param>
        /// <param name="itemGO">The game object (usually a prefab) of the item to place.</param>
        public virtual void PlaceVisibleItem(string requestId, int indexRangeStart, int indexRangeCount, int itemIndex, GameObject itemGO) 
        {         
        }

        #endregion Methods typically overridden


        protected void RequestVisibleItems()
        {
            _numVisibleItems = Math.Min(GetMaxVisibleItemCount(), GetTotalItemCount() - _firstVisibleItem);

            if (_numVisibleItems > 0)
            {
                RequestItems( _firstVisibleItem, _numVisibleItems);
            }
        }


        protected void RequestItems( int firstIdx, int count )
        {
            for(int idx = firstIdx; idx < firstIdx + count; idx++ )
            {
                AddItem(State.Requested, idx, null);
            }

            _dataConsumerCollection?.RequestCollectionItems(this, firstIdx, count, requestId);
        }



        protected virtual void PredictivelyLoadItems()
        {
        }


        protected virtual int GetMaxVisibleItemCount()
        {
            // override this to return the actual currently visible item count
            return Math.Min(GetTotalItemCount(), GetItemCountPerPage());
        }
    }
}
