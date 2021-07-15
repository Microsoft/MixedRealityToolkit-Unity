// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.﻿

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input.Editor
{
    [CustomEditor(typeof(ControllerPoseSynchronizer))]
    public class ControllerPoseSynchronizerInspector : UnityEditor.Editor
    {
        private const string SynchronizationSettingsKey = "MRTK_Inspector_SynchronizationSettingsFoldout";

        private static bool synchronizationSettingsFoldout = true;

        private SerializedProperty handedness;
        private SerializedProperty useSourcePoseData;
        private SerializedProperty useSourcePoseAsFallback;
        private SerializedProperty poseAction;
        private SerializedProperty destroyOnSourceLost;

        protected bool DrawHandednessProperty = true;

        protected virtual void OnEnable()
        {
            synchronizationSettingsFoldout = SessionState.GetBool(SynchronizationSettingsKey, synchronizationSettingsFoldout);
            handedness = serializedObject.FindProperty("handedness");
            useSourcePoseData = serializedObject.FindProperty("useSourcePoseData");
            useSourcePoseAsFallback = serializedObject.FindProperty("useSourcePoseAsFallback");
            poseAction = serializedObject.FindProperty("poseAction");
            destroyOnSourceLost = serializedObject.FindProperty("destroyOnSourceLost");
        }

        public override void OnInspectorGUI()
        {
            if (target != null)
            {
                InspectorUIUtility.RenderHelpURL(target.GetType());
            }

            serializedObject.Update();

            using (var c = new EditorGUI.ChangeCheckScope())
            {
                synchronizationSettingsFoldout = EditorGUILayout.Foldout(synchronizationSettingsFoldout, "Synchronization Settings", true);
                if (c.changed)
                {
                    SessionState.SetBool(SynchronizationSettingsKey, synchronizationSettingsFoldout);
                }
            }

            if (!synchronizationSettingsFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (DrawHandednessProperty)
                {
                    Rect position = EditorGUILayout.GetControlRect();
                    var label = new GUIContent(handedness.displayName);
                    using (new EditorGUI.PropertyScope(position, label, handedness))
                    {
                        var currentHandedness = (Handedness)handedness.enumValueIndex;

                        handedness.enumValueIndex = (int)(Handedness)EditorGUI.EnumPopup(position, label, currentHandedness,
                            (value) => { return (Handedness)value == Handedness.Left || (Handedness)value == Handedness.Right; });
                    }
                }

                EditorGUILayout.PropertyField(destroyOnSourceLost);
                EditorGUILayout.PropertyField(useSourcePoseData);

                if (!useSourcePoseData.boolValue)
                {
                    EditorGUILayout.PropertyField(useSourcePoseAsFallback);
                    EditorGUILayout.PropertyField(poseAction);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}