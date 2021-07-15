// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Demo class for IProgressIndicator examples
    /// </summary>
    public class ProgressIndicatorDemo : MonoBehaviour
    {
        [SerializeField, Header("Demo objects")]
        private GameObject demoObjectAsyncMethod = null;
        [SerializeField]
        private GameObject demoObjectAnimation = null;
        [SerializeField]
        private GameObject demoObjectSceneLoad = null;

        [SerializeField, Header("Editor Keyboard Controls")]
        private KeyCode toggleBarAsyncMethodKey = KeyCode.Alpha1;
        [SerializeField]
        private KeyCode toggleAnimationKey = KeyCode.Alpha2;
        [SerializeField]
        private KeyCode toggleSceneLoadKey = KeyCode.Alpha3;

        /// <summary>
        /// Target method for demo button
        /// </summary>
        public void OnClickAsyncMethod()
        {
            HandleButtonClick(demoObjectAsyncMethod.GetComponent<IProgressIndicatorDemoObject>());
        }

        /// <summary>
        /// Target method for demo button
        /// </summary>
        public void OnClickAnimation()
        {
            HandleButtonClick(demoObjectAnimation.GetComponent<IProgressIndicatorDemoObject>());
        }

        /// <summary>
        /// Target method for demo button
        /// </summary>
        public void OnClickSceneLoad()
        {
            HandleButtonClick(demoObjectSceneLoad.GetComponent<IProgressIndicatorDemoObject>());
        }

        private void HandleButtonClick(IProgressIndicatorDemoObject demoObject)
        {
            demoObject.StartProgressBehavior();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleBarAsyncMethodKey))
            {
                HandleButtonClick(demoObjectAsyncMethod.GetComponent<IProgressIndicatorDemoObject>());
            }

            if (UnityEngine.Input.GetKeyDown(toggleAnimationKey))
            {
                HandleButtonClick(demoObjectAnimation.GetComponent<IProgressIndicatorDemoObject>());
            }

            if (UnityEngine.Input.GetKeyDown(toggleSceneLoadKey))
            {
                HandleButtonClick(demoObjectSceneLoad.GetComponent<IProgressIndicatorDemoObject>());
            }
        }
    }
}