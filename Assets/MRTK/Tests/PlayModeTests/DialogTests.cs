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
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Microsoft.MixedReality.Toolkit.Tests
{
    public class DialogTests : BasePlayModeTests
    {
        // SDK/Features/UX/Prefabs/Dialog/DialogSmall_192x96.prefab
        private const string smallDialogPrefabAssetGuid = "8e686c90124b8e14cbf8093893109e9a";
        private static readonly string slatePrefabAssetPath = AssetDatabase.GUIDToAssetPath(smallDialogPrefabAssetGuid);

        private GameObject dialogGameObject;
        private Dialog dialogComponent;
        private DialogButtonType dialogResult;

        private const float DialogStabilizationTime = 1.5f;

        public override IEnumerator Setup()
        {
            yield return base.Setup();
            TestUtilities.PlayspaceToOriginLookingForward();
            yield return null;
        }

        public override IEnumerator TearDown()
        {
            Object.Destroy(dialogGameObject);
            Object.Destroy(dialogComponent);
            yield return base.TearDown();
        }

        /// <summary>
        /// Tests the prefabs number of buttons
        /// </summary>
        [UnityTest]
        public IEnumerator TestDialogPrefabInitializations()
        {
            TestHand handRight = new TestHand(Handedness.Right);
            float dialogDistance;
            handRight.Show(Vector3.zero);

            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.OK, true);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            // near distances determined by the Dialog.Open() function
            dialogDistance = dialogGameObject.transform.position.magnitude;
            Assert.AreEqual(dialogComponent.Result.Buttons, DialogButtonType.OK);
            Assert.AreEqual(dialogComponent.Result.Message, "This is an example dialog");
            Assert.AreEqual(dialogComponent.Result.Title, "Test Dialog");
            Assert.AreEqual(dialogComponent.Result.Buttons, DialogButtonType.OK);
            Assert.IsTrue(dialogDistance > 0.5f && dialogDistance < 1.0f);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            // The dialog only supports displaying up to two options
            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.Yes | DialogButtonType.No, true);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            // near distances determined by the Dialog.Open() function
            dialogDistance = dialogGameObject.transform.position.magnitude;
            Assert.AreEqual(dialogComponent.Result.Buttons, DialogButtonType.Yes | DialogButtonType.No);
            Assert.IsTrue(dialogDistance > 0.5f && dialogDistance < 1.0f);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.Yes | DialogButtonType.No, false);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            // near distances determined by the Dialog.Open() function
            dialogDistance = dialogGameObject.transform.position.magnitude;
            Assert.AreEqual(dialogComponent.Result.Buttons, DialogButtonType.Yes | DialogButtonType.No);
            Assert.IsTrue(dialogDistance > 1.0f && dialogDistance < 2.0f);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
        }

        /// <summary>
        /// Tests the prefabs number of buttons
        /// </summary>
        [UnityTest]
        public IEnumerator TestDialogPrefabResults()
        {
            TestHand handRight = new TestHand(Handedness.Right);
            int handsteps = 80;

            // Testing near interactions
            handRight.Show(Vector3.zero);
            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.OK, true);

            // Wait for the dialog to move to a stable position
            yield return new WaitForSeconds(DialogStabilizationTime);

            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.IsTrue(dialogComponent.State == DialogState.WaitingForInput);
            yield return handRight.Move(new Vector3(0.0f, -0.1f, 1.0f), handsteps);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.AreEqual(dialogResult, DialogButtonType.OK);
            Assert.AreEqual(dialogComponent.State, DialogState.Closed);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            // The dialog only supports displaying up to two options
            yield return handRight.MoveTo(Vector3.zero);
            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.Yes | DialogButtonType.No, true);

            // Wait for the dialog to move to a stable position
            yield return new WaitForSeconds(DialogStabilizationTime);

            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.IsTrue(dialogComponent.State == DialogState.WaitingForInput);
            yield return handRight.Move(new Vector3(0.1f, -0.1f, 1.0f), handsteps);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            Assert.AreEqual(dialogResult, DialogButtonType.No);
            Assert.AreEqual(dialogComponent.State, DialogState.Closed);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            // Testing far interactions
            // The dialog only supports displaying up to two options
            yield return handRight.MoveTo(Vector3.zero);
            InstantiateFromPrefab("Test Dialog", "This is an example dialog", DialogButtonType.Yes | DialogButtonType.No, false);

            // Wait for the dialog to move to a stable position
            yield return new WaitForSeconds(DialogStabilizationTime);


            Assert.IsTrue(dialogComponent.State == DialogState.WaitingForInput);
            // moving the hand to an appropriate position to click on the dialog
            yield return handRight.Move(new Vector3(-0.1f, -0.2f, 0.7f), handsteps);
            yield return handRight.SetGesture(ArticulatedHandPose.GestureId.Pinch);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();
            yield return handRight.SetGesture(ArticulatedHandPose.GestureId.Open);
            yield return PlayModeTestUtilities.WaitForInputSystemUpdate();

            Assert.AreEqual(dialogResult, DialogButtonType.Yes);
            Assert.AreEqual(dialogComponent.State, DialogState.Closed);

            GameObject.Destroy(dialogGameObject);
            GameObject.Destroy(dialogComponent);
        }

        /// <summary>
        /// Instantiates a slate from the default prefab at position and rotation
        /// </summary>
        private void InstantiateFromPrefab(string title, string message, DialogButtonType buttonType, bool isNearInteraction, Vector3? position = null, Quaternion? rotation = null)
        {
            GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(slatePrefabAssetPath, typeof(GameObject));

            dialogComponent = Dialog.Open(prefab, buttonType, title, message, isNearInteraction);
            Assert.IsNotNull(dialogComponent);

            dialogGameObject = dialogComponent.gameObject;
            Assert.IsNotNull(dialogGameObject);

            if (dialogComponent != null)
            {
                dialogComponent.OnClosed += OnClosedDialogEvent;
            }

            dialogGameObject.transform.position = position != null ? (Vector3)position : Vector3.forward;
            dialogGameObject.transform.rotation = rotation != null ? (Quaternion)rotation : Quaternion.identity;
        }

        private void OnClosedDialogEvent(DialogResult obj)
        {
            dialogResult = obj.Result;
        }
    }
}
#endif
