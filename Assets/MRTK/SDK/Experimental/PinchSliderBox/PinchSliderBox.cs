// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Microsoft.MixedReality.Toolkit.UI.PinchSlider;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI
{
    /// <summary>
    /// Manages and creates sliders to allow for non-uniform scaling of a box along multiple axes.
    /// The [MinMaxScaleConstraint](xref:Microsoft.MixedReality.Toolkit.UI.MinMaxScaleConstraint) is 
    /// used to control the target of the scale and scale constraints.
    /// </summary>
    [RequireComponent(typeof(MinMaxScaleConstraint))]
    public class PinchSliderBox : MonoBehaviour
    {
        #region Serialized Fields and Properties

        [Experimental, SerializeField, Tooltip("Should sliders be auto-created when this component is enabled?")]
        private bool createSlidersOnEnable = true;

        /// <summary>
        /// Should sliders be auto-created when this component is enabled?
        /// </summary>
        public bool CreateSlidersOnEnable
        {
            get => createSlidersOnEnable;
            set => createSlidersOnEnable = value;
        }

        [SerializeField, Tooltip("Should sliders be created for manipulating scale on the 'X' axis?")]
        private bool createXAxisSliders = true;

        /// <summary>
        /// Should sliders be created for manipulating scale on the 'X' axis?
        /// </summary>
        public bool CreateXAxisSliders
        {
            get => createXAxisSliders;
            set
            {
                createXAxisSliders = value;
                CreateSliders();
            }
        }

        [SerializeField, Tooltip("Should sliders be created for manipulating scale on the 'Y' axis?")]
        private bool createYAxisSliders = true;

        /// <summary>
        /// Should sliders be created for manipulating scale on the 'Y' axis?
        /// </summary>
        public bool CreateYAxisSliders
        {
            get => createYAxisSliders;
            set
            {
                createYAxisSliders = value;
                CreateSliders();
            }
        }

        [SerializeField, Tooltip("Should sliders be created for manipulating scale on the 'Z' axis?")]
        private bool createZAxisSliders = true;

        /// <summary>
        /// Should sliders be created for manipulating scale on the 'Z' axis?
        /// </summary>
        public bool CreateZAxisSliders
        {
            get => createZAxisSliders;
            set
            {
                createZAxisSliders = value;
                CreateSliders();
            }
        }

        [SerializeField, Tooltip("The prefab to spawn for slider thumb visualization.")]
        private GameObject thumbPrefab = null;

        /// <summary>
        /// The prefab to spawn for slider thumb visualization.
        /// </summary>
        public GameObject ThumbPrefab
        {
            get => thumbPrefab;
            set
            {
                thumbPrefab = value;
                CreateSliders();
            }
        }

        [SerializeField, Tooltip("The prefab use to demonstrate which axis of the box is being manipulated.")]
        private GameObject hightlightPrefab = null;

        /// <summary>
        /// The prefab use to demonstrate which axis of the box is being manipulated.
        /// </summary>
        public GameObject HightlightPrefab
        {
            get => hightlightPrefab;
            set
            {
                hightlightPrefab = value;
                CreateSliders();
            }
        }

        #endregion

        #region Private Members

        private MinMaxScaleConstraint scaleConstraint = null;
        private Transform pivot = null;
        private Transform axisHighlight = null;
        private Material defaultThumbMaterial = null;
        private bool quitting = false;

        private const int PositiveIndex = 0;
        private const int NegativeIndex = 1;
        private const int AxisOffset = 2;

        private class SliderPair
        {
            public PinchSlider[] Sliders = new PinchSlider[2];

            public float Value
            {
                get
                {
                    return (Sliders[PositiveIndex].SliderValue + 
                            Sliders[NegativeIndex].SliderValue) * 0.5f;
                }
            }
        }

        private class SliderPlane
        {
            public SliderAxis Axis = SliderAxis.XAxis;
            public SliderPair[] SliderPairs = new SliderPair[2];

            public SliderPair GetSliderPair(PinchSlider slider)
            {
                if (slider == SliderPairs[PositiveIndex].Sliders[PositiveIndex] ||
                    slider == SliderPairs[PositiveIndex].Sliders[NegativeIndex])
                {
                    return SliderPairs[PositiveIndex];
                }
                else
                {
                    return SliderPairs[NegativeIndex];
                }
            }
        }

        private const int SliderPlaneCount = 3;
        private SliderPlane[] sliderPlanes = new SliderPlane[SliderPlaneCount];
        private Dictionary<PinchSlider, SliderPlane> sliderToPlane = new Dictionary<PinchSlider, SliderPlane>();

        #endregion

        #region MonoBehaviour Implementation

        private void Awake()
        {
            // Ensure a [MinMaxScaleConstraint](xref:Microsoft.MixedReality.Toolkit.UI.MinMaxScaleConstraint) exists and it is initialized.
            scaleConstraint = gameObject.EnsureComponent<MinMaxScaleConstraint>();
            scaleConstraint.Initialize(new MixedRealityTransform(transform));

            if (thumbPrefab == null)
            {
                defaultThumbMaterial = new Material(StandardShaderUtility.MrtkStandardShader);
            }
        }

        private void OnDestroy()
        {
            Destroy(defaultThumbMaterial);
        }

        private void OnEnable()
        {
            if (createSlidersOnEnable)
            {
                CreateSliders();
            }
        }

        private void OnDisable()
        {
            OnHoverExited(null);
            DestroyHandles();
        }

        private void OnApplicationQuit()
        {
            quitting = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates sliders for each requested axis of the box. A pivot object is also created and 
        /// made the parent of the [MinMaxScaleConstraint](xref:Microsoft.MixedReality.Toolkit.UI.MinMaxScaleConstraint) TargetTransform.
        /// </summary>
        [ContextMenu("Create Sliders")]
        public void CreateSliders()
        {
            DestroyHandles();

            // Create a pivot object to contain the sliders and aide in non-uniform scaling. 
            pivot = new GameObject($"{nameof(PinchSliderBox)}Pivot").transform;
            pivot.parent = scaleConstraint.TargetTransform.parent;
            pivot.localPosition = scaleConstraint.TargetTransform.localPosition;
            pivot.localRotation = scaleConstraint.TargetTransform.localRotation;
            scaleConstraint.TargetTransform.parent = pivot;

            // Create an axis highlight game object to toggle when sliders are hovered upon.
            if (hightlightPrefab != null)
            {
                axisHighlight = Instantiate(hightlightPrefab, pivot, false).transform;
                axisHighlight.gameObject.SetActive(false);
            }

            // Create the requested sliders.
            if (createXAxisSliders)
            {
                sliderPlanes[(int)SliderAxis.XAxis] = AddSliderPlane(SliderAxis.XAxis);
            }

            if (createYAxisSliders)
            {
                sliderPlanes[(int)SliderAxis.YAxis] = AddSliderPlane(SliderAxis.YAxis);
            }

            if (createZAxisSliders)
            {
                sliderPlanes[(int)SliderAxis.ZAxis] = AddSliderPlane(SliderAxis.ZAxis);
            }
        }

        /// <summary>
        /// Destroys all sliders created with CreateSliders and restores the 
        /// [MinMaxScaleConstraint](xref:Microsoft.MixedReality.Toolkit.UI.MinMaxScaleConstraint)'s TargetTransform's parent.
        /// </summary>
        public void DestroyHandles()
        {
            if (pivot != null)
            {
                // Restore the original parent. Unity will present a warning if parents are altered when quitting.
                if (!quitting)
                {
                    scaleConstraint.TargetTransform.parent = pivot.parent;
                }

                Destroy(pivot.gameObject);
                pivot = null;
                axisHighlight = null;
            }

            for (var i = 0; i < SliderPlaneCount; ++i)
            {
                sliderPlanes[i] = null;
            }

            sliderToPlane.Clear();
        }

        #endregion

        #region Private Methods

        private SliderPlane AddSliderPlane(SliderAxis axis)
        {
            var sliders = new PinchSlider[4];
            var globalDirection = 1.0f;

            for (var i = 0; i < 2; ++i)
            {
                var localDirection = 1.0f;

                for (var j = 0; j < 2; ++j)
                {
                    sliders[i * 2 + j] = AddSlider(axis, globalDirection, localDirection);
                    localDirection = -localDirection;
                }

                globalDirection = -globalDirection;
            }

            var sliderPlane = new SliderPlane()
            {
                Axis = axis,
                SliderPairs = new SliderPair[]
                {
                    new SliderPair() { Sliders = new PinchSlider[] { sliders[0], sliders[1] } },
                    new SliderPair() { Sliders = new PinchSlider[] { sliders[2], sliders[3] } }
                }
            };

            foreach (var slider in sliders)
            {
                sliderToPlane.Add(slider, sliderPlane);
            }

            return sliderPlane;
        }

        private PinchSlider AddSlider(SliderAxis axis, float globalDirection, float localDirection)
        {
            var slider = new GameObject($"Slider {axis} {globalDirection} {localDirection}").AddComponent<PinchSlider>();
            slider.transform.parent = pivot;

            // Calculates a normal to the pinch slider axis to place the slider at.
            var targetTransform = scaleConstraint.TargetTransform;
            var axisNormal = GetSliderAxisDirection(CalculateAxisNormal(axis));
            var axisNormalHalfScale = CalculateAxisHalfScale(targetTransform, axisNormal);
            slider.transform.position = CalculateSliderPosition(targetTransform, axisNormal, axisNormalHalfScale, globalDirection);
            slider.transform.rotation = targetTransform.rotation;

            slider.CurrentSliderAxis = axis;
            var axisIndex = (int)axis;
            slider.SliderStartDistance = scaleConstraint.ScaleMinimumVector[axisIndex] * 0.5f * localDirection;
            slider.SliderEndDistance = scaleConstraint.ScaleMaximumVector[axisIndex] * 0.5f * localDirection;

            GameObject thumb;

            if (thumbPrefab != null)
            {
                thumb = Instantiate(thumbPrefab, slider.transform, false);
                thumb.EnsureComponent<NearInteractionGrabbable>();
                thumb.transform.rotation = Quaternion.LookRotation((targetTransform.rotation * axisNormal) * globalDirection);
            }
            else
            {
                thumb = CreateDefaultThumb(defaultThumbMaterial, slider.transform);
            }

            slider.ThumbRoot = thumb;

            var scaleRange = scaleConstraint.ScaleMaximumVector[axisIndex] - scaleConstraint.ScaleMinimumVector[axisIndex];
            slider.SliderValue = (targetTransform.localScale[axisIndex] - scaleConstraint.ScaleMinimumVector[axisIndex]) / scaleRange;
            
            slider.OnValueUpdated.AddListener(OnSlideValueUpdated);
            slider.OnHoverEntered.AddListener(OnHoverEntered);
            slider.OnHoverExited.AddListener(OnHoverExited);

            return slider;
        }

        private static Vector3 GetSliderAxisDirection(SliderAxis sliderAxis)
        {
            switch (sliderAxis)
            {
                case SliderAxis.XAxis:
                    return Vector3.right;
                case SliderAxis.YAxis:
                    return Vector3.up;
                case SliderAxis.ZAxis:
                    return Vector3.forward;
                default:
                    throw new ArgumentOutOfRangeException("Underhanded SliderAxis passed to GetSliderAxisDirection.");
            }
        }

        private static SliderAxis CalculateAxisNormal(SliderAxis axis)
        {
            return (SliderAxis)(((int)axis + AxisOffset) % SliderPlaneCount);
        }

        private static float CalculateAxisHalfScale(Transform targetTransform, Vector3 axis)
        {
            return Vector3.Dot(axis, targetTransform.localScale) * 0.5f;
        }

        private static Vector3 CalculateSliderPosition(Transform targetTransform, Vector3 axisNormal, float scale, float direction)
        {
            return targetTransform.position + (((targetTransform.rotation * axisNormal) * scale) * direction);
        }

        private static GameObject CreateDefaultThumb(Material material, Transform parent)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.name = "Thumb";
            primitive.AddComponent<NearInteractionGrabbable>();
            primitive.GetComponent<Renderer>().material = material;
            primitive.GetComponent<SphereCollider>().radius *= 3.0f;
            primitive.transform.parent = parent;
            primitive.transform.localPosition = Vector3.zero;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = Vector3.one * 0.03f;

            return primitive;
        }

        private void OnSlideValueUpdated(SliderEventData data)
        {
            var sliderPlane = sliderToPlane[data.Slider];
            var sliderPair = sliderPlane.GetSliderPair(data.Slider);

            var axisIndex = (int)sliderPlane.Axis;
            var scaleMin = scaleConstraint.ScaleMinimumVector[axisIndex];
            var scaleMax = scaleConstraint.ScaleMaximumVector[axisIndex];
            var scaleRange = scaleMax - scaleMin;
            var targetTransform = scaleConstraint.TargetTransform;

            // Update scale.
            var scale = targetTransform.localScale;
            scale[axisIndex] = (sliderPair.Value * scaleRange + scaleMin);
            targetTransform.localScale = scale;

            // Update position.
            var position = targetTransform.localPosition;
            position[axisIndex] = ((sliderPair.Sliders[PositiveIndex].SliderValue * scaleRange + scaleMin) * 0.25f) -
                                  ((sliderPair.Sliders[NegativeIndex].SliderValue * scaleRange + scaleMin) * 0.25f);
            targetTransform.localPosition = position;

            // Update the opposite slider pair.
            var oppositeSliderPair = (sliderPair == sliderPlane.SliderPairs[PositiveIndex]) ? sliderPlane.SliderPairs[NegativeIndex] : 
                                                                                              sliderPlane.SliderPairs[PositiveIndex];
            for (var i = 0; i < 2; ++i)
            {
                if (oppositeSliderPair.Sliders[i].SliderValue != sliderPair.Sliders[i].SliderValue)
                {
                    oppositeSliderPair.Sliders[i].SliderValue = sliderPair.Sliders[i].SliderValue;
                }
            }

            // Update the position of sliders on the modified plane.
            var copanarSliderPlane = sliderPlanes[(axisIndex + AxisOffset - 1) % SliderPlaneCount];

            if (copanarSliderPlane != null)
            {
                var axisNormal = GetSliderAxisDirection(CalculateAxisNormal(copanarSliderPlane.Axis));
                var axisNormalInverse = axisNormal;

                for (var i = 0; i < 3; ++i)
                {
                    axisNormalInverse[i] = axisNormalInverse[i] == 1.0f ? 0.0f : 1.0f;
                }

                var axisNormalHalfScale = CalculateAxisHalfScale(targetTransform, axisNormal);
                var globalDirection = 1.0f;

                for (var i = 0; i < 2; ++i)
                {
                    for (var j = 0; j < 2; ++j)
                    {
                        var slider = copanarSliderPlane.SliderPairs[i].Sliders[j];
                        slider.transform.position = CalculateSliderPosition(targetTransform, axisNormal, axisNormalHalfScale, globalDirection);

                        // Remove any translation due to scale.
                        slider.transform.localPosition -= Vector3.Scale(slider.transform.localPosition, axisNormalInverse);
                    }

                    globalDirection = -globalDirection;
                }
            }
        }

        private void OnHoverEntered(SliderEventData data)
        {
            if (axisHighlight != null)
            {
                axisHighlight.gameObject.SetActive(true);

                // Move the highlight to the hovered slider.
                var axisType = data.Slider.CurrentSliderAxis;
                var axis = GetSliderAxisDirection(axisType);
                var sliderPair = sliderToPlane[data.Slider].GetSliderPair(data.Slider);
                var direction = (sliderPair.Sliders[PositiveIndex] == data.Slider) ? 1.0f : -1.0f;

                axisHighlight.parent = scaleConstraint.TargetTransform;
                axisHighlight.localPosition = axis * 0.5f * direction;
                axisHighlight.localRotation = Quaternion.LookRotation(axis, axisType == SliderAxis.YAxis ? Vector3.right : Vector3.up);
                axisHighlight.localScale = Vector3.one;
            }
        }

        private void OnHoverExited(SliderEventData data)
        {
            if (axisHighlight != null)
            {
                axisHighlight.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
