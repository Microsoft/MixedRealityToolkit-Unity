// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A simple data collection item placer that will place each item at a specific
    /// offset from the previous object, first in the x, then y, then z directions, 
    /// using the offsets provided in the inspector.  The starting point is reset
    /// each time a new placement session is started using StartPlacement().
    /// 
    /// </summary>
    public class DataCollectionItemPlacerOffset : DataCollectionItemPlacerGOBase
    {


        [Tooltip("Place each item in a collection at successive offsets relative to parent gameobject, with the first item spawning at 0,0,0.")]
        [SerializeField]
        protected Vector3 itemOffset;

        [Tooltip("How many items to show in the x dimension using the x item offset.")]
        [SerializeField]
        protected int xCount = 4;

        [Tooltip("How many items to show in the y dimension using the y item offset.")]
        [SerializeField]
        protected int yCount = 3;

        [Tooltip("How many items to show in the y dimension using the y item offset.")]
        [SerializeField]
        protected int zCount = 1;

        protected Vector3 _itemPlacerPositionOffset;



        public override void StartPlacement()
        {
            base.StartPlacement();
            _itemPlacerPositionOffset = new Vector3(0, 0, 0);
        }

        public override void PlaceVisibleItem( string requestId, int indexRangeStart, int indexRangeCount, int itemIndex, GameObject itemGO)
        {
            itemIndex -= GetFirstVisibleItem();

            _itemPlacerPositionOffset.x = itemOffset.x * (itemIndex % xCount);
            _itemPlacerPositionOffset.y = itemOffset.y * ((itemIndex / xCount) % yCount);
            _itemPlacerPositionOffset.z = itemOffset.z * (itemIndex / (xCount * yCount));

            // When items are reused from the object pool, it's important to update in a way that does not
            // result in cumulative additive offsets. To ensure this, it uses parent's position, but this
            // does assume this prefab defaults to the correct local offset relative to the parent container.

            itemGO.transform.position = itemGO.transform.parent.transform.position + _itemPlacerPositionOffset;
        }


        public override int GetItemCountPerPage()
        {
            return xCount * yCount * zCount;
        }



    }
}
