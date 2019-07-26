﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit;
#if SCENE_TRANSITIONS_ENABLED
using Microsoft.MixedReality.Toolkit.Extensions.SceneTransitions;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Utility class to load scenes through MRTK Scene System. If Scene Transition Service is enabled, it uses it.
    /// Otherwise, it uses Scene System's LoadContent()
    /// </summary>
    public class LoadContentScene : MonoBehaviour
	{
        [SerializeField]
        private LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        [SerializeField]
        private string contentName = null;
        [SerializeField]
        private bool loadOnStartup = false;

        private void Start()
        {
            if (loadOnStartup)
            {
                LoadContent();
            }
        }

        public void LoadContent()
		{
			#if SCENE_TRANSITIONS_ENABLED
			ISceneTransitionService transitions = MixedRealityToolkit.Instance.GetService<ISceneTransitionService>();
			if (transitions.TransitionInProgress)
				return;

			transitions.DoSceneTransition(() => MixedRealityToolkit.SceneSystem.LoadContent(contentName, loadSceneMode));
			#else
			MixedRealityToolkit.SceneSystem.LoadContent(contentName, loadSceneMode);
			#endif
		}
	}
}