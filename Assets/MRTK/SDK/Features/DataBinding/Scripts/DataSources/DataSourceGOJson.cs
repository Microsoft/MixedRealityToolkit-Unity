// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data source Monobehaviour that manages a structured data set that was provided
    /// by a JSON text stream. 
    /// 
    /// The JSON provided to this call will typically be the result of a RESTful 
    /// call to a back-end service.  That JSON can be provided directly to this
    /// data source and will trigger changes to any data consumers listening for
    /// data changes to this source.
    /// 
    /// TODO: Compare deltas between JSON fetches to reduce the number of
    ///       data consumer notifications to only this keypaths that have actually
    ///       changed since the last data fetch.
    /// </summary>
    /// 

    public class DataSourceGOJsonBase : DataSourceGOBase
    {
        public delegate void RequestSuccessDelegate(string jsonText, string requestId);
        public delegate void RequestFailureDelegate(string errorString, string requestId);

        public DataSourceJson DataSource { get { return _dataSource as DataSourceJson; } }

        protected Regex _callbackRegex = new Regex(  @"^([a-zA-Z0-9_]+)\(" );


        public override IDataSource AllocateDataSource()
        {
            {
                if (_dataSource == null)
                {
                    _dataSource = new DataSourceJson();
                }

                return _dataSource;
            }
        }


        public IEnumerator StartJsonRequest(string uri, RequestSuccessDelegate successDelegate = null, RequestFailureDelegate failureDelegate = null, string requestId = null)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.isHttpError || webRequest.isNetworkError)
                {
                    if (failureDelegate != null)
                    {
                        failureDelegate?.Invoke(webRequest.error, requestId);
                    }
                }
                else
                {
                    string jsonText = RemoveCallbackWrapper(webRequest.downloadHandler.text);

                    DataSource.UpdateFromJson(jsonText);
                    if (successDelegate != null)
                    {
                        successDelegate?.Invoke(jsonText, requestId);
                    }
                }
            }
        }

        private string RemoveCallbackWrapper(string jsonText)
        {
            // Quick check to find a trailing parenthesis to validate that it is
            // wrapped in a callback and then also used for trimming that parens off.

            int lastCharToIncludeIdx;

            for(lastCharToIncludeIdx = jsonText.Length - 1; lastCharToIncludeIdx >= 0; lastCharToIncludeIdx-- )
            {
                char lastChar = jsonText[lastCharToIncludeIdx];
                if (!char.IsWhiteSpace(lastChar))
                {
                    if (lastChar == ')')
                    {
                        MatchCollection matches = _callbackRegex.Matches(jsonText);

                        if (matches.Count == 1)
                        {
                            string callbackName = matches[0].Groups[1].Value;
                            int remainingCharCount = lastCharToIncludeIdx - callbackName.Length - 1; //1 for open parens
                            return jsonText.Substring(callbackName.Length + 1, remainingCharCount); //also remove parens
                        }
                    } else
                    {
                        break;
                    }
                }                  
            }
            return jsonText;
        }

    }
}

