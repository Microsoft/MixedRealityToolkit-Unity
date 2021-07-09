// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using IOFileInfo = System.IO.FileInfo;
// This using exists to work around naming conflicts described in:
// https://github.com/microsoft/MixedRealityToolkit-Unity/issues/8104
// If the internal rest class gets renamed this workaround can be removed.
using RestHelpers = Microsoft.MixedReality.Toolkit.Utilities;

namespace Microsoft.MixedReality.Toolkit.WindowsDevicePortal
{
    /// <summary>
    /// Function used to communicate with Windows 10 devices through the device portal REST APIs.
    /// </summary>
    public static class DevicePortal
    {
        /// <summary>
        /// Use SSL Connections when making rest calls.
        /// </summary>
        public static bool UseSSL { get; set; } = true;

        /// <summary>
        /// Use SSL Certificate Verification when making SSL rest calls.
        /// </summary>
        public static bool VerifySSLCertificates { get; set; } = true;

        private enum AppInstallStatus
        {
            Invalid,
            Installing,
            InstallSuccess,
            InstallFail
        }

        /// <summary>
        /// Custom certificate handler for device portal request.
        /// The device portal on HoloLens uses a self-signed certificate, therefore SSL Unity WebRequest will fail.
        /// As a fix we simply accept all certificates, including self-signed, without further checking, 
        /// as they do not chain up to any Microsoft Root Certificate.
        /// </summary>
        private class BlankCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Accept all the certificates!
                return true;
            }
        }

        private static readonly BlankCertificateHandler BlankCertificateHandlerInstance = new BlankCertificateHandler();

        private static CertificateHandler DevicePortalCertificateHandler
        {
            get { return !VerifySSLCertificates ? BlankCertificateHandlerInstance : null; }
        }

        // Device Portal API Resources
        // https://docs.microsoft.com/windows/uwp/debug-test-perf/device-portal-api-hololens#holographic-os
        // https://docs.microsoft.com/windows/uwp/debug-test-perf/device-portal-api-core
        private const string GetDeviceOsInfoQuery = @"{0}/api/os/info";
        private const string GetMachineNameQuery = @"{0}/api/os/machinename";
        private const string GetBatteryQuery = @"{0}/api/power/battery";
        private const string GetPowerStateQuery = @"{0}/api/power/state";
        private const string RestartDeviceQuery = @"{0}/api/control/restart";
        private const string ShutdownDeviceQuery = @"{0}/api/control/shutdown";
        private const string ProcessQuery = @"{0}/api/resourcemanager/processes";
        private const string AppQuery = @"{0}/api/taskmanager/app";
        private const string PackagesQuery = @"{0}/api/appx/packagemanager/packages";
        private const string InstallQuery = @"{0}/api/app/packagemanager/package";
        private const string InstallStatusQuery = @"{0}/api/app/packagemanager/state";
        private const string FileQuery = @"{0}/api/filesystem/apps/file?knownfolderid=LocalAppData&filename=UnityPlayer.log&packagefullname={1}&path=%5C%5CTempState";
        private const string IpConfigQuery = @"{0}/api/networking/ipconfig";
        private const string WiFiNetworkQuery = @"{0}/api/wifi/network{1}";
        private const string WiFiInterfacesQuery = @"{0}/api/wifi/interfaces";

#if !UNITY_WSA || UNITY_EDITOR
        /// <summary>
        /// Opens the Device Portal for the target device.
        /// </summary>
        public static void OpenWebPortal(DeviceInfo targetDevice)
        {
            System.Diagnostics.Process.Start(FinalizeUrl(targetDevice.IP));
        }
#endif

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.DeviceOsInfo"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.DeviceOsInfo"/></returns>
        public static async Task<DeviceOsInfo> GetDeviceOsInfoAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(GetDeviceOsInfoQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetDeviceOsInfoAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<DeviceOsInfo>(await response.GetResponseBody());
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.MachineName"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.MachineName"/></returns>
        public static async Task<MachineName> GetMachineNameAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(GetMachineNameQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetMachineNameAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<MachineName>( await response.GetResponseBody());
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.BatteryInfo"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.BatteryInfo"/></returns>
        public static async Task<BatteryInfo> GetBatteryStateAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(GetBatteryQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetBatteryStateAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<BatteryInfo>(await response.GetResponseBody());
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.PowerStateInfo"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.PowerStateInfo"/></returns>
        public static async Task<PowerStateInfo> GetPowerStateAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(GetPowerStateQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    await GetPowerStateAsync(targetDevice);
                }
                return null;
            }

            return JsonUtility.FromJson<PowerStateInfo>(await response.GetResponseBody());
        }

        /// <summary>
        /// Restart the target device.
        /// </summary>
        /// <returns>True, if the device has successfully restarted.</returns>
        public static async Task<bool> RestartAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return false; }

            var response = await RestHelpers.Rest.PostAsync(string.Format(RestartDeviceQuery, FinalizeUrl(targetDevice.IP)), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (response.Successful)
            {
                bool hasRestarted = false;
                string query = string.Format(GetPowerStateQuery, FinalizeUrl(targetDevice.IP));

                while (!hasRestarted)
                {
                    response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

                    if (!response.Successful)
                    {
                        if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                        {
                            continue;
                        }

                        Debug.LogError(await response.GetResponseBody());
                        return false;
                    }

                    hasRestarted = response.Successful;
                }

                return true;
            }

            if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
            {
                await RestartAsync(targetDevice);
            }

            Debug.LogError(await response.GetResponseBody());
            return false;
        }

        /// <summary>
        /// Shuts down the target device.
        /// </summary>
        /// <returns>True, if the device is shutting down.</returns>
        public static async Task<bool> ShutdownAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return false; }

            var response = await RestHelpers.Rest.PostAsync(string.Format(ShutdownDeviceQuery, FinalizeUrl(targetDevice.IP)), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await ShutdownAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the target application is currently running on the target device.
        /// </summary>
        /// <returns>True, if application is currently installed on device.</returns>
        public static async Task<bool> IsAppInstalledAsync(string packageName, DeviceInfo targetDevice)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));
            return await GetApplicationInfoAsync(packageName, targetDevice) != null;
        }

        /// <summary>
        /// Determines if the target application is running on the target device.
        /// </summary>
        /// <param name="appInfo">Optional cached <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/>.</param>
        /// <returns>True, if the application is running.</returns>
        public static async Task<bool> IsAppRunningAsync(string packageName, DeviceInfo targetDevice, ApplicationInfo appInfo = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));

            if (appInfo == null)
            {
                appInfo = await GetApplicationInfoAsync(packageName, targetDevice);
            }

            if (appInfo == null)
            {
                Debug.LogError($"{packageName} not installed.");
                return false;
            }

            var response = await RestHelpers.Rest.GetAsync(string.Format(ProcessQuery, FinalizeUrl(targetDevice.IP)), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (response.Successful)
            {
                var processList = JsonUtility.FromJson<ProcessList>(await response.GetResponseBody());
                for (int i = 0; i < processList.Processes.Length; ++i)
                {
                    if (processList.Processes[i].ImageName.Contains(appInfo.Name))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
            {
                return await IsAppRunningAsync(packageName, targetDevice, appInfo);
            }

            Debug.LogError($"{await response.GetResponseBody()}");
            return false;
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/> of the target application on the target device.
        /// </summary>
        /// <returns>Returns the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/> of the target application from the target device.</returns>
        private static async Task<ApplicationInfo> GetApplicationInfoAsync(string packageName, DeviceInfo targetDevice)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));
            var appList = await GetAllInstalledAppsAsync(targetDevice);

            for (int i = 0; i < appList?.InstalledPackages.Length; ++i)
            {
                if (appList.InstalledPackages[i].PackageFullName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return appList.InstalledPackages[i];
                }

                if (appList.InstalledPackages[i].PackageFamilyName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return appList.InstalledPackages[i];
                }
            }

            return null;
        }

        public static async Task<InstalledApps> GetAllInstalledAppsAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            var response = await RestHelpers.Rest.GetAsync(string.Format(PackagesQuery, FinalizeUrl(targetDevice.IP)), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetAllInstalledAppsAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<InstalledApps>(await response.GetResponseBody());
        }

        /// <summary>
        /// Installs the target application on the target device.
        /// </summary>
        /// <param name="waitForDone">Should the thread wait until installation is complete?</param>
        /// <returns>True, if Installation was a success.</returns>
        public static async Task<bool> InstallAppAsync(string appFullPath, DeviceInfo targetDevice, bool waitForDone = true)
        {
            Debug.Assert(!string.IsNullOrEmpty(appFullPath));
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth)
            {
                return false;
            }

            Debug.Log($"Starting app install on {targetDevice.ToString()}...");

            // Calculate the cert and dependency paths
            string fileName = Path.GetFileName(appFullPath);
            string certFullPath = Path.ChangeExtension(appFullPath, ".cer");
            string certName = Path.GetFileName(certFullPath);

            string arch = "ARM";
            if (appFullPath.Contains("x86"))
            {
                arch = "x86";
            }
            else if (appFullPath.Contains("ARM64"))
            {
                arch = "ARM64";
            }

            string depPath = $@"{Path.GetDirectoryName(appFullPath)}\Dependencies\{arch}\";

            var form = new WWWForm();

            try
            {
                // APPX file
                Debug.Assert(appFullPath != null);
                using (var stream = new FileStream(appFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        form.AddBinaryData(fileName, reader.ReadBytes((int)reader.BaseStream.Length), fileName);
                    }
                }

                // CERT file
                Debug.Assert(certFullPath != null);
                using (var stream = new FileStream(certFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        form.AddBinaryData(certName, reader.ReadBytes((int)reader.BaseStream.Length), certName);
                    }
                }

                // Dependencies
                IOFileInfo[] depFiles = new DirectoryInfo(depPath).GetFiles();
                foreach (IOFileInfo dep in depFiles)
                {
                    using (var stream = new FileStream(dep.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            string depFilename = Path.GetFileName(dep.FullName);
                            form.AddBinaryData(depFilename, reader.ReadBytes((int)reader.BaseStream.Length), depFilename);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            // Query
            string query = $"{string.Format(InstallQuery, FinalizeUrl(targetDevice.IP))}?package={UnityWebRequest.EscapeURL(fileName)}";

            var response = await RestHelpers.Rest.PostAsync(query, form, targetDevice.Authorization, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await InstallAppAsync(appFullPath, targetDevice, waitForDone);
                }

                Debug.LogError($"Failed to install {fileName} on {targetDevice.ToString()}.");
                return false;
            }

            var status = AppInstallStatus.Installing;

            // Wait for done (if requested)
            while (waitForDone && status == AppInstallStatus.Installing)
            {
                status = await GetInstallStatusAsync(targetDevice);

                switch (status)
                {
                    case AppInstallStatus.InstallSuccess:
                        Debug.Log($"Successfully installed {fileName} on {targetDevice.ToString()}.");
                        return true;
                    case AppInstallStatus.InstallFail:
                        Debug.LogError($"Failed to install {fileName} on {targetDevice.ToString()}.");
                        return false;
                }
            }

            return true;
        }

        private static async Task<AppInstallStatus> GetInstallStatusAsync(DeviceInfo targetDevice)
        {
            var response = await RestHelpers.Rest.GetAsync(string.Format(InstallStatusQuery, FinalizeUrl(targetDevice.IP)), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (response.Successful)
            {
                var status = JsonUtility.FromJson<InstallStatus>(await response.GetResponseBody());

                if (status == null)
                {
                    return AppInstallStatus.Installing;
                }

                if (status.Success)
                {
                    return AppInstallStatus.InstallSuccess;
                }

                Debug.LogError($"{status.Reason}\n{status.CodeText}");
            }
            else
            {
                return AppInstallStatus.Installing;
            }

            return AppInstallStatus.InstallFail;
        }

        /// <summary>
        /// Uninstalls the target application on the target device
        /// </summary>
        /// <param name="appInfo">Optional cached <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/>.</param>
        /// <returns>True, if uninstall was a success.</returns>
        public static async Task<bool> UninstallAppAsync(string packageName, DeviceInfo targetDevice, ApplicationInfo appInfo = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));

            if (appInfo == null)
            {
                appInfo = await GetApplicationInfoAsync(packageName, targetDevice);
            }

            if (appInfo == null)
            {
                Debug.LogWarning($"Application '{packageName}' not found");
                return false;
            }

            Debug.Log($"Attempting to uninstall {packageName} on {targetDevice.ToString()}...");

            string query = $"{string.Format(InstallQuery, FinalizeUrl(targetDevice.IP))}?package={UnityWebRequest.EscapeURL(appInfo.PackageFullName)}";
            var response = await RestHelpers.Rest.DeleteAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (response.Successful)
            {
                Debug.Log($"Successfully uninstalled {packageName} on {targetDevice.ToString()}.");
            }
            else
            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await UninstallAppAsync(packageName, targetDevice);
                }

                Debug.LogError($"Failed to uninstall {packageName} on {targetDevice.ToString()}");
                Debug.LogError(await response.GetResponseBody());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Launches the target application on the target device.
        /// </summary>
        /// <param name="appInfo">Optional cached <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/>.</param>
        /// <returns>True, if application was successfully launched and is currently running on the target device.</returns>
        public static async Task<bool> LaunchAppAsync(string packageName, DeviceInfo targetDevice, ApplicationInfo appInfo = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));

            if (appInfo == null)
            {
                appInfo = await GetApplicationInfoAsync(packageName, targetDevice);
            }

            if (appInfo == null)
            {
                Debug.LogWarning($"Application '{packageName}' not found");
                return false;
            }

            string query = $"{string.Format(AppQuery, FinalizeUrl(targetDevice.IP))}?appid={UnityWebRequest.EscapeURL(appInfo.PackageRelativeId.EncodeTo64())}&package={UnityWebRequest.EscapeURL(appInfo.PackageFullName)}";
            var response = await RestHelpers.Rest.PostAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await LaunchAppAsync(packageName, targetDevice);
                }

                Debug.LogError($"{response.ResponseCode}|{await response.GetResponseBody()}");
                return false;
            }

            while (!await IsAppRunningAsync(packageName, targetDevice, appInfo))
            {
                await new WaitForSeconds(1f);
            }

            return true;
        }

        /// <summary>
        /// Stops the target application on the target device.
        /// </summary>
        /// <param name="appInfo">Optional cached <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/>.</param>
        /// <returns>true, if application was successfully stopped.</returns>
        public static async Task<bool> StopAppAsync(string packageName, DeviceInfo targetDevice, ApplicationInfo appInfo = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));

            if (appInfo == null)
            {
                appInfo = await GetApplicationInfoAsync(packageName, targetDevice);
            }

            if (appInfo == null)
            {
                Debug.LogWarning($"Application '{packageName}' not found");
                return false;
            }

            string query = $"{string.Format(AppQuery, FinalizeUrl(targetDevice.IP))}?package={UnityWebRequest.EscapeURL(appInfo.PackageFullName.EncodeTo64())}";
            Response response = await RestHelpers.Rest.DeleteAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await StopAppAsync(packageName, targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return false;
            }

            while (!await IsAppRunningAsync(packageName, targetDevice, appInfo))
            {
                await new WaitForSeconds(1f);
            }

            return true;
        }

        /// <summary>
        /// Downloads and launches the Log file for the target application on the target device.
        /// </summary>
        /// <param name="appInfo">Optional cached <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.ApplicationInfo"/>.</param>
        /// <returns>The path of the downloaded log file.</returns>
        public static async Task<string> DownloadLogFileAsync(string packageName, DeviceInfo targetDevice, ApplicationInfo appInfo = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));

            if (appInfo == null)
            {
                appInfo = await GetApplicationInfoAsync(packageName, targetDevice);
            }

            if (appInfo == null)
            {
                Debug.LogWarning($"Application '{packageName}' not found");
                return string.Empty;
            }

            string logFile = $"{Application.temporaryCachePath}/{targetDevice.MachineName}_{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}_player.txt";
            var response = await RestHelpers.Rest.GetAsync(string.Format(FileQuery, FinalizeUrl(targetDevice.IP), appInfo.PackageFullName), targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await DownloadLogFileAsync(packageName, targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return string.Empty;
            }

            File.WriteAllText(logFile, await response.GetResponseBody());
            return logFile;

        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.IpConfigInfo"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.IpConfigInfo"/></returns>
        public static async Task<IpConfigInfo> GetIpConfigInfoAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(IpConfigQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetIpConfigInfoAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<IpConfigInfo>(await response.GetResponseBody());
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.AvailableWiFiNetworks"/> of the target device.
        /// </summary>
        /// <param name="interfaceInfo">The GUID for the network interface to use to search for wireless networks, without brackets.</param>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.AvailableWiFiNetworks"/></returns>
        public static async Task<AvailableWiFiNetworks> GetAvailableWiFiNetworksAsync(DeviceInfo targetDevice, InterfaceInfo interfaceInfo)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(WiFiNetworkQuery, FinalizeUrl(targetDevice.IP), $"s?interface={interfaceInfo.GUID}");
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetAvailableWiFiNetworksAsync(targetDevice, interfaceInfo);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<AvailableWiFiNetworks>(await response.GetResponseBody());
        }

        /// <summary>
        /// Connects to the specified WiFi Network.
        /// </summary>
        /// <param name="interfaceInfo">The interface to use to connect.</param>
        /// <param name="wifiNetwork">The network to connect to.</param>
        /// <param name="password">Password for network access.</param>
        /// <returns>True, if connection successful.</returns>
        public static async Task<Response> ConnectToWiFiNetworkAsync(DeviceInfo targetDevice, InterfaceInfo interfaceInfo, WirelessNetworkInfo wifiNetwork, string password)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return new Response(false, "Unable to authenticate with device", null, 403); }

            string query = string.Format(
                WiFiNetworkQuery,
                FinalizeUrl(targetDevice.IP),
                $"?interface={interfaceInfo.GUID}&ssid={wifiNetwork.SSID.EncodeTo64()}&op=connect&createprofile=yes&key={password}");
            return await RestHelpers.Rest.PostAsync(query, targetDevice.Authorization, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.NetworkInterfaces"/> of the target device.
        /// </summary>
        /// <returns><see cref="Microsoft.MixedReality.Toolkit.WindowsDevicePortal.NetworkInterfaces"/></returns>
        public static async Task<NetworkInterfaces> GetWiFiNetworkInterfacesAsync(DeviceInfo targetDevice)
        {
            var isAuth = await EnsureAuthenticationAsync(targetDevice);
            if (!isAuth) { return null; }

            string query = string.Format(WiFiInterfacesQuery, FinalizeUrl(targetDevice.IP));
            var response = await RestHelpers.Rest.GetAsync(query, targetDevice.Authorization, readResponseData: true, certificateHandler: DevicePortalCertificateHandler, disposeCertificateHandlerOnDispose: false);

            if (!response.Successful)
            {
                if (response.ResponseCode == 403 && await RefreshCsrfTokenAsync(targetDevice))
                {
                    return await GetWiFiNetworkInterfacesAsync(targetDevice);
                }

                Debug.LogError(await response.GetResponseBody());
                return null;
            }

            return JsonUtility.FromJson<NetworkInterfaces>(await response.GetResponseBody());
        }

        /// <summary>
        /// This Utility method finalizes the URL and formats the HTTPS string if needed.
        /// </summary>
        /// <remarks>Local Machine will be changed to 127.0.0.1:10080 for HoloLens connections.</remarks>
        /// <param name="targetUrl">The target URL i.e. 128.128.128.128</param>
        /// <returns>The finalized URL with http/https prefix.</returns>
        public static string FinalizeUrl(string targetUrl)
        {
            string ssl = DevicePortal.UseSSL ? "s" : string.Empty;

            if (targetUrl.Contains(DeviceInfo.LocalMachine) || targetUrl.Contains(DeviceInfo.LocalIPAddress))
            {
                targetUrl = UseSSL ? $"{DeviceInfo.LocalIPAddress}:10443" : $"{DeviceInfo.LocalIPAddress}:10080";
            }

            return $@"http{ssl}://{targetUrl}";
        }

        /// <summary>
        /// Refreshes the CSRF Token in case the device or its portal was restarted.
        /// </summary>
        /// <returns>True, if refresh was successful.</returns>
        public static async Task<bool> RefreshCsrfTokenAsync(DeviceInfo targetDevice)
        {
            if (!targetDevice.Authorization.ContainsKey("cookie"))
            {
                Debug.LogError("Resetting Auth failed!");
                return false;
            }

            targetDevice.Authorization.Remove("cookie");

            return await EnsureAuthenticationAsync(targetDevice);
        }

        /// <summary>
        /// Makes sure the Authentication Headers and CSRF Tokens are set.
        /// </summary>
        /// <returns>True if Authentication is successful, otherwise false.</returns>
        public static async Task<bool> EnsureAuthenticationAsync(DeviceInfo targetDevice)
        {
            targetDevice.Authorization["Authorization"] = RestHelpers.Rest.GetBasicAuthentication(targetDevice.User, targetDevice.Password);

            bool success;

            if (!targetDevice.Authorization.ContainsKey("cookie"))
            {
                var response = await DevicePortalAuthorizationAsync(targetDevice);
                success = response.Successful;

                if (success)
                {
                    // If null, authentication succeeded but we had no cookie token in the response.
                    // This usually means Unity has a cached token, so it can be ignored.
                    if (await response.GetResponseBody() != null)
                    {
                        targetDevice.CsrfToken = await response.GetResponseBody();

                        // Strip the beginning of the cookie header
                        targetDevice.CsrfToken = targetDevice.CsrfToken.Replace("CSRF-Token=", string.Empty);
                    }
                }
                else
                {
                    Debug.LogError($"Authentication failed! {await response.GetResponseBody()}");
                }

                if (!string.IsNullOrEmpty(targetDevice.CsrfToken))
                {
                    if (!targetDevice.Authorization.ContainsKey("cookie"))
                    {
                        targetDevice.Authorization.Add("cookie", targetDevice.CsrfToken);
                    }
                    else
                    {
                        targetDevice.Authorization["cookie"] = targetDevice.CsrfToken;
                    }

                    if (targetDevice.Authorization.ContainsKey("x-csrf-token"))
                    {
                        targetDevice.Authorization["x-csrf-token"] = targetDevice.CsrfToken;
                    }
                    else
                    {
                        targetDevice.Authorization.Add("x-csrf-token", targetDevice.CsrfToken);
                    }
                }
            }
            else
            {
                success = true;
            }

            return success;
        }

        private static async Task<Response> DevicePortalAuthorizationAsync(DeviceInfo targetDevice)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(FinalizeUrl(targetDevice.IP));

            webRequest.timeout = 5;
            webRequest.certificateHandler = DevicePortalCertificateHandler;
            webRequest.disposeCertificateHandlerOnDispose = false;
            webRequest.SetRequestHeader("Authorization", targetDevice.Authorization["Authorization"]);

            await webRequest.SendWebRequest();

            long responseCode = webRequest.responseCode;
#if UNITY_2020_1_OR_NEWER
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
            if (webRequest.isNetworkError || webRequest.isHttpError)
#endif // UNITY_2020_1_OR_NEWER
            {
                if (responseCode == 401)
                {
                    return new Response(false, "Invalid Credentials", null, responseCode);
                }

                if (webRequest.GetResponseHeaders() == null)
                {
                    return new Response(false, "Device Not Found | No Response Headers", null, responseCode);
                }

                string responseHeaders = webRequest.GetResponseHeaders().Aggregate(string.Empty, (current, header) => $"\n{header.Key}: {header.Value}");

                byte[] downloadHandlerData = webRequest.downloadHandler?.data;
                string downloadHandlerText = await ResponseUtils.BytesToString(downloadHandlerData);

                Debug.LogError($"REST Auth Error: {responseCode}\n{downloadHandlerText}{responseHeaders}");
                return new Response(false, $"{downloadHandlerText}", downloadHandlerData, responseCode);
            }

            return new Response(true, new Task<string>(() => webRequest.GetResponseHeader("Set-Cookie")), () => webRequest.downloadHandler?.data, responseCode);
        }
    }
}
