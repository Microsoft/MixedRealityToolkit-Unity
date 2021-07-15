// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Demo class for IProgressIndicator examples.
    /// Use a progress indicator to show when an async task has completed.
    /// </summary>
    public class ProgressIndicatorDemoAsyncMethod : MonoBehaviour, IProgressIndicatorDemoObject
    {
        [SerializeField]
        private GameObject progressIndicatorObject;
        [SerializeField, Range(0.5f, 10f)]
        private float asyncTaskDuration = 5f;
        [SerializeField]
        private AnimationCurve coffeeCurve;
        [SerializeField]
        private Transform coffeeTransform;

        private IProgressIndicator progressIndicator;
        private CancellationTokenSource tokenSource;

        private void Awake()
        {
            progressIndicator = progressIndicatorObject.GetComponent<IProgressIndicator>();
        }

        private void OnDisable()
        {
            tokenSource?.Cancel();
            tokenSource = null;
        }

        public async void StartProgressBehavior()
        {
            if (tokenSource != null)
            {
                Debug.LogWarning("Can't start progress behavior in this state.");
                return;
            }

            progressIndicator.Message = "Opening...";
            await progressIndicator.OpenAsync();

            tokenSource = new CancellationTokenSource();
            Task asyncMethod = AsyncMethod(tokenSource.Token);

            progressIndicator.Message = "Waiting for async method to complete...";
            while (!asyncMethod.IsCompleted)
            {
                await Task.Yield();
            }
            tokenSource = null;

            progressIndicator.Message = "Closing...";
            await progressIndicator.CloseAsync();
        }

        private async Task AsyncMethod(CancellationToken token)
        {
            float timeStarted = Time.time;
            Vector3 startPos = coffeeTransform.localPosition;
            while (Time.time < timeStarted + asyncTaskDuration)
            {
                float normalizedTime = Mathf.Clamp01((Time.time - timeStarted) / asyncTaskDuration);
                float eval = coffeeCurve.Evaluate(normalizedTime);
                coffeeTransform.localPosition = startPos + (Vector3.down * coffeeCurve.Evaluate(normalizedTime));
                await Task.Yield();
            }
            coffeeTransform.localPosition = startPos;
        }
    }
}