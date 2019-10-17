﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.Editor
{
    /// <summary>
    /// Base folder types for modules searched by the MixedRealityToolkitFiles utility.
    /// </summary>
    public enum MixedRealityToolkitModuleType
    {
        None = 0,
        Core,
        Generated,
        Providers,
        Services,
        SDK,
        Examples,
        Tests,
        Extensions,
        Tools,
        // This module only exists for testing purposes, and is used in edit mode tests in conjunction
        // with MixedRealityToolkitFiles to ensure that this class is able to reason over MRTK
        // files that are placed outside of the root asset folder.
        AdhocTesting = -1,
    }

    /// <summary>
    /// API for working with MixedRealityToolkit folders contained in the project.
    /// </summary>
    /// <remarks>
    /// This class works by looking for sentinel files (following the pattern MRTK.*.sentinel,
    /// for example, MRTK.Core.sentinel) in order to identify where the MRTK is located
    /// within the project.
    ///
    /// If the MRTK is being consumed as code that sits within the Assets folder, the "root"
    /// MRTK folder must be at most three directories deep - this search code will only reason
    /// over MRTK folders that sit in a depth range [0, 3].
    /// </remarks>
    [InitializeOnLoad]
    public static class MixedRealityToolkitFiles
    {
        /// <summary>
        /// This controls the behavior of MapRelativePathToAbsolutePath.
        /// </summary>
        private enum SearchType
        {
            /// <summary>
            /// This indicates
            /// </summary>
            File,
            Folder,
        }

        /// <summary>
        /// The MRTK uses "sentinel" files (for example, MRTK.Core.sentinel) which are used to uniquely
        /// identify the presence of certain MRTK folders and modules. This is the file pattern used
        /// to search within folders for those sentinel files and make the file search a little more
        /// efficient than a full file enumeration.
        /// </summary>
        private const string SentinelFilePattern = "MRTK.*.sentinel";

        /// <summary>
        /// In order to subscribe for a <see cref="OnPostprocessAllAssets(string[], string[], string[], string[])"/> callback, 
        /// the class declaring the method must derive from AssetPostprocessor. So this class is nested privately as to prevent instantiation of it.
        /// </summary>
        private class AssetPostprocessor : UnityEditor.AssetPostprocessor
        {
            public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                searchForFoldersTask.Wait();

                foreach (string asset in importedAssets.Concat(movedAssets))
                {
                    if (IsSentinelFile(asset))
                    {
                        // TODO: need to resolve for IsSentinelFile?
                        string fullAssetPath = ResolveFullAssetsPath(asset);
                        TryRegisterModuleViaFile(fullAssetPath);
                    }
                }

                foreach (string asset in deletedAssets.Concat(movedFromAssetPaths))
                {
                    if (IsSentinelFile(asset))
                    {
                        string fullAssetPath = ResolveFullAssetsPath(asset);
                        string folderPath = Path.GetDirectoryName(fullAssetPath);
                        TryUnregisterModuleFolder(folderPath);
                    }
                }
            }
        }

        // Storage of our list of module paths (stored as absolute file paths) and bucketed by ModuleType
        private readonly static Dictionary<MixedRealityToolkitModuleType, HashSet<string>> mrtkFolders =
            new Dictionary<MixedRealityToolkitModuleType, HashSet<string>>();

        private static Task searchForFoldersTask;

        private static string NormalizeSeparators(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        private static string FormatSeparatorsForUnity(string path) => path.Replace('\\', '/');

        private static readonly Dictionary<string, MixedRealityToolkitModuleType> moduleNameMap = new Dictionary<string, MixedRealityToolkitModuleType>()
        {
            { "Core", MixedRealityToolkitModuleType.Core },
            { "Generated", MixedRealityToolkitModuleType.Generated },
            { "Providers", MixedRealityToolkitModuleType.Providers },
            { "Services", MixedRealityToolkitModuleType.Services },
            { "SDK", MixedRealityToolkitModuleType.SDK },
            { "Examples", MixedRealityToolkitModuleType.Examples },
            { "Tests", MixedRealityToolkitModuleType.Tests },
            { "Extensions", MixedRealityToolkitModuleType.Extensions },
            { "Tools", MixedRealityToolkitModuleType.Tools },

            // This module only exists for testing purposes, and is used in edit mode tests in conjunction
            // with MixedRealityToolkitFiles to ensure that this class is able to reason over MRTK
            // files that are placed outside of the root asset folder.
            { "AdhocTesting", MixedRealityToolkitModuleType.AdhocTesting },
        };

        /// <summary>
        /// Maps an absolute path to be relative to the Project Root path (the Unity folder that contains Assets)
        /// </summary>
        /// <param name="absolutePath">The absolute path to the project.</param>
        /// <returns>The project relative path.</returns>
        /// <remarks>This doesn't produce paths that contain step out '..' relative paths.</remarks>
        public static string GetAssetDatabasePath(string absolutePath) => FormatSeparatorsForUnity(absolutePath).Replace(Application.dataPath, "Assets");

        /// <summary>
        /// Returns a collection of MRTK Core directories found in the project.
        /// </summary>
        /// <remarks>
        /// File/Folder paths returned are absolute, not relative
        /// </remarks>
        public static IEnumerable<string> MRTKDirectories => GetDirectories(MixedRealityToolkitModuleType.Core);

        /// <summary>
        /// Get list of discovered directories for provided module type
        /// </summary>
        /// <param name="module">Module type to filter against</param>
        /// <returns>string list of discovered directory paths</returns>
        /// <remarks>
        /// File/Folder paths returned are absolute, not relative
        /// </remarks>
        public static IEnumerable<string> GetDirectories(MixedRealityToolkitModuleType module)
        {
            if (mrtkFolders.TryGetValue(module, out HashSet<string> folders))
            {
                return folders;
            }
            return null;
        }

        /// <summary>
        /// Are any of the MRTK directories available?
        /// </summary>
        /// <remarks>
        /// If a search is currently in progress, then property will wait synchronously for the task to finish
        /// </remarks>
        public static bool AreFoldersAvailable
        {
            get
            {
                searchForFoldersTask.Wait();
                return mrtkFolders.Count > 0;
            }
        }

        static MixedRealityToolkitFiles()
        {
            string path = Application.dataPath;
            searchForFoldersTask = SearchForFoldersAsync(path);
        }

        /// <summary>
        /// Force refresh of MRTK tracked folders
        /// </summary>
        /// <remarks>
        /// Kicks off async refresh of the MRTK folder database.
        /// </remarks>
        public static void RefreshFolders()
        {
            searchForFoldersTask.Wait();
            searchForFoldersTask = SearchForFoldersAsync(Application.dataPath);
        }

        /// <summary>
        /// Returns files from all folder instances of the core MRTK folder relative path.
        /// </summary>
        /// <param name="mrtkRelativeFolder">The core MRTK folder relative path to the target folder.</param>
        /// <returns>The array of files.</returns>
        public static string[] GetFiles(string mrtkRelativeFolder)
        {
            return GetFiles(MixedRealityToolkitModuleType.Core, mrtkRelativeFolder);
        }

        /// <summary>
        /// Returns files from all folder instances of the MRTK folder relative path.
        /// </summary>
        /// <param name="mrtkRelativeFolder">The MRTK folder relative path to the target folder.</param>
        /// <returns>The array of files.</returns>
        public static string[] GetFiles(MixedRealityToolkitModuleType module, string mrtkRelativeFolder)
        {
            if (!AreFoldersAvailable)
            {
                Debug.LogError("Failed to locate MixedRealityToolkit folders in the project.");
                return null;
            }

            if (mrtkFolders.TryGetValue(module, out HashSet<string> modFolders))
            {
                return modFolders
                    .Select(t => Path.Combine(t, mrtkRelativeFolder))
                    .Where(Directory.Exists)
                    .SelectMany(t => Directory.GetFiles(t))
                    .Select(GetAssetDatabasePath)
                    .ToArray();
            }
            return null;
        }

        /// <summary>
        /// Maps a single relative path file to a concrete path from one of the core MRTK folders, if found. Otherwise returns null.
        /// </summary>
        /// <param name="mrtkPathToFile">The core MRTK folder relative path to the file.</param>
        /// <returns>The project relative path to the file.</returns>
        public static string MapRelativeFilePath(string mrtkPathToFile)
        {
            return MapRelativeFilePath(MixedRealityToolkitModuleType.Core, mrtkPathToFile);
        }

        /// <summary>
        /// Maps a single relative path file to a concrete path from one of the MRTK folders, if found. Otherwise returns null.
        /// </summary>
        /// <param name="mrtkPathToFile">The MRTK folder relative path to the file.</param>
        /// <returns>The project relative path to the file.</returns>
        public static string MapRelativeFilePath(MixedRealityToolkitModuleType module, string mrtkPathToFile)
        {
            string absolutePath = MapRelativeFilePathToAbsolutePath(module, mrtkPathToFile);
            return absolutePath != null ? GetAssetDatabasePath(absolutePath) : null;
        }

        /// <summary>
        /// Maps a single relative path file to MRTK folders to its absolute path, if found. Otherwise returns null.
        /// </summary>
        /// <remarks>
        /// For example, this will map "Inspectors\Data\EditorWindowOptions.json" to its full path like
        /// "c:\project\Assets\Libs\MRTK\MixedRealityToolkit\Inspectors\Data\EditorWindowOptions.json".
        /// This assumes that the passed in mrtkPathToFile is found under the "MixedRealityToolkit" folder
        /// (instead of the MixedRealityToolkit.SDK, or any of the other folders).
        /// </remarks>
        public static string MapRelativeFilePathToAbsolutePath(string mrtkPathToFile)
        {
            return MapRelativeFilePathToAbsolutePath(MixedRealityToolkitModuleType.Core, mrtkPathToFile);
        }

        /// <summary>
        /// Overload of MapRelativeFilePathToAbsolutePath which provides the ability to specify the module that the
        /// file belongs to.
        /// </summary>
        /// <remarks>
        /// When searching for a resource that lives in the MixedRealityToolkit.SDK folder, this could be invoked
        /// in this way:
        /// MapRelativeFilePathToAbsolutePath(MixedRealityToolkitModuleType.SDK, mrtkPathToFile)
        /// </remarks>
        public static string MapRelativeFilePathToAbsolutePath(MixedRealityToolkitModuleType module, string mrtkPathToFile)
        {
            return MapRelativePathToAbsolutePath(SearchType.File, module, mrtkPathToFile);
        }

        /// <summary>
        /// Similar to MapRelativeFilePathToAbsolutePath, except this checks for the existence of a folder instead of file.
        /// </summary>
        /// <remarks>
        /// Returns first valid path found
        /// </remarks>
        public static string MapRelativeFolderPathToAbsolutePath(MixedRealityToolkitModuleType module, string mrtkPathToFolder)
        {
            return MapRelativePathToAbsolutePath(SearchType.Folder, module, mrtkPathToFolder);
        }

        /// <summary>
        /// Get the relative asset folder path to the provided Module type
        /// </summary>
        /// <param name="module">Module type to search for</param>
        /// <remarks>
        /// Returns first valid module folder path (relative) found
        /// </remarks>
        public static string MapModulePath(MixedRealityToolkitModuleType module)
        {
            return GetAssetDatabasePath(MapRelativeFolderPathToAbsolutePath(module, ""));
        }

        /// <summary>
        /// Finds the module type, if found, from the specified package folder name.
        /// </summary>
        /// <param name="packageFolder">The asset folder name (ex: MixedRealityToolkit.Providers)</param>
        /// <returns>
        /// <see cref="MixedRealityToolkitModuleType"/> associated with the package folder name. Returns
        /// MixedRealityToolkitModuleType.None if an appropriate module type could not be found.
        /// </returns>
        public static MixedRealityToolkitModuleType GetModuleFromPackageFolder(string packageFolder)
        {
            if (!packageFolder.StartsWith("MixedRealityToolkit"))
            {
                // There are no mappings for folders that do not start with "MixedRealityToolkit"
                return MixedRealityToolkitModuleType.None;
            }

            int separatorIndex = packageFolder.IndexOf('.');
            packageFolder = (separatorIndex != -1) ? packageFolder.Substring(separatorIndex + 1) : "Core";

            MixedRealityToolkitModuleType moduleType;
            return moduleNameMap.TryGetValue(packageFolder, out moduleType) ? moduleType : MixedRealityToolkitModuleType.None;
        }

        private static async Task SearchForFoldersAsync(string rootPath)
        {
            await Task.Run(() => SearchForFolders(rootPath));
        }

        private static void SearchForFolders(string rootPath)
        {
            try
            {
                var filePathResults = Directory.GetFiles(rootPath, SentinelFilePattern, SearchOption.AllDirectories);
                foreach (var sentinelFilePath in filePathResults)
                {
                    TryRegisterModuleViaFile(sentinelFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private static void TryRegisterModuleViaFile(string filePath)
        {
            MixedRealityToolkitModuleType moduleType = GetModuleType(filePath);
            if (moduleType != MixedRealityToolkitModuleType.None)
            {
                string folderPath = Path.GetDirectoryName(filePath);

                RegisterFolderToModule(folderPath, moduleType);
            }
        }

        private static void RegisterFolderToModule(string folderPath, MixedRealityToolkitModuleType module)
        {
            string normalizedFolder = NormalizeSeparators(folderPath);
            if (!mrtkFolders.TryGetValue(module, out HashSet<string> modFolders))
            {
                modFolders = new HashSet<string>();
                mrtkFolders.Add(module, modFolders);
            }

            modFolders.Add(normalizedFolder);

            if (module == MixedRealityToolkitModuleType.Core)
            {
                TryToCreateGeneratedFolder(folderPath);
            }
        }

        private static void TryToCreateGeneratedFolder(string folderPath)
        {
            string parentFolderPath = Directory.GetParent(folderPath).FullName;
            string generatedFolderPath = parentFolderPath + "\\" + "MixedRealityToolkit.Generated";
            if (!Directory.Exists(generatedFolderPath))
            {
                Directory.CreateDirectory(generatedFolderPath);
            }

            string generatedSentinelFilePath = generatedFolderPath + "\\" + "MRTK.Generated.sentinel";
            if (!File.Exists(generatedSentinelFilePath))
            {
                // Make sure we create and dispose/close the filestream just created
                using (var f = File.Create(generatedSentinelFilePath)) { }
            }

            TryRegisterModuleViaFile(generatedSentinelFilePath);
        }

        private static bool TryUnregisterModuleFolder(string folderPath)
        {
            string normalizedFolder = NormalizeSeparators(folderPath);
            bool found = false;
            foreach (var modFolders in mrtkFolders)
            {
                if (modFolders.Value.Remove(normalizedFolder))
                {
                    if (modFolders.Value.Count == 0)
                    {
                        mrtkFolders.Remove(modFolders.Key);
                    }
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Maps a single relative path (file or folder) in MRTK folders to its absolute path, if found.
        /// Otherwise returns null.
        /// </summary>
        private static string MapRelativePathToAbsolutePath(SearchType searchType, MixedRealityToolkitModuleType module, string mrtkPath)
        {
            if (!AreFoldersAvailable)
            {
                Debug.LogError("Failed to locate MixedRealityToolkit folders in the project.");
                return null;
            }

            if (mrtkFolders.TryGetValue(module, out HashSet<string> modFolders))
            {
                string path = modFolders
                    .Select(t => Path.Combine(t, mrtkPath))
                    .FirstOrDefault(t => searchType == SearchType.File ? File.Exists(t) : Directory.Exists(t));
                return path;
            }
            return null;
        }

        /// <summary>
        /// Given the full file path, returns the module it's associated with (if it is an MRTK sentinel file)
        /// </summary>
        private static MixedRealityToolkitModuleType GetModuleType(string filePath)
        {
            const string sentinelRegexPattern = @"^MRTK\.(?<module>[a-zA-Z]+)\.sentinel";
            string fileName = Path.GetFileName(filePath);
            var matches = Regex.Matches(fileName, sentinelRegexPattern);
            if (matches.Count == 1)
            {
                var moduleName = matches[0].Groups["module"].Value;
                MixedRealityToolkitModuleType moduleType;
                if (moduleNameMap.TryGetValue(moduleName, out moduleType))
                {
                    return moduleType;
                }
            }
            return MixedRealityToolkitModuleType.None;
        }

        private static bool IsSentinelFile(string assetPath)
        {
            return Regex.IsMatch(Path.GetFileName(assetPath), SentinelFilePattern);
        }

        /// <summary>
        /// Resolves the given asset to its full path if and only if the asset belongs to the
        /// Assets folder (i.e. it is prefixed with "Assets/..."
        /// </summary>
        /// <remarks>
        /// If not associated with the Assets folder, will return the path unchanged.
        /// </remarks>
        private static string ResolveFullAssetsPath(string path)
        {
            if (path.StartsWith("Assets"))
            {
                // asset.Substring(6) represents the characters after the "Assets" string.
                return Application.dataPath + path.Substring(6);
            }
            return path;
        }
    }
}
