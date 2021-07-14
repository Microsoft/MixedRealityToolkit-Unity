// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;

using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// This data consumer will swap any particular material of a specific name or name pattern, and 
    /// replace it with a material that matches a name constructed from a combination of static and
    /// dynamic data.  Example:
    /// 
    /// materialNameRegex:      backplate.*     Match any material that starts with the name backplate
    /// materialVariableName:   backplate_{{company}}_{{brand}}_{{locale}}
    /// 
    /// In this example, given a datasource that provides a company, brand and locale, chose a
    /// material that has the name constructed from those values:
    /// 
    ///    company:  Nike
    ///    brand:    AirJordan
    ///    locale:   en-UK
    ///    
    /// Then the name will be "backplate_Nike_AirJordan_en-UK"
    /// 
    /// If a Materal Data Source is provided, then the name will be used as a key path to request
    /// the correct material from that data source. This is useful for fetching materials from a back-end
    /// CMS solution.
    /// 
    /// </summary>

#if false

    [Serializable]
    public class DataConsumerMaterialLookup : DataConsumerGOBase
    {
        [Serializable]
        public struct MaterialToTemplateLookup
        {
            [Tooltip("Name of the material to swap out. If the material is used multiple times, all will be swapped. This will also be used as the default material if the calculated material can't be found.")]
            [SerializeField] public string materialName;

            [Tooltip("Material name teamplate with embedded data {{ variables }} used to construct the name or key path of the desired material.")]
            [SerializeField] public string materialSelectorTemplate;

        }

        [Tooltip("Optional data source for fetching the material that matches the key path. If not specified, the calculated material name will be used to load a local resource.")]
        [SerializeField]
        public string materialDataSource;

        [Tooltip("Manage materials in child game objects as well as this one.")]
        [SerializeField] private bool manageChildren = true;

        [Tooltip("List of value-to-sprite mappings.")]
        [SerializeField] private MaterialToTemplateLookup[] materialToTemplateLookup;


        protected override Type[] GetComponentTypes()
        {

            Type[] types = { typeof(Material) };
            return types;
        }


        protected override bool ManageChildren()
        {
            return manageChildren;
        }



        protected override void AddVariableKeyPathsForComponent(Type componentType, Component component)
        {
            foreach( MaterialToTemplateLookup materialLookup in materialToTemplateLookup)
            {
                if ( component.name == materialLookup.materialName )
                {
                    MatchCollection matches = GetVariableMatchingRegex().Matches();

                    foreach (Match match in matches)
                    {
                        string localKeyPath = match.Groups[1].Value;

                        string resolvedKeyPath = DataSource.ResolveKeyPath(ResolvedKeyPathPrefix, localKeyPath);

                        AddKeyPathListener(localKeyPath);

                    }
                }
            }
        }


        protected override void ProcessDataChanged(IDataSource dataSource, string resolvedKeyPath, string localKeyPath, object newValue, DataChangeType dataChangeType)
        {
            if (localKeyPath == keyPath)
            {
                string value = newValue.ToString();

                foreach (ValueToMaterialInfo v2si in valueToMaterialLookup)
                {
                    if (value == v2si.Value)
                    {
                        _spriteRenderer.sprite = v2si.Material;
                        break;
                    }
                }
            }
        }
    } // End of DataConsumerMaterialLookup

#endif

} // End of namespace 
