using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace SimplePlanes2PartEditor
{
    internal sealed class UpdateCheckService
    {
        private const int DownloadTimeoutMilliseconds = 8000;

        public void CheckForUpdates(string indexUrl, string currentVersion, Action<UpdateCheckResult> completed)
        {
            if (string.IsNullOrWhiteSpace(indexUrl) || string.IsNullOrWhiteSpace(currentVersion) || completed == null)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateCheckResult result = TryReadUpdateIndex(indexUrl, currentVersion);
                if (result != null)
                {
                    completed(result);
                }
            });
        }

        private static UpdateCheckResult TryReadUpdateIndex(string indexUrl, string currentVersion)
        {
            string json;
            Dictionary<string, string> values;
            string latestVersion;
            string releaseNotes;

            try
            {
                json = DownloadString(indexUrl);
                values = SimpleJson.ReadFlatObject(json);
                if (!values.TryGetValue("version", out latestVersion) || string.IsNullOrWhiteSpace(latestVersion))
                {
                    return null;
                }

                values.TryGetValue("releaseNotes", out releaseNotes);
                return new UpdateCheckResult(IsRemoteVersionNewer(latestVersion, currentVersion), latestVersion, releaseNotes);
            }
            catch
            {
                return null;
            }
        }

        private static string DownloadString(string url)
        {
            using (TimeoutWebClient client = new TimeoutWebClient(DownloadTimeoutMilliseconds))
            {
                client.Headers[HttpRequestHeader.UserAgent] = SimplePlanes2PartEditorPlugin.PluginName + "/" + SimplePlanes2PartEditorPlugin.PluginVersion;
                client.Headers[HttpRequestHeader.Accept] = "application/vnd.github.raw";
                client.Headers["X-GitHub-Api-Version"] = "2022-11-28";
                return client.DownloadString(url);
            }
        }

        private static bool IsRemoteVersionNewer(string remoteVersion, string currentVersion)
        {
            int compareResult;

            if (string.Equals(remoteVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (TryCompareVersions(remoteVersion, currentVersion, out compareResult))
            {
                return compareResult > 0;
            }

            return !string.Equals(remoteVersion.Trim(), currentVersion.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCompareVersions(string remoteVersion, string currentVersion, out int compareResult)
        {
            Version remote;
            Version current;

            compareResult = 0;
            if (!Version.TryParse(NormalizeVersion(remoteVersion), out remote) ||
                !Version.TryParse(NormalizeVersion(currentVersion), out current))
            {
                return false;
            }

            compareResult = remote.CompareTo(current);
            return true;
        }

        private static string NormalizeVersion(string version)
        {
            string trimmedVersion = (version ?? string.Empty).Trim();
            if (trimmedVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedVersion.Substring(1);
            }

            return trimmedVersion;
        }

        private sealed class TimeoutWebClient : WebClient
        {
            private readonly int _timeoutMilliseconds;

            public TimeoutWebClient(int timeoutMilliseconds)
            {
                _timeoutMilliseconds = timeoutMilliseconds;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null)
                {
                    request.Timeout = _timeoutMilliseconds;
                }

                return request;
            }
        }
    }
}
