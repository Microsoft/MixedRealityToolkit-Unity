// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Displays progress bar and (optionally) percentage text.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Scripts/MRTK/SDK/ProgressIndicatorLoadingBar")]
    public class ProgressIndicatorLoadingBar : MonoBehaviour, IProgressIndicator
    {
        const float SmoothProgressSpeed = 0.25f;

        /// <inheritdoc/>
        public Transform MainTransform { get { return transform; } }

        /// <inheritdoc/>
        public ProgressIndicatorState State { get { return state; } }

        /// <inheritdoc/>
        public float Progress { set { targetProgress = value; } }

        /// <inheritdoc/>
        public string Message { set { messageText.text = value; } }

        // The animated progress bar object
        [SerializeField]
        private Transform progressBar = null;

        // The message text used by the 'Visible' message style
        [SerializeField]
        private TextMeshPro messageText = null;

        // The progress text used by all non-'None' progress styles
        [SerializeField]
        private TextMeshPro progressText = null;

        [Tooltip("Format to use when converting progress number to string.")]
        [SerializeField]
        private string progressStringFormat = "{0:P0}";

        [Tooltip("Whether to display the percentage as text in addition to the loading bar.")]
        [SerializeField]
        private bool displayPercentage = true;

        private float smoothProgress = 0f;
        private float lastSmoothProgress = -1;
        private ProgressIndicatorState state = ProgressIndicatorState.Closed;
        private Vector3 barScale = Vector3.one;

        /// <summary>
        /// Target for smooth progress, 0-1. Serialized to test in editor.
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float targetProgress = 0f;

        /// <inheritdoc/>
        public async Task OpenAsync()
        {
            if (state != ProgressIndicatorState.Closed)
            {
                throw new System.Exception("Can't open in state " + state);
            }

            smoothProgress = 0;
            lastSmoothProgress = 0;
            progressText.text = string.Format(progressStringFormat, smoothProgress);

            gameObject.SetActive(true);

            state = ProgressIndicatorState.Opening;

            await Task.Yield();

            state = ProgressIndicatorState.Open;
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            if (state != ProgressIndicatorState.Open)
            {
                throw new System.Exception("Can't close in state " + state);
            }

            state = ProgressIndicatorState.Closing;

            await Task.Yield();

            state = ProgressIndicatorState.Closed;

            gameObject.SetActive(false);
        }

        /// <inheritdoc/>
        public async Task AwaitTransitionAsync()
        {
            while (isActiveAndEnabled)
            {
                switch (state)
                {
                    case ProgressIndicatorState.Open:
                    case ProgressIndicatorState.Closed:
                        return;

                    default:
                        break;
                }

                await Task.Yield();
            }
        }

        private void Update()
        {
            if (state != ProgressIndicatorState.Open)
            {
                return;
            }

            smoothProgress = Mathf.Lerp(smoothProgress, targetProgress, SmoothProgressSpeed);

            if (smoothProgress > 0.99f)
            {
                smoothProgress = 1f;
            }

            barScale.x = Mathf.Clamp(smoothProgress, 0.01f, 1f);
            progressBar.localScale = barScale;

            progressText.gameObject.SetActive(displayPercentage);

            if (lastSmoothProgress != smoothProgress)
            {
                progressText.text = string.Format(progressStringFormat, smoothProgress);
                lastSmoothProgress = smoothProgress;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
