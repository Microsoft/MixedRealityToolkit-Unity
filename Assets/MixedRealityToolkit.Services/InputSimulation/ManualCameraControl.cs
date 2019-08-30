// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Class for manually controlling the camera in the Unity editor. Attach to the MainCamera object.
    /// </summary>
    public class ManualCameraControl
    {
        private MixedRealityInputSimulationProfile profile;

        private bool isMouseJumping = false;
        private bool isGamepadLookEnabled = true;
        private bool isFlyKeypressEnabled = true;
        private Vector3 lastTrackerToUnityTranslation = Vector3.zero;
        private Quaternion lastTrackerToUnityRotation = Quaternion.identity;
        private bool wasLooking = false;
        private bool wasCursorVisible = true;

        public ManualCameraControl(MixedRealityInputSimulationProfile _profile)
        {
            profile = _profile;
        }

        private static float InputCurve(float x)
        {
            // smoothing input curve, converts from [-1,1] to [-2,2]
            return Mathf.Sign(x) * (1.0f - Mathf.Cos(0.5f * Mathf.PI * Mathf.Clamp(x, -1.0f, 1.0f)));
        }

        public void UpdateTransform(Transform transform, Vector3 mouseDelta)
        {
            // Undo the last tracker to Unity transforms applied
            transform.Translate(-this.lastTrackerToUnityTranslation, Space.World);
            transform.Rotate(-this.lastTrackerToUnityRotation.eulerAngles, Space.World);

            // Calculate and apply the camera control movement this frame
            Vector3 rotate = GetCameraControlRotation(mouseDelta);
            Vector3 translate = GetCameraControlTranslation(transform);

            transform.Rotate(rotate.x, 0.0f, 0.0f);
            transform.Rotate(0.0f, rotate.y, 0.0f, Space.World);
            transform.Translate(translate, Space.World);

            transform.Rotate(this.lastTrackerToUnityRotation.eulerAngles, Space.World);
            transform.Translate(this.lastTrackerToUnityTranslation, Space.World);
        }

        private static float GetKeyDir(string neg, string pos)
        {
            return UnityEngine.Input.GetKey(neg) ? -1.0f : UnityEngine.Input.GetKey(pos) ? 1.0f : 0.0f;
        }

        private Vector3 GetCameraControlTranslation(Transform transform)
        {
            Vector3 deltaPosition = Vector3.zero;

            // Support fly up/down keypresses if the current project maps it. This isn't a standard
            // Unity InputManager mapping, so it has to gracefully fail if unavailable.
            if (this.isFlyKeypressEnabled)
            {
                try
                {
                    deltaPosition += InputCurve(UnityEngine.Input.GetAxis("Fly")) * transform.up;
                }
                catch (System.Exception)
                {
                    this.isFlyKeypressEnabled = false;
                }
            }
            else
            {
                // use page up/down in this case
                deltaPosition += GetKeyDir("page down", "page up") * Vector3.up;
            }

            deltaPosition += InputCurve(UnityEngine.Input.GetAxis(profile.MoveHorizontal)) * transform.right;

            Vector3 forward;
            Vector3 up;
            if (profile.CurrentControlMode == InputSimulationControlMode.Walk)
            {
                up = Vector3.up;
                forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            }
            else
            {
                forward = transform.forward;
                up = transform.up;
            }
            deltaPosition += InputCurve(UnityEngine.Input.GetAxis(profile.MoveVertical)) * forward;
            deltaPosition += InputCurve(UnityEngine.Input.GetAxis(profile.MoveUpDown)) * up;

            float accel = KeyInputSystem.GetKey(profile.FastControlKey) ? profile.ControlFastSpeed : profile.ControlSlowSpeed;
            return accel * deltaPosition;
        }

        private Vector3 GetCameraControlRotation(Vector3 mouseDelta)
        {
            float inversionFactor = profile.IsControllerLookInverted ? -1.0f : 1.0f;

            Vector3 rot = Vector3.zero;

            if (this.isGamepadLookEnabled)
            {
                try
                {
                    // Get the axes information from the right stick of X360 controller
                    rot.x += InputCurve(UnityEngine.Input.GetAxis(profile.LookVertical)) * inversionFactor;
                    rot.y += InputCurve(UnityEngine.Input.GetAxis(profile.LookHorizontal));
                }
                catch (System.Exception)
                {
                    this.isGamepadLookEnabled = false;
                }
            }

            if (this.ShouldMouseLook)
            {
                if (!this.wasLooking)
                {
                    OnStartMouseLook();
                }
                else
                {
                    rot.x += -InputCurve(mouseDelta.y * profile.MouseRotationSensitivity);
                    rot.y += InputCurve(mouseDelta.x * profile.MouseRotationSensitivity);
                }

                this.wasLooking = true;
            }
            else
            {
                if (this.wasLooking)
                {
                    OnEndMouseLook();
                }

                this.wasLooking = false;
            }

            rot *= profile.ExtraMouseRotationScale;

            return rot;
        }

        private void OnStartMouseLook()
        {
            if (profile.MouseLookButton.BindingType == KeyBinding.KeyType.Mouse)
            {
                // if mousebutton is either left, right or middle
                SetWantsMouseJumping(true);
            }
            else if (profile.MouseLookButton.BindingType == KeyBinding.KeyType.Key)
            {
                // if mousebutton is either control, shift or focused
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                // save current cursor visibility before hiding it
                wasCursorVisible = UnityEngine.Cursor.visible;
                UnityEngine.Cursor.visible = false;
            }

            // do nothing if (this.MouseLookButton == MouseButton.None)
        }

        private void OnEndMouseLook()
        {
            if (profile.MouseLookButton.BindingType == KeyBinding.KeyType.Mouse)
            {
                // if mousebutton is either left, right or middle
                SetWantsMouseJumping(false);
            }
            else if (profile.MouseLookButton.BindingType == KeyBinding.KeyType.Key)
            {
                // if mousebutton is either control, shift or focused
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = wasCursorVisible;
            }

            // do nothing if (this.MouseLookButton == MouseButton.None)
        }

        private bool ShouldMouseLook
        {
            get
            {
                // Only allow the mouse to control rotation when Unity has focus. This enables
                // the player to temporarily alt-tab away without having the player look around randomly
                // back in the Unity Game window.
                if (!Application.isFocused)
                {
                    return false;
                }
                else
                {
                    if (profile.MouseLookToggle)
                    {
                        if (this.wasLooking)
                        {
                            // pressing escape will stop capture
                            return !UnityEngine.Input.GetKeyDown(KeyCode.Escape);
                        }
                        else
                        {
                            // any kind of click will capture focus
                            return KeyInputSystem.GetKeyDown(profile.MouseLookButton);
                        }
                    }
                    else
                    {
                        return KeyInputSystem.GetKey(profile.MouseLookButton);
                    }
                }
            }
        }

        /// <summary>
        /// Mouse jumping is where the mouse cursor appears outside the Unity game window, but
        /// disappears when it enters the Unity game window.
        /// </summary>
        /// <param name="wantsJumping">Show the cursor</param>
        private void SetWantsMouseJumping(bool wantsJumping)
        {
            if (wantsJumping != this.isMouseJumping)
            {
                this.isMouseJumping = wantsJumping;

                if (wantsJumping)
                {
                    // unlock the cursor if it was locked
                    UnityEngine.Cursor.lockState = CursorLockMode.None;

                    // save original state of cursor before hiding
                    wasCursorVisible = UnityEngine.Cursor.visible;
                    // hide the cursor
                    UnityEngine.Cursor.visible = false;
                }
                else
                {
                    // recenter the cursor (setting lockCursor has side-effects under the hood)
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.lockState = CursorLockMode.None;

                    // restore the cursor
                    UnityEngine.Cursor.visible = wasCursorVisible;
                }

    #if UNITY_EDITOR
                UnityEditor.EditorGUIUtility.SetWantsMouseJumping(wantsJumping ? 1 : 0);
    #endif
            }
        }
    }
}