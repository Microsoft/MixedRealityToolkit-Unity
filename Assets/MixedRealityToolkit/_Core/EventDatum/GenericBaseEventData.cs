﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Internal.Interfaces.Events;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Internal.EventDatum
{
    /// <summary>
    /// Generic Base Event Data for Sending Events through the the Event System.
    /// </summary>
    public class GenericBaseEventData : BaseEventData
    {
        /// <summary>
        /// The Event Source that the event originates from.
        /// </summary>
        public IMixedRealityEventSource EventSource { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventSystem">Usually <see cref="EventSystem.current"/></param>
        public GenericBaseEventData(EventSystem eventSystem) : base(eventSystem) { }

        /// <summary>
        /// Used to initialize/reset the event and populate the data.
        /// </summary>
        /// <param name="eventSource">The source of the event.</param>
        protected void BaseInitialize(IMixedRealityEventSource eventSource)
        {
            Reset();
            EventSource = eventSource;
        }
    }
}