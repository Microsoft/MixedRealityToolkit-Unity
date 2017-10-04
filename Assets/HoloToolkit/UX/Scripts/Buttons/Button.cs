//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using System;
using System.Collections;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

namespace HoloToolkit.Unity.Buttons
{
    /// <summary>
    /// Base class for buttons.
    /// </summary>
    public abstract class Button : MonoBehaviour, IInputHandler, IPointerSpecificFocusable, IHoldHandler, ISourceStateHandler, IInputClickHandler
    {
        #region Public Members

        public enum ButtonStateEnum
        {
            /// <summary>
            /// Looking at and Pressed
            /// </summary>
            Pressed,
            /// <summary>
            /// Looking at and finger up
            /// </summary>
            Targeted,
            /// <summary>
            /// Not looking at it and finger is up
            /// </summary>
            Interactive,
            /// <summary>
            /// Looking at button finger down
            /// </summary> 
            ObservationTargeted,
            /// <summary>
            /// Not looking at it and finger down
            /// </summary>
            Observation,
            /// <summary>
            /// Button in a disabled state
            /// </summary>
            Disabled,
        }

        /// <summary>
        /// Current Button State
        /// </summary>
        [Header("Button")]
        [Tooltip("Current State of the Button")]
        public ButtonStateEnum ButtonState = ButtonStateEnum.Observation;

        /// <summary>
        /// Filter to apply for the correct button source
        /// </summary>
        [Tooltip("Filter for press info for click or press event")]
        public InteractionSourcePressInfo ButtonPressFilter = InteractionSourcePressInfo.Select;

        /// <summary>
        /// If true the interactible will unselect when you look off of the object
        /// </summary>
        [Tooltip("If RequireGaze then looking away will unselect object")]
        public bool RequireGaze = true;

        /// <summary>
        /// Event to receive button state change
        /// </summary>
        public event Action<ButtonStateEnum> StateChange;

        /// <summary>
        /// Event fired when tap interaction received.
        /// </summary>
        public event Action<GameObject> OnButtonPressed;

        /// <summary>
        /// Event fired when released interaction received.
        /// </summary>
        public event Action<GameObject> OnButtonReleased;

        /// <summary>
        /// Event fired when click interaction received.
        /// </summary>
        public event Action<GameObject> OnButtonClicked;

        /// <summary>
        /// Event fired when hold interaction initiated.
        /// </summary>
        public event Action<GameObject> OnButtonHeld;

        /// <summary>
        /// Event fired when hold interaction cancelled.
        /// </summary>
        public event Action<GameObject> OnButtonCancelled;

        #endregion

        #region Private and Protected Members
        /// <summary>
        /// Internal protected member for our default gizmo icon
        /// </summary>
        protected string _gizmoIconDefault = "HUX/hux_button_icon.png";

        /// <summary>
        /// Internal protected member for our gizmo selected icon
        /// </summary>
        protected string _gizmoIconSelected = "HUX/hux_button_icon_selected.png";

        /// <summary>
        /// Protected string for the current active gizmo icon
        /// </summary>
        protected string _gizmoIcon;

        /// <summary>
        /// Last state of hands being visible
        /// </summary>
        private bool m_bLastHandVisible = false;

        /// <summary>
        /// State of hands being visible
        /// </summary>
        private bool m_bHandVisible = false;

        /// <summary>
        /// State of hands being visible
        /// </summary>
        private bool m_bFocused = false;

        /// <summary>
        /// Count of visible hands
        /// </summary>
        private int m_HandCount = 0;

        /// <summary>
        /// Check for disabled state or disabled behavior
        /// </summary>
        private bool m_disabled { get { return ButtonState == ButtonStateEnum.Disabled || !enabled; } }

        #endregion

        /// <summary>
        /// Public function to force a clicked event on a button
        /// </summary>
        public void TriggerClicked()
        {
            DoButtonPressed(true);
        }

        #region Input Interface Functions
        /// <summary>
        /// Handle input down events from IInputSource.
        /// </summary>
        /// <param name="args"></param>
        /// 
        public void OnInputDown(InputEventData eventData)
        {
            if (enabled)
            {
                if(ButtonPressFilter == InteractionSourcePressInfo.None || ButtonPressFilter == eventData.PressType)
                {
                    DoButtonPressed();

                    // Set state to Pressed
                    ButtonStateEnum newState = ButtonStateEnum.Pressed;
                    this.OnStateChange(newState);
                }
            }
        }

        /// <summary>
        /// Handle on input up events from IInputSource
        /// </summary>
        /// <param name="eventData"></param>
        public void OnInputUp(InputEventData eventData)
        {
            if (enabled)
            {
                if (ButtonPressFilter == InteractionSourcePressInfo.None || ButtonPressFilter == eventData.PressType)
                {
                    DoButtonReleased();
                }
            }
        }

        /// <summary>
        /// Handle clicked event
        /// </summary>
        /// <param name="eventData"></param>
        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (enabled)
            {
                if (ButtonPressFilter == InteractionSourcePressInfo.None || ButtonPressFilter == eventData.PressType)
                {
                    DoButtonPressed(true);
                }
            }
        }


        /// <summary>
        /// Handle On Hold started from IHoldSource
        /// </summary>
        /// <param name="eventData"></param>
        public void OnHoldStarted(HoldEventData eventData)
        {
            if (!m_disabled)
            {
                DoButtonPressed();
            }
        }

        /// <summary>
        /// Handle On Hold started from IHoldSource
        /// </summary>
        /// <param name="eventData"></param>
        public void OnHoldCompleted(HoldEventData eventData)
        {
            if (!m_disabled && ButtonState == ButtonStateEnum.Pressed)
            {
                DoButtonHeld();

                // Unset state from pressed.
                ButtonStateEnum newState = ButtonStateEnum.Targeted;
                this.OnStateChange(newState);
            }
        }

                /// <summary>
        /// Handle On Hold started from IHoldSource
        /// </summary>
        /// <param name="eventData"></param>
        public void OnHoldCanceled(HoldEventData eventData)
        {
            if (!m_disabled && ButtonState == ButtonStateEnum.Pressed)
            {
                DoButtonCancelled();
                // Unset state from pressed.

                ButtonStateEnum newState = ButtonStateEnum.Targeted;
                this.OnStateChange(newState);
            }
        }

        /// <summary>
        /// FocusManager SendMessage("FocusEnter") receiver.
        /// </summary>
        public void OnFocusEnter(PointerSpecificEventData eventData)
        {
            if (!m_disabled)
            {
                ButtonStateEnum newState = m_bHandVisible ? ButtonStateEnum.Targeted : ButtonStateEnum.ObservationTargeted;
                this.OnStateChange(newState);

                m_bFocused = true;
            }
        }

        /// <summary>
        /// FocusManager SendMessage("FocusExit") receiver.
        /// </summary>
        public void OnFocusExit(PointerSpecificEventData eventData)
        {
             if (!m_disabled) // && FocusManager.Instance.IsFocused(this))
            {
                if (ButtonState == ButtonStateEnum.Pressed)
                {
                    DoButtonCancelled();
                }

                ButtonStateEnum newState = m_bHandVisible ? ButtonStateEnum.Interactive : ButtonStateEnum.Observation;

                if (RequireGaze || ButtonState != ButtonStateEnum.Pressed)
                {
                    this.OnStateChange(newState);
                }

                m_bFocused = false;
            }
        }

        /// <summary>
        /// On Source detected see if it is a hand and increment hand count and set visiblity
        /// </summary>
        /// <param name="eventData"></param>
        public void OnSourceDetected(SourceStateEventData eventData)
        {
            InteractionSourceInfo sourceInfo;
            if (eventData.InputSource.TryGetSourceKind(eventData.SourceId, out sourceInfo))
            {
                if (sourceInfo == InteractionSourceInfo.Hand)
                {
                    m_HandCount++;
                    m_bHandVisible = true;
                }
            }
        }

        /// <summary>
        ///  On Source lost decrement hand count and set visiblity
        /// </summary>
        /// <param name="eventData"></param>
        public void OnSourceLost(SourceStateEventData eventData)
        {
            InteractionSourceInfo sourceInfo;
            if (eventData.InputSource.TryGetSourceKind(eventData.SourceId, out sourceInfo))
            {
                if (sourceInfo == InteractionSourceInfo.Hand)
                {
                    m_HandCount--;
                    m_bHandVisible = m_HandCount <= 0;
                }
            }
        }
        #endregion

        /// <summary>
        /// Called when button is pressed down.
        /// </summary>
        protected void DoButtonPressed(bool bRelease = false)
        {
            ButtonStateEnum newState = ButtonStateEnum.Pressed;
            this.OnStateChange(newState);

            if (OnButtonPressed != null)
            {
                OnButtonPressed(gameObject);
            }

            if (bRelease)
            {
                StartCoroutine("DelayedRelease", 0.2f);
            }
        }

        /// <summary>
        /// Called when button is released.
        /// </summary>
        protected void DoButtonReleased()
        {
            ButtonStateEnum newState;

            if(m_bFocused)
            {
                newState = m_bHandVisible ? ButtonStateEnum.Targeted : ButtonStateEnum.ObservationTargeted;
            }
            else
            {
                newState = m_bHandVisible ? ButtonStateEnum.Interactive : ButtonStateEnum.Observation;
            }

            this.OnStateChange(newState);

            if (OnButtonReleased != null)
            {
                OnButtonReleased(gameObject);
            }
        }

        /// <summary>
        /// Delayed function to release button works for click events
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator DelayedRelease(float delay)
        {
            yield return new WaitForSeconds(delay);
            DoButtonReleased();
        }

        /// <summary>
        /// Called while button is pressed down.
        /// </summary>
        protected void DoButtonHeld()
        {
            if (OnButtonHeld != null)
            {
                OnButtonHeld(gameObject);
            }
        }

        /// <summary>
        /// Called when something interrupts the button pressed state.
        /// </summary>
        protected void DoButtonCancelled()
        {
            if (OnButtonCancelled != null)
            {
                OnButtonCancelled(gameObject);
            }
        }

        /// <summary>
        /// Use LateUpdate to check for whether or not the hand is up
        /// </summary>
        public void LateUpdate()
        {
            if (!m_disabled && m_bLastHandVisible != m_bHandVisible)
            {
                OnHandVisibleChange(m_bHandVisible);
            }
        }

        /// <summary>
        /// Event to fire off when hand visibity changes
        /// </summary>
        /// <param name="visible"></param>
        public virtual void OnHandVisibleChange(bool visible)
        {
            m_bLastHandVisible = visible;

            ButtonStateEnum newState = ButtonState;

            switch (ButtonState)
            {
                case ButtonStateEnum.Interactive:
                {
                    newState = visible ? ButtonStateEnum.Interactive : ButtonStateEnum.Observation;
                    break;
                }

                case ButtonStateEnum.Targeted:
                {
                    newState = visible ? ButtonStateEnum.Targeted : ButtonStateEnum.ObservationTargeted;
                    break;
                }

                case ButtonStateEnum.Observation:
                {
                    newState = visible ? ButtonStateEnum.Interactive : ButtonStateEnum.Observation;
                    break;
                }

                case ButtonStateEnum.ObservationTargeted:
                {
                    newState = visible ? ButtonStateEnum.Targeted : ButtonStateEnum.ObservationTargeted;
                    break;
                }

            }

            OnStateChange(newState);
        }

        /// <summary>
        /// Ensures the button returns to a neutral state when disabled
        /// </summary>
        public virtual void OnDisable()
        {
            if (ButtonState != ButtonStateEnum.Disabled)
            {
                OnStateChange(ButtonStateEnum.Observation);
            }
        }

        /// <summary>
        /// Callback virtual function for when the button state changes
        /// </summary>
        /// <param name="newState">
        /// A <see cref="ButtonStateEnum"/> for the new button state.
        /// </param>
        public virtual void OnStateChange(ButtonStateEnum newState)
        {
            ButtonState = newState;

            // Send out the action/event for the statechange
            if (this.StateChange != null)
                this.StateChange(newState);
        }

#if UNITY_EDITOR
        /// <summary>
        /// On draw gizmo shows the icon for the object in the editor 
        /// </summary>
        private void OnDrawGizmos()
        {
            // Simple visualization if Gazer is none - we could be in a level without the gazer spawned yet, or in editor.
            Collider collider = this.GetComponent<Collider>();
            if (collider != null)
            {
                _gizmoIcon = UnityEditor.Selection.activeGameObject == this.gameObject ? _gizmoIconSelected : _gizmoIconDefault;
                Gizmos.DrawIcon(this.transform.position, _gizmoIcon, false);
                Gizmos.DrawIcon(collider.bounds.center + (collider.bounds.size.y * Vector3.up), _gizmoIcon, false);
            }
        }
#endif
    }
}