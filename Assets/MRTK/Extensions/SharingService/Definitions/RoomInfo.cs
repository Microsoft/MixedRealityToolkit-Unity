﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.Extensions.Sharing
{
    /// <summary>
    /// Struct used to define a room available in the lobby.
    /// </summary>
    public struct RoomInfo
    {
        /// <summary>
        /// The name of the room.
        /// </summary>
        public string Name;
        /// <summary>
        /// The number of devices currently in a room.
        /// </summary>
        public short NumDevices;
        /// <summary>
        /// The max number of devices that can enter the room.
        /// </summary>
        public short MaxDevices;
        /// <summary>
        /// True if this room can be joined.
        /// </summary>
        public bool IsOpen;
        /// <summary>
        /// Set of custom properties that are visible in the lobby.
        /// </summary>
        public IEnumerable<string> LobbyProps;
        /// <summary>
        /// Set of custom properties that are visible in the room. Will be empty until room is joined.
        /// </summary>
        public IEnumerable<RoomProp> RoomProps;
    }
}