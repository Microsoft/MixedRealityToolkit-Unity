// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// A simple data source that fetches JSON data from a specified URL every N seconds.
    ///
    /// This will trigger notification changes by the base class once the data has been
    /// parsed.
    /// </summary>
    public class DataSourceJsonTest : DataSourceGOJsonBase
    {
        [Tooltip("URL for a json data source")]
        [SerializeField]
        private string url = "https://official-joke-api.appspot.com/rando_joke";

        [Tooltip("How many seconds between fetching the data source and notifying all consumer of changes.")]
        [SerializeField]
        private float secondsBetweenFetches = 15.0f;


        protected float _time = 0.0f;


        protected override void InitializeDataSource()
        {
            _time = secondsBetweenFetches;
        }

        // Update is called once per frame
        void Update()
        {
            _time += Time.deltaTime;

            if (_time >= secondsBetweenFetches )
            {
                _time -= secondsBetweenFetches;

                StartCoroutine(StartJsonRequest(url));
            }
        }
    }
}

