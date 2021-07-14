// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// An abstract data consumer that will fetch any supported image type from a URL
    /// and create a Texture.
    /// </summary>
    /// <remarks>
    /// This can be used as a base implementation for updating sprites, quads or any other element that
    /// expects an image texture
    /// 
    /// There are 2 methods to implement:
    /// 
    ///     protected abstract void InitializeForComponent(Type componentType, Component component);
    ///     protected abstract void PlaceImageTexture(Texture imageTexture);
    /// </remarks>
    /// 


    /// </summary>

    [Serializable]
    public abstract class DataConsumerImageTextureFromUrl : DataConsumerGOBase
    {
        [Tooltip("Manage images in child game objects as well as this one.")]
        [SerializeField] private bool manageChildren = true;

        [Tooltip("Key path of where to retrieve image URL within the data source data set.")]
        [SerializeField] private string keyPath = "imageUrl";

        [Tooltip("(Optional) Random load delay to load balance when multiple sprites are requested at same time, such as during the instantiation of an entire list.")]
        [SerializeField] private int maxRandomLoadBalancingDelayInMilliseconds = 0;


        protected bool _fetchInProgress = false;
        protected string _waitingUrlToFetch = null;
        protected int _frameDelayCountdown = 0;
        protected System.Random _random;
        private IEnumerator _coroutine;

        protected readonly float FramesPerMillisecond = 60.0f / 1000.0f; // 60 frames per 1000 milliscends

        void Update()
        {
            CheckForWaitingUrlToFetch();

        }

        private void OnDisable()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _fetchInProgress = false;
            _waitingUrlToFetch = null;
        }


        protected override bool ManageChildren()
        {
            return manageChildren;
        }

        protected override void InitializeDataConsumer()
        {
            _random = new System.Random();
        }

        protected override void AddVariableKeyPathsForComponent(Type componentType, Component component)
        {
            InitializeForComponent(componentType, component);

            AddKeyPathListener(keyPath);
        }


        protected abstract void InitializeForComponent(Type componentType, Component component);
        protected abstract void PlaceImageTexture(Texture2D imageTexture);


        protected override void ProcessDataChanged(IDataSource dataSource, string resolvedKeyPath, string localKeyPath, object newValue, DataChangeType dataChangeType)
        {
            // Debug.Log("ProcessDataChanged " + resolvedKeyPath + ",  local: " + localKeyPath);

            if (localKeyPath == keyPath)
            {
                string newUrl = newValue.ToString();
                _frameDelayCountdown = (int)((float)maxRandomLoadBalancingDelayInMilliseconds * FramesPerMillisecond);
                _frameDelayCountdown = _random.Next(0, _frameDelayCountdown);

                if (newUrl != null)
                {
                    if (_fetchInProgress || _frameDelayCountdown > 0)
                    {
                        _waitingUrlToFetch = newUrl;
                    }
                    else
                    {
                        _fetchInProgress = true;
                        _coroutine = FetchImageTexture(newUrl);
                        StartCoroutine(_coroutine);
                    }
                }
            }
        }

        protected void CheckForWaitingUrlToFetch()
        {
            if (_waitingUrlToFetch != null && _fetchInProgress == false && --_frameDelayCountdown <= 0)
            {
                _fetchInProgress = true;
                _coroutine = FetchImageTexture(_waitingUrlToFetch);
                StartCoroutine(_coroutine);
                _waitingUrlToFetch = null;
            }
        }


        protected IEnumerator FetchImageTexture(string uri)
        {
            bool doneWithSuccess = false;
            const int kMaxRetries = 3;

            // UnityEngine.Debug.Log("SendWebRequest: " + uri);
            for (int retries = 0; !doneWithSuccess && retries < kMaxRetries; retries++)
            {
                using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri))
                {
                    yield return webRequest.SendWebRequest();

                    if (webRequest.isHttpError || webRequest.isNetworkError)
                    {
                        UnityEngine.Debug.LogError("SendWebRequest error: " + webRequest.error + " for URL " + uri );
                    }
                    else
                    {
#if UNITY_2019_1_OR_NEWER
                        bool saveAllowThreaded = Texture.allowThreadedTextureCreation;
                        Texture.allowThreadedTextureCreation = true;
#endif
                        Texture2D imageTexture = DownloadHandlerTexture.GetContent(webRequest);

                        PlaceImageTexture(imageTexture);

#if UNITY_2019_1_OR_NEWER
                        Texture.allowThreadedTextureCreation = saveAllowThreaded;
#endif
                        doneWithSuccess = true;
                    }
                    _fetchInProgress = false;
                }
            }
            _coroutine = null;
        }

    }
}
