// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data consumer that can alter the style of a text
    /// component based on a style lookup.
    /// 
    /// Currently supported are:
    ///     TextMeshPro (via TextMeshProUGUI)
    ///     TextMesh (via UnityEngine.UI.Text)
    /// 
    /// One of these data consumer components can manage any number
    /// of text components so long as they are being populated by
    /// the same data source.
    /// 
    /// </summary>
    /// 
    public class DataConsumerTextStyle : DataConsumerGOBase
    {

        [Tooltip("Key path for style sheet name.")]
        [SerializeField]
        private string styleSheetKeyPath = "stylesheet";

        [Tooltip("Manage sprites in child game objects as well as this one.")]
        [SerializeField]
        private bool manageChildren = true;

        protected List<TextMeshProUGUI> _textComponents = new List<TextMeshProUGUI>();



        protected override Type[] GetComponentTypes()
        {

            Type[] types = { typeof(TextMeshProUGUI) };
            return types;
        }

        protected override bool ManageChildren()
        {
            return manageChildren;
        }



        protected override void ProcessDataChanged(IDataSource dataSource, string resolvedKeyPath, string localKeyPath, object newValue, DataChangeType dataChangeType )
        {

            string stylesheetPath = "Stylesheets/" + newValue.ToString();

            TMP_StyleSheet tmpStyleSheet = Resources.Load<TMP_StyleSheet>(stylesheetPath);

            if ( tmpStyleSheet == null )
            {
                UnityEngine.Debug.LogError("Stylesheet not found at resource path " + stylesheetPath);
            }
            else
            {
                foreach (TextMeshProUGUI textMeshComponent in _textComponents)
                {
#if UNITY_2019_1_OR_NEWER
                    textMeshComponent.styleSheet = tmpStyleSheet;
#else
                    Debug.LogWarning("TextMeshPro stylesheets only work in Unity 2019 or later.");
#endif
                }
            }

        }



        protected override void AddVariableKeyPathsForComponent(Type componentType, Component component)
        {   
            // We only asked for TextMeshProGUI components, so we can confidently cast here.

            _textComponents.Add(component as TextMeshProUGUI);
            AddKeyPathListener(styleSheetKeyPath);
        }

    }
}
