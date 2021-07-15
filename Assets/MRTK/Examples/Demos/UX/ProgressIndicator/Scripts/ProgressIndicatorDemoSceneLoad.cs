// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.SceneSystem;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Demo class for IProgressIndicator examples.
    /// Use a progress indicator to show scene load / unload operation progress.
    /// Please ensure sceneToLoad has been added to build settings.
    /// </summary>
    public class ProgressIndicatorDemoSceneLoad : MonoBehaviour, IProgressIndicatorDemoObject
    {
        [SerializeField]
        private GameObject progressIndicatorObject;
        [SerializeField]
        private SceneInfo sceneToLoad;
        [SerializeField, Range(0f, 5f)]
        private float loadDelay = 2.5f;

        private bool startedProgressBehavior = false;

        public async void StartProgressBehavior()
        {
            if (startedProgressBehavior)
            {
                Debug.Log("Can't start until behavior is completed.");
                return;
            }

            startedProgressBehavior = true;

            IProgressIndicator indicator = progressIndicatorObject.GetComponent<IProgressIndicator>();
            indicator.Message = "Preparing for scene operation...";
            await indicator.OpenAsync();

            Task sceneTask = null;
            string progressMessage = string.Empty;

            // A scene this small will load almost instantly, so we're doing a delay so the indicator is visible
            float timeStarted = Time.time;
            while (Time.time < timeStarted + loadDelay)
                await Task.Yield();

            if (CoreServices.SceneSystem.IsContentLoaded(sceneToLoad.Name))
            {
                sceneTask = CoreServices.SceneSystem.UnloadContent(sceneToLoad.Name);
                progressMessage = "Unloading scene {0}";
            }
            else
            {
                sceneTask = CoreServices.SceneSystem.LoadContent(sceneToLoad.Name, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                progressMessage = "Loading scene {0}";
            }

            indicator.Message = progressMessage;

            while (!sceneTask.IsCompleted)
            {
                indicator.Message = string.Format(progressMessage, CoreServices.SceneSystem.SceneOperationProgress * 100);
                await Task.Yield();
            }

            indicator.Message = "Finished operation";
            await indicator.CloseAsync();

            startedProgressBehavior = false;
        }
    }
}