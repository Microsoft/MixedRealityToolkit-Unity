﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;

#if UNITY_2017_2_OR_NEWER
using System.Collections;
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Microsoft.MixedReality.Toolkit
{
    /// <summary>
    /// Use this script on GameObjects you wish to be aligned in certain ways depending on the application space type.
    /// For example, if you want to place an object at the height of the user's head in a room scale application, check alignWithHeadHeight.
    /// In a stationary scale application, this is equivalent to placing the object at a height of 0.
    /// You can also specify specific locations to place the object based on space type.
    /// 
    /// This script runs once, on GameObject creation.
    /// 
    /// See https://developer.microsoft.com/en-us/windows/mixed-reality/coordinate_systems_in_unity for more information.
    /// <see cref="BoundaryManager"/> for TrackingSpaceType settings.
    /// </summary>
    public class MixedRealitySceneContent : MonoBehaviour
    {
        public enum AlignmentType
        {
            AlignWithExperienceScale,
            AlignWithHeadHeight
        }

        [SerializeField]
        [Tooltip("Select this if the container should be placed in front of the head on app launch in a room scale app.")]
        public AlignmentType alignmentType = AlignmentType.AlignWithExperienceScale;

        private Vector3 contentPosition = Vector3.zero;

        private int frameWaitHack = 0;

        [Tooltip("Optional container object reference. If null, this script will move the object it's attached to.")]
        private Transform containerObject = null;

        private void Awake()
        {

            if (containerObject == null)
            {
                containerObject = transform;
            }

             StartCoroutine(SetContentHeight());
        }

        private IEnumerator SetContentHeight()
        {
            if (frameWaitHack < 1)
            {
                // Not waiting a frame often caused the camera's position to be incorrect at this point. This seems like a Unity bug.
                frameWaitHack++;
                yield return null;
            }

            if (alignmentType == AlignmentType.AlignWithExperienceScale)
            {
                if(MixedRealityToolkit.Instance.ActiveProfile.ExperienceSettingsProfile.TargetExperienceScale == ExperienceScale.Room ||
                    MixedRealityToolkit.Instance.ActiveProfile.ExperienceSettingsProfile.TargetExperienceScale == ExperienceScale.World)
                {
                    contentPosition.x = containerObject.position.x;
                    contentPosition.y = containerObject.position.y + MixedRealityToolkit.Instance.ActiveProfile.ExperienceSettingsProfile.FloorHeight;
                    contentPosition.z = containerObject.position.z;

                    containerObject.position = contentPosition;
                }
                else
                {
                    contentPosition = Vector3.zero;
                    containerObject.position = contentPosition;
                }
            }

            if (alignmentType == AlignmentType.AlignWithHeadHeight)
            {
                contentPosition.x = containerObject.position.x;
                contentPosition.y = containerObject.position.y + CameraCache.Main.transform.position.y;
                contentPosition.z = containerObject.position.z;

                containerObject.position = contentPosition;
            }
        }
    }
}