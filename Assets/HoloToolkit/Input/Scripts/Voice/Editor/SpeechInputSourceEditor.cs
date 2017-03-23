﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace HoloToolkit.Unity.InputModule
{
    [CustomEditor(typeof(SpeechInputSource))]
    public class SpeechInputSourceEditor : Editor
    {
        private SerializedProperty recognizerStart;
        private SerializedProperty keywordsAndKeys;
        private SerializedProperty recognizeResponse;

        private void OnEnable()
        {
            recognizerStart = serializedObject.FindProperty("RecognizerStart");
            keywordsAndKeys = serializedObject.FindProperty("Keywords");
            recognizeResponse = serializedObject.FindProperty("RecognizeRespone");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // the RecognizerStart field
            EditorGUILayout.PropertyField(recognizerStart);
            // the Keywords field
            ShowList(keywordsAndKeys);
            // RecognizeResponse field
            EditorGUILayout.PropertyField(recognizeResponse);
            serializedObject.ApplyModifiedProperties();

            // error and warning messages
            if (keywordsAndKeys.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No keywords have been assigned!", MessageType.Warning);
            }
        }

        private static GUIContent removeButtonContent = new GUIContent("-", "Remove keyword");
        private static GUIContent addButtonContent = new GUIContent("+", "Add keyword");
        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20.0f);

        private static void ShowList(SerializedProperty list)
        {
            // property name with expansion widget
            EditorGUILayout.PropertyField(list);

            if (list.isExpanded)
            {
                EditorGUI.indentLevel++;

                // header row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Keyword");
                EditorGUILayout.LabelField("Key Shortcut");
                EditorGUILayout.EndHorizontal();

                // keyword rows
                for (int index = 0; index < list.arraySize; index++)
                {
                    EditorGUILayout.BeginHorizontal();
                    // the element
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(index));
                    // the remove element button
                    if (GUILayout.Button(removeButtonContent, EditorStyles.miniButton, miniButtonWidth))
                    {
                        list.DeleteArrayElementAtIndex(index);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // add button row
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                // the add element button
                if (GUILayout.Button(addButtonContent, EditorStyles.miniButton, miniButtonWidth))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }
    }
}
