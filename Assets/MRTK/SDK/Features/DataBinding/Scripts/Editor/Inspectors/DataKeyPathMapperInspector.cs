// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;

namespace Microsoft.MixedReality.Toolkit.Data
{
    [CustomEditor(typeof(DataKeyPathMapperGODictionary.ViewToDataKeypathMap))]

    public class DataKeyPathMapperInspector : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("viewKeypathToDataKeypathMapper"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
