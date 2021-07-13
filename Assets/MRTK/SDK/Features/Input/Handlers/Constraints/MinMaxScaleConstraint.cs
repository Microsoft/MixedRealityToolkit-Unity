// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Component for setting the min/max scale values for ObjectManipulator
    /// or BoundsControl
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/MinMaxScaleConstraint")]
    public class MinMaxScaleConstraint : TransformConstraint
    {
        #region Properties

        [SerializeField]
        [Tooltip("Minimum scaling allowed")]
        private float scaleMinimum = 0.2f;

        private Vector3 minimumScale;

        /// <summary>
        /// Minimum scaling allowed
        /// </summary>
        public float ScaleMinimum
        {
            get => minimumScale.x;
            set
            {
                scaleMinimum = value;
                SetScaleLimits();
            }
        }

        /// <summary>
        /// Accessor for the minimum scale along all three axes.
        /// </summary>
        public Vector3 ScaleMinimumVector => minimumScale;

        [SerializeField]
        [Tooltip("Maximum scaling allowed")]
        private float scaleMaximum = 2f;

        private Vector3 maximumScale;

        /// <summary>
        /// Maximum scaling allowed
        /// </summary>
        public float ScaleMaximum
        {
            get => maximumScale.x;
            set
            {
                scaleMaximum = value;
                SetScaleLimits();
            }
        }

        /// <summary>
        /// Accessor for the maximum scale along all three axes.
        /// </summary>
        public Vector3 ScaleMaximumVector => maximumScale;

        [SerializeField]
        [Tooltip("Min/max scaling relative to initial scale if true")]
        private bool relativeToInitialState = true;

        /// <summary>
        /// Min/max scaling relative to initial scale if true
        /// </summary>
        public bool RelativeToInitialState
        {
            get => relativeToInitialState;
            set
            {
                relativeToInitialState = value;
                SetScaleLimits();
            }
        }

        public override TransformFlags ConstraintType => TransformFlags.Scale;

        #endregion Properties

        #region Public Methods
        public override void Initialize(MixedRealityTransform worldPose)
        {
            base.Initialize(worldPose);
            SetScaleLimits();
        }

        /// <summary>
        /// Clamps the transform scale to the scale limits set by <see cref="SetScaleLimits"/> such that:
        /// - No one component of the returned vector will be greater than the max scale.
        /// - No one component of the returned vector will be less than the min scale.
        /// - The returned vector's direction will be the same as the given vector
        /// </summary>
        public override void ApplyConstraint(ref MixedRealityTransform transform)
        {
            if (Vector3.Min(maximumScale, transform.Scale) != transform.Scale)
            {
                float maxRatio = 0.0f;
                int maxIdx = -1;

                // Find out the component with the maximum ratio to its maximum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (maximumScale[i] > 0)
                    {
                        float ratio = transform.Scale[i] / maximumScale[i];
                        if (ratio > maxRatio)
                        {
                            maxRatio = ratio;
                            maxIdx = i;
                        }
                    }
                }

                if (maxIdx != -1)
                {
                    transform.Scale /= maxRatio;
                }
            }

            if (Vector3.Max(minimumScale, transform.Scale) != transform.Scale)
            {
                float minRatio = 1.0f;
                int minIdx = -1;

                // Find out the component with the minimum ratio to its minimum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (minimumScale[i] > 0)
                    {
                        float ratio = transform.Scale[i] / minimumScale[i];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            minIdx = i;
                        }
                    }
                }

                if (minIdx != -1)
                {
                    transform.Scale /= minRatio;
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void SetScaleLimits()
        {
            if (relativeToInitialState)
            {
                minimumScale = worldPoseOnManipulationStart.Scale * scaleMinimum;
                maximumScale = worldPoseOnManipulationStart.Scale * scaleMaximum;
            }
            else
            {
                minimumScale = new Vector3(scaleMinimum, scaleMinimum, scaleMinimum);
                maximumScale = new Vector3(scaleMaximum, scaleMaximum, scaleMaximum);
            }
        }

        #endregion Private Methods
    }
}