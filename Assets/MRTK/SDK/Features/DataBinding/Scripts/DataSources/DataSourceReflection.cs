// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;


using Microsoft.MixedReality.Toolkit.Utilities;

namespace Microsoft.MixedReality.Toolkit.Data
{

    /// <summary>
    /// A data source that uses reflection to determine the structure of an object, 
    /// either a struct, a class or a primitive, and makes objects in that structure
    /// available using a keypath structure similar to what is used for the DataSourceObjects
    /// and DataSourceJson classes.
    /// 
    /// </summary>
    /// 
    public class DataSourceReflection : DataSourceBase
    {
        protected static readonly string CollectionElementkeyPathPrefixFormat = "{0}[{1:d}]";
        protected static readonly string DictionaryElementkeyPathPrefixFormat = "{0}[{1}]";
        protected static readonly MemberFilter FieldOrPropertyByNameMemberFilter = new MemberFilter(FieldOrPropertyNameCompare);


        protected static readonly string ArrayTokenPattern = @"^\s*\[\s*([a-zA-Z0-9\-_]*?)\s*\]";
        protected static readonly string KeyTokenPattern = @"^\s*([a-zA-Z0-9\-_]+?)(?:[.\[]|$)";

        protected static readonly Regex ArrayTokenRegex = new Regex(ArrayTokenPattern);
        protected static readonly Regex KeyTokenRegex = new Regex(KeyTokenPattern);

        protected object _dataSourceObject = null;

        protected Dictionary<string, MemberInfo> _keyPathToMemberInfoLookup = new Dictionary< string, MemberInfo >();

        public DataSourceReflection() { }

        public DataSourceReflection( object dataSourceObject )
        {
            _dataSourceObject = dataSourceObject;
        }

        public void SetDataSourceObject( object dataSourceObject )
        {
            _dataSourceObject = dataSourceObject;
        }


        public override bool IsCollectionAtKeyPath(string resolvedKeyPath)
        {
            return IsArrayOrList(KeyPathToObject(resolvedKeyPath));
        }

        public override int GetCollectionCount(string resolvedKeyPath)
        {
            object value = KeyPathToObject(resolvedKeyPath);
            if (IsArray(value)) {
                return (value as Array).Length;
            } else if (IsList(value)) {
                return (value as IList).Count;
            } else
            {
                return 0;
            }
        }


        public override void SetValueInternal(string resolvedKeyPath, object value)
        {
            object currentObject = null;
            MemberInfo memberInfo = KeyPathToMemberInfo(resolvedKeyPath, out currentObject);
            if (memberInfo != null)
            {
                SetValueFromFieldOrProperty(_dataSourceObject, memberInfo, value);
            }
        }

        public override IEnumerable<string> GetCollectionKeyPathRange(string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            if (IsDataSourceAvailable())
            {
                object objectAtKeyPath = KeyPathToObject(resolvedKeyPath);
                if (objectAtKeyPath != null)
                {
                    if (IsArrayOrList(objectAtKeyPath))
                    {
                        return GetValueAsArrayKeyPathsOptimized(objectAtKeyPath, resolvedKeyPath, rangeStart, rangeCount);
                    }
                }
            }
            return null;
        }

        public override object GetValueInternal(string resolvedKeyPath)
        {
            if (IsDataSourceAvailable())
            {
                object objectAtKeyPath = KeyPathToObject(resolvedKeyPath);
                if (objectAtKeyPath != null)
                {
                    if (objectAtKeyPath is IList)
                    {
                        IList collection = objectAtKeyPath as IList;
                        return GetValueAsArrayKeyPaths(collection, resolvedKeyPath, 0, collection.Count);
                    }
                    else if (objectAtKeyPath is IDictionary)
                    {
                        return GetValueAsDictionaryKeyPaths(objectAtKeyPath as IDictionary, resolvedKeyPath);
                    }
                    else
                    {
                        return objectAtKeyPath;
                    }
                }
            }

            return null;
        }


        protected IEnumerable<string> GetValueAsArrayKeyPathsOptimized(object arrayOrList, string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            return new KeyPathEnumerable(resolvedKeyPath, rangeStart, rangeCount) as IEnumerable<string>;
        }


        protected IEnumerable<string> GetValueAsArrayKeyPaths(IList list, string resolvedKeyPath, int rangeStart, int rangeCount)
        {
            int rangeMax = rangeStart + rangeCount;

            if (rangeMax > list.Count)
            {
                rangeMax = list.Count;
            }


            List<string> keyPaths = new List<string>();
            for (int idx = rangeStart; idx < rangeMax; idx++)
            {
                keyPaths.Add(string.Format(CollectionElementkeyPathPrefixFormat, resolvedKeyPath, idx));
            }
            return keyPaths as IEnumerable<string>;

        }

        protected IEnumerable<string> GetValueAsDictionaryKeyPaths(IDictionary dict, string resolvedKeyPath)
        {
            List<string> keyPaths = new List<string>();
            foreach (string key in dict.Keys )
            {
                keyPaths.Add(string.Format(DictionaryElementkeyPathPrefixFormat, resolvedKeyPath, key));
            }
            return keyPaths as IEnumerable<string>;
        }


        protected object KeyPathToObject(string resolvedKeyPath)
        {
            if ( resolvedKeyPath == "")
            {
                return _dataSourceObject;   // root object
            }

            object foundObject = null;
            MemberInfo memberInfo = KeyPathToMemberInfo(resolvedKeyPath, out foundObject );
            if ( memberInfo != null && foundObject == null )
            {
                return GetValueFromFieldOrProperty(_dataSourceObject, memberInfo);
            }
            else
            {
               return foundObject;
            }
        }


        protected MemberInfo KeyPathToMemberInfo(string resolvedKeyPath, out object currentObject )
        {
            // TODO: Either figure out a way to always get member info, or remove lookup

            // look in MemberInfo cache for a hit to save search time
            //if (_keyPathToMemberInfoLookup.ContainsKey(resolvedKeyPath))
            //{
            //   return _keyPathToMemberInfoLookup[resolvedKeyPath];
            //}
            //else
            {
                MemberInfo foundMemberInfo = null;
                currentObject = _dataSourceObject;

                string keyPath = resolvedKeyPath;

                while (currentObject != null && keyPath != null && keyPath != "")
                {
                    int amountToSkip = 0;
                    MatchCollection arrayMatches = ArrayTokenRegex.Matches(keyPath);
                    if (arrayMatches.Count > 0)
                    {
                        string arrayIndexText = arrayMatches[0].Groups[1].Value;
                        int arrayIndex = int.Parse(arrayIndexText);

                        foundMemberInfo = null;     // TODO: For efficiency, add logic to get MemberInfo for an array or list

                        if (IsList(currentObject) )
                        {
                            IList list = currentObject as IList;
                            currentObject = list[arrayIndex];
                        }
                        else if (IsArray(currentObject))
                        {
                            Array array = currentObject as Array;
                            currentObject = array.GetValue(arrayIndex);
                        } 
                        else
                        {
                            currentObject = null;
                        }
                        amountToSkip = arrayMatches[0].Value.Length;

                    }
                    else
                    {
                        MatchCollection keyMatches = KeyTokenRegex.Matches(keyPath);
                        if (keyMatches.Count > 0)
                        {
                            string key = keyMatches[0].Groups[1].Value;

                            if (IsStructOrClass( currentObject ) )
                            {
                                currentObject = GetNamedFieldOrProperty(currentObject, key, out foundMemberInfo);
                            }
                            else
                            {
                                currentObject = null;
                            }

                            amountToSkip = key.Length;
                        }
                    }

                    if (keyPath.Length > amountToSkip && keyPath[amountToSkip] == '.')
                    {
                        amountToSkip++;
                    }
                    if (amountToSkip > 0)
                    {
                        keyPath = keyPath.Substring(amountToSkip);
                    }
                    else
                    {
                        currentObject = null;
                        break;      // was not a valid path piece
                    }
                }

                if (currentObject != null && foundMemberInfo != null )
                {
                    //_keyPathToMemberInfoLookup[resolvedKeyPath] = foundMemberInfo;
                }
                return foundMemberInfo;
            }
        }


        protected object GetNamedFieldOrProperty( object containingObject, string key, out MemberInfo foundMemberInfoOut ) {

            MemberInfo[] members;
            foundMemberInfoOut = null;

            try
            {
                Type objType = containingObject.GetType();

                members = objType.FindMembers(
                    MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.Public | BindingFlags.Instance,
                    FieldOrPropertyByNameMemberFilter,
                    key );

               if (members.Length > 0)
                {
                    // We can assume there is only one item with the exact name we searched for.
                    foundMemberInfoOut = members[0];
                    return GetValueFromFieldOrProperty(containingObject, foundMemberInfoOut);
                } 
            }
            catch (Exception)
            {
            }

            return null;
        }
        
        protected object GetValueFromFieldOrProperty( object containingObject, MemberInfo memberInfo )
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                return propertyInfo.GetValue(containingObject);
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                return fieldInfo.GetValue(containingObject);
            } else
            {
                return null;
            }

        }


        protected void SetValueFromFieldOrProperty(object containingObject, MemberInfo memberInfo, object value )
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                propertyInfo.SetValue(containingObject, value );
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                fieldInfo.SetValue(containingObject, value );
            }
        }


        protected static bool FieldOrPropertyNameCompare(MemberInfo objMemberInfo, Object key)
        {
            return objMemberInfo.Name.ToString() == key.ToString();
        }
    

        public bool IsList(object source)
        {
            return source is IList;
        }

        public bool IsArray(object source)
        {
            return source.GetType().IsArray;
        }

        public bool IsArrayOrList(object source)
        {
            return IsArray(source) || IsList(source);
        }

        public bool IsStructOrClass(object source)
        {
            Type type = source.GetType();

            return type.IsClass || (type.IsValueType && !type.IsPrimitive && !type.IsEnum);
        }


        protected override bool IsDataSourceAvailable()
        {
            return _dataSourceObject != null;
        }
    } // End of class DataSourceObjects

} // End of namespace Microsoft.MixedReality.Toolkit.Data


