namespace Video
{
    /// <summary>
    /// URL para <see cref="UnityEngine.Video.VideoSource.Url"/> relativamente a StreamingAssets,
    /// alinhado a <see cref="FullscreenVideoOverlay"/>.
    /// </summary>
    public static class StreamingAssetsVideoUrl
    {
        public static string NormalizeRelativePath(string relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? string.Empty
                : relativePath.Trim().Replace('\\', '/');
        }

        /// <summary>Path absoluto (file:, ou raiz StreamingAssets conforme plataforma no build).</summary>
        public static string BuildAbsolute(string relativePath)
        {
            string root = UnityEngine.Application.streamingAssetsPath;
            relativePath = NormalizeRelativePath(relativePath);
            if (string.IsNullOrEmpty(relativePath))
                return relativePath;
            if (string.IsNullOrEmpty(root))
                return relativePath;

            bool rootSlash = root.EndsWith("/") || root.EndsWith("\\");
            return rootSlash ? root + relativePath : root + "/" + relativePath;
        }
    }
}
