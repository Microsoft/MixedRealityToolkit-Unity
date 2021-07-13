// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI
{
    /// <summary>
    /// Simulates a point mass on a spring that can be used as a grabbable handle.
    /// </summary>
    [RequireComponent(typeof(NearInteractionGrabbable))]
    public class PinchSpring : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusHandler
    {
        #region Serialized Fields and Properties

        [Experimental, SerializeField, Tooltip("The static anchor point of the spring")]
        private Transform handleAnchor = null;

        /// <summary>
        /// The static anchor point of the spring.
        /// </summary>
        public Transform HandleAnchor
        {
            get => handleAnchor;
            set => handleAnchor = value;
        }

        [SerializeField, Tooltip("The visuals to connect the anchor to the tip. This object will be scaled along the local z-axis.")]
        private Transform handleConnector = null;

        /// <summary>
        /// The visuals to connect the anchor to the tip. This object will be scaled along the local z-axis.
        /// </summary>
        public Transform HandleConnector
        {
            get => handleConnector;
            set => handleConnector = value;
        }

        [SerializeField, Tooltip("The object that acts as the point mass in the spring system.")]
        private Transform handleTip = null;

        /// <summary>
        /// The object that acts as the point mass in the spring system.
        /// </summary>
        public Transform HandleTip
        {
            get => handleTip;
            set
            {
                handleTip = value;

                if (handleTip != null)
                {
                    tipScaleConstraint = handleTip.GetComponent<MinMaxScaleConstraint>();
                }
            }
        }

        [SerializeField, Tooltip("How far the tip should be positioned from the anchor when the spring is at rest.")]
        private float restingDistance = 0.0f;

        /// <summary>
        /// How far the tip should rest from the anchor when the spring is at rest.
        /// </summary>
        public float RestingDistance
        {
            get => restingDistance;
            set => restingDistance = value;
        }

        [SerializeField, Tooltip("How far the tip should be positioned from the anchor when the spring is at rest and focused.")]
        private float restingFocusedDistance = 0.05f;

        /// <summary>
        /// How far the tip should be positioned from the anchor when the spring is at rest and focused.
        /// </summary>
        public float RestingFocusedDistance
        {
            get => restingFocusedDistance;
            set => restingFocusedDistance = value;
        }

        [SerializeField, Tooltip("The direction the tip should be positioned from the anchor when the spring is at rest.")]
        private Vector3 restingDirection = new Vector3(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// The direction the tip should be positioned from the anchor when the spring is at rest.
        /// </summary>
        public Vector3 RestingDirection
        {
            get => restingDirection;
            set
            {
                restingDirection = value;
                restingDirection.Normalize();
            }
        }

        [SerializeField, Tooltip("The mass (in kilograms) of the tip.")]
        private float tipMass = 0.05f;

        /// <summary>
        /// The mass (in kilograms) of the tip.
        /// </summary>
        public float TipMass
        {
            get => tipMass;
            set => tipMass = value;
        }

        [SerializeField, Tooltip("The constant factor characteristic of the spring, or stiffness.")]
        private float springStiffness = 50.0f;

        /// <summary>
        /// The constant factor characteristic of the spring, or stiffness.
        /// </summary>
        public float SpringStiffness
        {
            get => springStiffness;
            set => springStiffness = value;
        }

        [SerializeField, Range(0.0f, 1.0f), Tooltip("The percentage of velocity to remove from the point mass each frame.")]
        private float springDampening = 0.9f;

        /// <summary>
        /// The percentage of velocity to remove from the point mass each frame.
        /// </summary>
        public float SpringDampening
        {
            get => springDampening;
            set => springDampening = value;
        }

        [SerializeField, Tooltip("Distance (in meters) to switch from interpolation to snapping to the grasp point when being manipulated.")]
        private float snapDistance = 0.03f;

        /// <summary>
        /// Distance (in meters) to switch from interpolation to snapping to the grasp point when being manipulated.
        /// </summary>
        public float SnapDistance
        {
            get => snapDistance;
            set => snapDistance = value;
        }

        [SerializeField, Tooltip("How quickly to move the tip when interpolation the position and scale.")]
        private float handleTipInterpolateSpeed = 20.0f;

        /// <summary>
        /// How quickly to move the tip when interpolation the position and scale.
        /// </summary>
        public float HandleTipInterpolateSpeed
        {
            get => handleTipInterpolateSpeed;
            set => handleTipInterpolateSpeed = value;
        }

        #endregion

        #region Private Members

        private MinMaxScaleConstraint tipScaleConstraint = null;
        private IMixedRealityPointer manipulatePointer = null;
        private IMixedRealityPointer focusedPointer = null;
        private Vector3 velocity = Vector3.zero;
        private float currentRestingDistance = 0.0f;
        private bool snapped = false;

        #endregion

        #region MonoBehaviour Implementation

        private void Start()
        {
            restingDirection.Normalize();

            if (handleTip != null)
            {
                tipScaleConstraint = handleTip.GetComponent<MinMaxScaleConstraint>();
            }
        }

        private void FixedUpdate()
        {
            UpdateTip();
            UpdateConnector();
        }

        #endregion

        #region IMixedRealityFocusHandler Implementation

        /// <inheritdoc />
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            manipulatePointer = eventData.Pointer;

            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnPointerDownEventHandler);
        }

        /// <inheritdoc />
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnPointerDraggedEventHandler);
        }

        /// <inheritdoc />
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            manipulatePointer = null;

            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnPointerUpEventHandler);
        }

        /// <inheritdoc />
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnInputClickedEventHandler);
        }

        #endregion

        #region IMixedRealityFocusHandler Implementation

        /// <inheritdoc />
        public void OnFocusEnter(FocusEventData eventData)
        {
            focusedPointer = eventData.Pointer;

            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnFocusEnterEventHandler);
        }

        /// <inheritdoc />
        public void OnFocusExit(FocusEventData eventData)
        {
            focusedPointer = null;

            // Continue to forward the events upward because this component is a passive observer.
            EventSystemExtensions.ExecuteHierarchyUpward(gameObject, eventData, MixedRealityInputSystem.OnFocusExitEventHandler);
        }

        #endregion

        #region Private Methods

        private void UpdateTip()
        {
            var deltaTime = Time.deltaTime;
            var t = handleTipInterpolateSpeed * deltaTime;

            // Move the handle tip towards the interacting pointer, else spring back to the resting location.
            var currentPosition = handleTip.position;
            var isFocused = focusedPointer != null;
            currentRestingDistance = Mathf.Lerp(currentRestingDistance, isFocused ? restingFocusedDistance : restingDistance, t);
            var restingPosition = handleAnchor.position + (transform.rotation * restingDirection) * currentRestingDistance;

            Vector3 targetPosition;
            Vector3 graspPosition;
            var nearPointer = focusedPointer as IMixedRealityNearPointer;

            if (nearPointer != null && nearPointer.TryGetNearGraspPoint(out graspPosition))
            {
                if (snapped || ((currentPosition - graspPosition).magnitude < snapDistance))
                {
                    // Snap to the grasp position.
                    targetPosition = graspPosition;
                    snapped = true;
                }
                else
                {
                    // Interpolate to the snap position.
                    targetPosition = Vector3.Lerp(currentPosition, graspPosition, t);
                }
            }
            else
            {
                // Spring back to the resting position using Hooke's Law.
                var delta = restingPosition - currentPosition;

                if (delta != Vector3.zero)
                {
                    var deltaMagnitude = delta.magnitude;
                    delta /= deltaMagnitude;

                    // Integrate the point mass.
                    var force = delta * (springStiffness * deltaMagnitude);
                    var acceleration = force / tipMass;

                    velocity += acceleration * deltaTime;
                    targetPosition = currentPosition + (velocity * deltaTime);

                    velocity *= springDampening;
                }
                else
                {
                    targetPosition = restingPosition;
                }

                snapped = false;
            }

            handleTip.position = targetPosition;

            // Update the tip scale.
            if (tipScaleConstraint != null)
            {
                var isManipulated = manipulatePointer != null;
                var tagetScale = isManipulated ? Vector3.one * tipScaleConstraint.ScaleMinimum : Vector3.one * tipScaleConstraint.ScaleMaximum;
                handleTip.localScale = Vector3.Lerp(handleTip.localScale, tagetScale, t);
            }
        }

        private void UpdateConnector()
        {
            if (handleConnector != null)
            {
                var delta = handleTip.position - handleAnchor.position;
                handleConnector.position = (handleAnchor.position + handleTip.position) * 0.5f;

                if (delta != Vector3.zero)
                {
                    handleConnector.rotation = Quaternion.LookRotation(delta);
                }

                var scale = handleConnector.localScale;
                handleConnector.localScale = new Vector3(scale.x, scale.y, delta.magnitude);
            }
        }

        #endregion
    }
}
