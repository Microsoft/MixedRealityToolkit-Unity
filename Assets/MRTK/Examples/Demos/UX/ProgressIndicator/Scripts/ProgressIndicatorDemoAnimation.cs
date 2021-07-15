// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class ProgressIndicatorDemoAnimation : MonoBehaviour, IProgressIndicatorDemoObject
    {
        const string animationName = "PIDemoAnimationPlay";

        [SerializeField]
        private GameObject progressIndicatorObject;
        [SerializeField]
        private Animator animator;

        private IProgressIndicator progressIndicator;

        private void Awake()
        {
            progressIndicator = progressIndicatorObject.GetComponent<IProgressIndicator>();
        }

        public async void StartProgressBehavior()
        {
            if (progressIndicator.State != ProgressIndicatorState.Closed)
            {
                Debug.LogWarning("Can't start progress behavior in this state.");
                return;
            }

            progressIndicator.Message = $"Starting animation...";
            progressIndicator.Progress = 0;
            await progressIndicator.OpenAsync();

            animator.SetTrigger("PlayAnimation");

            await Task.Yield();

            // Wait for animation to START playing
            bool playingAnimation = false;
            while (!playingAnimation)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                playingAnimation = stateInfo.IsName(animationName);

                progressIndicator.Message = $"Waiting for animation to start...";
                await Task.Yield();
            }

            // Wait for animation to STOP playing
            while (playingAnimation)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                playingAnimation = stateInfo.IsName(animationName);

                if (playingAnimation)
                {
                    progressIndicator.Message = $"Waiting for animation to finish...";
                    progressIndicator.Progress = stateInfo.normalizedTime;
                }
                await Task.Yield();
            }

            progressIndicator.Progress = 1;
            progressIndicator.Message = $"Finished with animation...";

            await progressIndicator.CloseAsync();
        }
    }
}