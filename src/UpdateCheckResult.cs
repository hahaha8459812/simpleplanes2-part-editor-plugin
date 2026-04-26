namespace SimplePlanes2PartEditor
{
    internal sealed class UpdateCheckResult
    {
        public UpdateCheckResult(bool hasUpdate, string latestVersion, string releaseNotes)
        {
            HasUpdate = hasUpdate;
            LatestVersion = latestVersion ?? string.Empty;
            ReleaseNotes = releaseNotes ?? string.Empty;
        }

        public bool HasUpdate { get; private set; }

        public string LatestVersion { get; private set; }

        public string ReleaseNotes { get; private set; }
    }
}
