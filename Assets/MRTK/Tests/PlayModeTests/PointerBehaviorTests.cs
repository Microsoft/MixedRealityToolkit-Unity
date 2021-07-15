// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !WINDOWS_UWP
// When the .NET scripting backend is enabled and C# projects are built
// The assembly that this file is part of is still built for the player,
// even though the assembly itself is marked as a test assembly (this is not
// expected because test assemblies should not be included in player builds).
// Because the .NET backend is deprecated in 2018 and removed in 2019 and this
// issue will likely persist for 2018, this issue is worked around by wrapping all
// play mode tests in this check.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Teleport;
using Microsoft.MixedReality.Toolkit.Utilities;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Microsoft.MixedReality.Toolkit.Tests
{
    /// <summary>
    /// Verify that pointers can be turned on and off via FocusProvider.SetPointerBehavior
    /// </summary>
    public class PointerBehaviorTests : BasePlayModeTests
    {
        // Tests/PlayModeTests/TestProfiles/TestCustomPointerMediatorInputProfile.asset
        private const string ProfileGuid = "f1bb9273769c84743a29dc206e284023";
        private static readonly string ProfilePath = AssetDatabase.GUIDToAssetPath(ProfileGuid);

        /// <summary>
        /// Simple container for comparing expected pointer states to actual.
        /// if a bool? is null, this means pointer is null (doesn't exist)
        /// if a bool? is false, this means the pointer exists, but is not enabled (IsInteractionEnabled == false)
        /// if a bool? is true, this means the pointer exists, and is enabled
        /// </summary>
        private class PointerStateContainer
        {
            public bool? LinePointerEnabled { get; set; }
            public bool? SpherePointerEnabled { get; set; }
            public bool? PokePointerEnabled { get; set; }
            public bool? GazePointerEnabled { get; set; }
            public bool? GGVPointerEnabled { get; set; }
        }

        private void EnsurePointerStates(Handedness h, PointerStateContainer c)
        {
            Action<IMixedRealityPointer, string, bool?> helper = (ptr, name, expected) =>
            {
                if (!expected.HasValue)
                {
                    Assert.Null(ptr, $"Expected {h} {name} to be null but it was not null");
                }
                else
                {
                    Assert.NotNull(ptr, $"Expected {name} to not be null, but it was null");
                    Assert.AreEqual(expected.Value, ptr.IsInteractionEnabled,
                    $"Incorrect state for {h} {name}.IsInteractionEnabled");
                }

            };
            helper(PointerUtils.GetPointer<LinePointer>(h), "Line Pointer", c.LinePointerEnabled);
            helper(PointerUtils.GetPointer<SpherePointer>(h), "Sphere Pointer", c.SpherePointerEnabled);
            helper(PointerUtils.GetPointer<PokePointer>(h), "Poke Pointer", c.PokePointerEnabled);
            helper(PointerUtils.GetPointer<GGVPointer>(h), "GGV Pointer", c.GGVPointerEnabled);
            helper(CoreServices.InputSystem.GazeProvider.GazePointer, "Gaze Pointer", c.GazePointerEnabled);
        }

        /// <summary>
        /// Tests that the gaze pointer can be turned on and off
        /// </summary>
        [UnityTest]
        public IEnumerator TestGaze()
        {
            PointerStateContainer gazeOn = new PointerStateContainer()
            {
                GazePointerEnabled = true,
                GGVPointerEnabled = true,
                PokePointerEnabled = null,
                SpherePointerEnabled = null,
                LinePointerEnabled = null
            };

            // set input simulation mode to GGV
            PlayModeTestUtilities.SetControllerSimulationMode(ControllerSimulationMode.HandGestures);

            TestHand rightHand = new TestHand(Handedness.Right);
            TestHand leftHand = new TestHand(Handedness.Left);

            TestContext.Out.WriteLine("Show both hands");
            yield return rightHand.Show(Vector3.zero);
            yield return leftHand.Show(Vector3.zero);

            EnsurePointerStates(Handedness.Right, gazeOn);
            EnsurePointerStates(Handedness.Left, gazeOn);

            TestContext.Out.WriteLine("Turn off gaze cursor");
            PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOff);

            yield return null;

            PointerStateContainer gazeOff = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = false,
                PokePointerEnabled = null,
                SpherePointerEnabled = null,
                LinePointerEnabled = null
            };
            EnsurePointerStates(Handedness.Right, gazeOff);
            EnsurePointerStates(Handedness.Left, gazeOff);
        }

        /// <summary>
        /// Tests that poke pointer can be turned on/off
        /// </summary>
        [UnityTest]
        public IEnumerator TestPoke()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<NearInteractionTouchableVolume>();
            cube.transform.position = Vector3.forward * 0.5f;

            TestHand rightHand = new TestHand(Handedness.Right);
            TestHand leftHand = new TestHand(Handedness.Left);

            TestContext.Out.WriteLine("Show both hands near touchable cube");
            yield return rightHand.Show(Vector3.zero);
            yield return leftHand.Show(Vector3.zero);
            yield return new WaitForFixedUpdate();

            PointerStateContainer touchOn = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = true,
                SpherePointerEnabled = false,
                LinePointerEnabled = false
            };

            EnsurePointerStates(Handedness.Right, touchOn);
            EnsurePointerStates(Handedness.Left, touchOn);

            TestContext.Out.WriteLine("Turn off poke pointer right hand");
            PointerUtils.SetHandPokePointerBehavior(PointerBehavior.AlwaysOff, Handedness.Right);
            yield return null;

            PointerStateContainer touchOff = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = false,
                SpherePointerEnabled = false,
                LinePointerEnabled = true
            };

            EnsurePointerStates(Handedness.Right, touchOff);
            EnsurePointerStates(Handedness.Left, touchOn);

            TestContext.Out.WriteLine("Turn off poke pointer both hands");
            PointerUtils.SetHandPokePointerBehavior(PointerBehavior.AlwaysOff);
            yield return null;

            EnsurePointerStates(Handedness.Right, touchOff);
            EnsurePointerStates(Handedness.Left, touchOff);
        }

        /// <summary>
        /// Tests the grab pointer can be turned on/off
        /// </summary>
        [UnityTest]
        public IEnumerator TestGrab()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<NearInteractionGrabbable>();
            cube.transform.position = Vector3.forward;
            TestUtilities.PlayspaceToOriginLookingForward();

            TestHand rightHand = new TestHand(Handedness.Right);
            TestHand leftHand = new TestHand(Handedness.Left);

            TestContext.Out.WriteLine("Show both hands near grabbable cube");
            yield return rightHand.Show(cube.transform.position);
            yield return leftHand.Show(cube.transform.position);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            PointerStateContainer grabOn = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = false,
                SpherePointerEnabled = true,
                LinePointerEnabled = false
            };

            EnsurePointerStates(Handedness.Right, grabOn);
            EnsurePointerStates(Handedness.Left, grabOn);

            TestContext.Out.WriteLine("Turn off grab pointer right hand");
            PointerUtils.SetHandGrabPointerBehavior(PointerBehavior.AlwaysOff, Handedness.Right);
            yield return null;

            PointerStateContainer grabOff = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = false,
                SpherePointerEnabled = false,
                LinePointerEnabled = true
            };

            EnsurePointerStates(Handedness.Right, grabOff);
            EnsurePointerStates(Handedness.Left, grabOn);

            TestContext.Out.WriteLine("Turn off grab pointer both hands");
            PointerUtils.SetHandGrabPointerBehavior(PointerBehavior.AlwaysOff);
            yield return null;

            EnsurePointerStates(Handedness.Right, grabOff);
            EnsurePointerStates(Handedness.Left, grabOff);
        }
        
        /// <summary>
        /// Tests that when source pose data is used the poke pointer and grab pointer are aligned in approximately the correct positions
        /// </summary>
        [UnityTest]
        public IEnumerator TestUseSourcePoseData()
        {
            TestHand rightHand = new TestHand(Handedness.Right);
            yield return rightHand.Show(Vector3.zero);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            PokePointer pokePointer = PointerUtils.GetPointer<PokePointer>(Handedness.Right);
            Assert.IsNotNull(pokePointer);
            SpherePointer grabPointer = PointerUtils.GetPointer<SpherePointer>(Handedness.Right);
            Assert.IsNotNull(grabPointer);

            pokePointer.UseSourcePoseData = true;
            grabPointer.UseSourcePoseData = true;
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            yield return rightHand.Hide();
            yield return rightHand.Show(Vector3.zero);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();


            // The source pose is centered on the palm
            MixedRealityPose palmPose;
            HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out palmPose);
            // This offset value is derived from the PokePointer's sourcePoseOffset property
            float pokePointerOffset = 0.075f; 
            TestUtilities.AssertAboutEqual(pokePointer.Position, grabPointer.Position + pokePointer.transform.forward * pokePointerOffset, "pointer was not in the expected position");
            TestUtilities.AssertAboutEqual(grabPointer.Position, palmPose.Position, "pointer was not in the expected position");
        }

        /// <summary>
        /// Tests that when the source pose data is used as a fallback when the normal pose action is not raised
        /// </summary>
        [UnityTest]
        public IEnumerator TestUseSourcePoseDataAsFallback()
        {
            TestHand rightHand = new TestHand(Handedness.Right);
            yield return rightHand.Show(Vector3.zero);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            PokePointer pokePointer = PointerUtils.GetPointer<PokePointer>(Handedness.Right);
            Assert.IsNotNull(pokePointer);
            pokePointer.UseSourcePoseAsFallback = true;

            SpherePointer grabPointer = PointerUtils.GetPointer<SpherePointer>(Handedness.Right);
            Assert.IsNotNull(grabPointer);
            grabPointer.UseSourcePoseAsFallback = true;

            // Setting the pointer's pose action to a new input action is functionally equivalent to ensuring that an event is never raised of the pointer's desired pose action
            // This makes it so it will have to use the source pose data as a fallback
            pokePointer.PoseAction = new MixedRealityInputAction();
            grabPointer.PoseAction = new MixedRealityInputAction();
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            yield return rightHand.Hide();
            yield return rightHand.Show(Vector3.zero);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            // The source pose is centered on the palm
            MixedRealityPose palmPose;
            HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out palmPose);

            // This offset value is derived from the PokePointer's sourcePoseOffset property
            float pokePointerOffset = 0.075f;
            TestUtilities.AssertAboutEqual(pokePointer.Position, grabPointer.Position + pokePointer.transform.forward * pokePointerOffset, "pointer was not in the expected position");
            TestUtilities.AssertAboutEqual(grabPointer.Position, palmPose.Position, "pointer was not in the expected position");
        }

        /// <summary>
        /// Tests that the teleport pointer functions as expected
        /// </summary>
        [UnityTest]
        public IEnumerator TestTeleportAndContentOffset()
        {
            var iss = PlayModeTestUtilities.GetInputSimulationService();
            ExperienceScale originalExperienceScale;
            float originalProfileContentOffset;

            float contentOffset = 1.3f;

            // MRTK has already been created by SetUp prior to calling this,
            // we have to shut it down to re-init with the custom input profile which
            // has our contentOffset value set
            PlayModeTestUtilities.TearDown();
            yield return null;

            // Initialize a profile with the appropriate contentOffset
            var profile = TestUtilities.GetDefaultMixedRealityProfile<MixedRealityToolkitConfigurationProfile>();

            originalProfileContentOffset = profile.ExperienceSettingsProfile.ContentOffset;
            originalExperienceScale = profile.ExperienceSettingsProfile.TargetExperienceScale;

            profile.ExperienceSettingsProfile.ContentOffset = contentOffset;
            profile.ExperienceSettingsProfile.TargetExperienceScale = ExperienceScale.Room;

            PlayModeTestUtilities.Setup(profile);

            yield return new WaitForSeconds(0.5f);

            // Ensure that the SceneContent object is contentOffset units above the origin
            Assert.AreEqual(GameObject.Find("MixedRealitySceneContent").transform.position.y, contentOffset, 0.005f);
            
            // Create a floor and make sure it's below the camera
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = -0.5f * Vector3.up;

            // Test Right Hand Teleport
            TestUtilities.PlayspaceToOriginLookingForward();
            float initialForwardPosition = MixedRealityPlayspace.Position.z;

            TestHand leftHand = new TestHand(Handedness.Left);
            TestHand rightHand = new TestHand(Handedness.Right);

            // Make sure the hand is in front of the camera
            yield return rightHand.Show(Vector3.forward * 0.6f);
            rightHand.SetRotation(Quaternion.identity);

            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.TeleportStart);
            // Wait for the hand to animate
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return new WaitForSeconds(1.0f / iss.InputSimulationProfile.HandGestureAnimationSpeed + 0.1f);
            
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.TeleportEnd);
            // Wait for the hand to animate
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return new WaitForSeconds(1.0f / iss.InputSimulationProfile.HandGestureAnimationSpeed + 0.1f);

            // We should have teleported in the forward direction after the teleport
            Assert.IsTrue(MixedRealityPlayspace.Position.z > initialForwardPosition);
            rightHand.Hide();

            // Test Left Hand Teleport
            TestUtilities.PlayspaceToOriginLookingForward();

            // Make sure the hand is in front of the camera
            yield return leftHand.Show(Vector3.forward * 0.6f);

            yield return leftHand.SetGesture(ArticulatedHandPose.GestureId.TeleportStart);
            // Wait for the hand to animate
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return new WaitForSeconds(1.0f / iss.InputSimulationProfile.HandGestureAnimationSpeed + 0.1f);

            yield return leftHand.SetGesture(ArticulatedHandPose.GestureId.TeleportEnd);
            // Wait for the hand to animate
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return new WaitForSeconds(1.0f / iss.InputSimulationProfile.HandGestureAnimationSpeed + 0.1f);

            // We should have teleported in the forward direction after the teleport
            Assert.IsTrue(MixedRealityPlayspace.Position.z > initialForwardPosition);

            // Reset the profile's settings to it's original value
            profile.ExperienceSettingsProfile.TargetExperienceScale = originalExperienceScale;
            profile.ExperienceSettingsProfile.ContentOffset = originalProfileContentOffset;

            leftHand.Hide();
        }


        /// <summary>
        /// Tests that the teleport pointer functions as expected
        /// </summary>
        [UnityTest]
        public IEnumerator TestTeleportLayers()
        {
            var iss = PlayModeTestUtilities.GetInputSimulationService();

            // Create a floor and make sure it's below the camera
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = -0.5f * Vector3.up;

            // Bring out the right hand and set it to the teleport gesture
            TestUtilities.PlayspaceToOriginLookingForward();
            float initialForwardPosition = MixedRealityPlayspace.Position.z;

            TestHand rightHand = new TestHand(Handedness.Right);

            // Make sure the hand is in front of the camera
            yield return rightHand.Show(Vector3.forward * 0.6f);
            rightHand.SetRotation(Quaternion.identity);

            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.TeleportStart);
            // Wait for the hand to animate
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return new WaitForSeconds(1.0f / iss.InputSimulationProfile.HandGestureAnimationSpeed + 0.1f);

            TeleportPointer teleportPointer = rightHand.GetPointer<TeleportPointer>();

            floor.layer = LayerMask.NameToLayer("Default");
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.AreEqual(teleportPointer.TeleportSurfaceResult, Physics.TeleportSurfaceResult.Valid);

            floor.layer = LayerMask.NameToLayer("Ignore Raycast");
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.AreEqual(rightHand.GetPointer<TeleportPointer>().TeleportSurfaceResult, Physics.TeleportSurfaceResult.Invalid);

            floor.layer = LayerMask.NameToLayer("UI");
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.AreEqual(rightHand.GetPointer<TeleportPointer>().TeleportSurfaceResult, Physics.TeleportSurfaceResult.None);

        }

        /// <summary>
        /// Tests strafing with the teleport pointer
        /// </summary>
        [UnityTest]
        public IEnumerator TestTeleportStrafe()
        {
            var iss = PlayModeTestUtilities.GetInputSimulationService();

            // Create a floor and make sure it's below the camera
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = -0.5f * Vector3.up;

            // Bring out the right hand and set it to the teleport gesture
            TestUtilities.PlayspaceToOriginLookingForward();
            Vector3 initialPosition = MixedRealityPlayspace.Position;

            TestHand rightHand = new TestHand(Handedness.Right);

            // Make sure the hand is in front of the camera
            yield return rightHand.Show(Vector3.forward * 0.6f);
            rightHand.SetRotation(Quaternion.identity);

            TeleportPointer teleportPointer = rightHand.GetPointer<TeleportPointer>();
            teleportPointer.PerformStrafe();
            TestUtilities.AssertAboutEqual(MixedRealityPlayspace.Position, initialPosition - Vector3.forward * teleportPointer.strafeAmount, "Did not strafe to the expected position");

            teleportPointer.checkForFloorOnStrafe = true;
            teleportPointer.adjustHeightOnStrafe = true;
            teleportPointer.strafeAmount = 1.0f;
            teleportPointer.maxHeightChangeOnStrafe = 0.5f;

            TestUtilities.PlayspaceToOriginLookingForward();
            teleportPointer.PerformStrafe();
            TestUtilities.AssertAboutEqual(MixedRealityPlayspace.Position, initialPosition, "Performed an invalid strafe");

            var floor2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor2.transform.position = new Vector3(0,-0.25f,-1.0f);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            
            TestUtilities.PlayspaceToOriginLookingForward();
            teleportPointer.PerformStrafe();
            TestUtilities.AssertAboutEqual(MixedRealityPlayspace.Position, initialPosition + new Vector3(0, 0.25f, -teleportPointer.strafeAmount), "Height did not change on strafe");

            floor2.transform.position = new Vector3(0, -0.75f, -1.0f);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            TestUtilities.PlayspaceToOriginLookingForward();
            teleportPointer.PerformStrafe();
            TestUtilities.AssertAboutEqual(MixedRealityPlayspace.Position, initialPosition + new Vector3(0, -0.25f, -teleportPointer.strafeAmount), "Height did not change on strafe");
        }

        /// <summary>
        /// Tests that rays can be turned on and off
        /// </summary>
        [UnityTest]
        public IEnumerator TestRays()
        {
            yield return TestRaysWorker();
        }


        /// <summary>
        /// Tests that rays can be turned on and off even with a custom mediator.
        /// </summary>
        /// <remarks>
        /// Added to ensure we don't regress https://github.com/microsoft/MixedRealityToolkit-Unity/issues/8243
        /// </remarks>
        [UnityTest]
        public IEnumerator TestRaysCustomMediator()
        {
            // MRTK has already been created by SetUp prior to calling this,
            // we have to shut it down to re-init with the custom input profile which
            // replaces the default pointer mediator.
            PlayModeTestUtilities.TearDown();
            yield return null;

            var profile = TestUtilities.GetDefaultMixedRealityProfile<MixedRealityToolkitConfigurationProfile>();
            profile.InputSystemProfile.PointerProfile = AssetDatabase.LoadAssetAtPath<MixedRealityPointerProfile>(ProfilePath);
            PlayModeTestUtilities.Setup(profile);
            yield return null;

            yield return TestRaysWorker();
        }

        private IEnumerator TestRaysWorker()
        {
            PointerStateContainer lineOn = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = false,
                SpherePointerEnabled = false,
                LinePointerEnabled = true
            };

            TestHand rightHand = new TestHand(Handedness.Right);
            TestHand leftHand = new TestHand(Handedness.Left);

            yield return rightHand.Show(Vector3.zero);
            yield return leftHand.Show(Vector3.zero);

            TestContext.Out.WriteLine("Show both hands");
            EnsurePointerStates(Handedness.Right, lineOn);
            EnsurePointerStates(Handedness.Left, lineOn);

            TestContext.Out.WriteLine("Turn off ray pointer both hands");
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
            yield return null;

            PointerStateContainer lineOff = new PointerStateContainer()
            {
                GazePointerEnabled = false,
                GGVPointerEnabled = null,
                PokePointerEnabled = false,
                SpherePointerEnabled = false,
                LinePointerEnabled = false
            };

            EnsurePointerStates(Handedness.Right, lineOff);
            EnsurePointerStates(Handedness.Left, lineOff);

            TestContext.Out.WriteLine("Turn on ray right hand.");
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOn, Handedness.Right);
            yield return null;

            EnsurePointerStates(Handedness.Right, lineOn);
            EnsurePointerStates(Handedness.Left, lineOff);

            TestContext.Out.WriteLine("Turn on ray (default behavior) right hand.");
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.Default, Handedness.Right);
            yield return null;
            EnsurePointerStates(Handedness.Right, lineOn);
            EnsurePointerStates(Handedness.Left, lineOff);

            TestContext.Out.WriteLine("Turn on ray (default behavior) left hand.");
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.Default, Handedness.Left);
            yield return null;
            EnsurePointerStates(Handedness.Right, lineOn);
            EnsurePointerStates(Handedness.Left, lineOn);
        }
    }
}

#endif
