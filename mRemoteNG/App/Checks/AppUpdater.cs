using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using mRemoteNG.App.Info;
using mRemoteNG.Security.SymmetricEncryption;
using System.Threading.Tasks;
using mRemoteNG.Properties;
using System.Runtime.Versioning;
// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.App.Update
{
    [SupportedOSPlatform("windows")]
    public class AppUpdater
    {
        private WebProxy? _webProxy;
        private HttpClient? _httpClient;
        private CancellationTokenSource? _changeLogCancelToken;
        private CancellationTokenSource? _getUpdateInfoCancelToken;

        #region Public Properties

        public UpdateInfo? CurrentUpdateInfo { get; private set; }

        public bool IsGetUpdateInfoRunning
        {
            get
            {
                return _getUpdateInfoCancelToken != null;
            }
        }

        private bool IsGetChangeLogRunning
        {
            get
            {
                return _changeLogCancelToken != null;
            }
        }

        #endregion

        #region Public Methods

        public AppUpdater()
        {
            SetDefaultProxySettings();
        }

        private void SetDefaultProxySettings()
        {
            bool shouldWeUseProxy = Properties.OptionsUpdatesPage.Default.UpdateUseProxy;
            string proxyAddress = Properties.OptionsUpdatesPage.Default.UpdateProxyAddress;
            int port = Properties.OptionsUpdatesPage.Default.UpdateProxyPort;
            bool useAuthentication = Properties.OptionsUpdatesPage.Default.UpdateProxyUseAuthentication;
            string username = Properties.OptionsUpdatesPage.Default.UpdateProxyAuthUser;
            LegacyRijndaelCryptographyProvider cryptographyProvider = new();
            string password = cryptographyProvider.Decrypt(Properties.OptionsUpdatesPage.Default.UpdateProxyAuthPass, Runtime.EncryptionKey);

            SetProxySettings(shouldWeUseProxy, proxyAddress, port, useAuthentication, username, password);
        }

        public void SetProxySettings(bool useProxy, string address, int port, bool useAuthentication, string username, string password)
        {
            if (useProxy && !string.IsNullOrEmpty(address))
            {
                _webProxy = port != 0 ? new WebProxy(address, port) : new WebProxy(address);
                _webProxy.Credentials = useAuthentication ? new NetworkCredential(username, password) : null;
            }
            else
            {
                _webProxy = null;
            }

            UpdateHttpClient();
        }

        public bool IsUpdateAvailable()
        {
            if (CurrentUpdateInfo == null || !CurrentUpdateInfo.IsValid)
            {
                return false;
            }

            return CurrentUpdateInfo.Version > GeneralAppInfo.GetApplicationVersion();
        }

        #endregion

        #region Private Methods

        private void UpdateHttpClient()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }

            HttpClientHandler httpClientHandler = new();
            if (_webProxy != null)
            {
                httpClientHandler.UseProxy = true;
                httpClientHandler.Proxy = _webProxy;
            }
            else
            {
                // Bypass Windows system proxy when no custom proxy is configured
                httpClientHandler.UseProxy = false;
            }
            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GeneralAppInfo.UserAgent);
        }

        public async Task GetUpdateInfoAsync()
        {
            if (IsGetUpdateInfoRunning)
            {
                _getUpdateInfoCancelToken!.Cancel();
                _getUpdateInfoCancelToken.Dispose();
                _getUpdateInfoCancelToken = null;
            }

            try
            {
                _getUpdateInfoCancelToken = new CancellationTokenSource();
                if (_httpClient == null)
                    throw new InvalidOperationException("HttpClient has not been initialized.");
                Uri updateUri = UpdateChannelInfo.GetUpdateChannelInfo();
                string updateInfo = await _httpClient.GetStringAsync(updateUri, _getUpdateInfoCancelToken.Token);
                CurrentUpdateInfo = UpdateInfo.FromGitHubJson(updateInfo);
                Properties.OptionsUpdatesPage.Default.CheckForUpdatesLastCheck = DateTime.UtcNow;

                if (!Properties.OptionsUpdatesPage.Default.UpdatePending)
                {
                    Properties.OptionsUpdatesPage.Default.UpdatePending = IsUpdateAvailable();
                }
            }
            finally
            {
                _getUpdateInfoCancelToken?.Dispose();
                _getUpdateInfoCancelToken = null;
            }
        }

        public async Task<string> GetChangeLogAsync()
        {
            if (IsGetChangeLogRunning)
            {
                _changeLogCancelToken!.Cancel();
                _changeLogCancelToken.Dispose();
                _changeLogCancelToken = null;
            }

            try
            {
                _changeLogCancelToken = new CancellationTokenSource();
                if (_httpClient == null)
                    throw new InvalidOperationException("HttpClient has not been initialized.");
                if (CurrentUpdateInfo == null)
                    throw new InvalidOperationException("CurrentUpdateInfo is not available. GetUpdateInfoAsync() must be called first.");
                return await _httpClient.GetStringAsync(CurrentUpdateInfo.ChangeLogAddress, _changeLogCancelToken.Token);
            }
            finally
            {
                _changeLogCancelToken?.Dispose();
                _changeLogCancelToken = null;
            }
        }

        #endregion
    }
}
