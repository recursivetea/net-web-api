using System.Collections.Specialized;

namespace Net.Web.Api.Sdk.Properties
{
    /// <summary>
    /// Application settings for the SDK.
    /// </summary>
    internal sealed class Settings
    {
        private static readonly Settings _default = new Settings();

        public static Settings Default => _default;

        /// <summary>
        /// Gets the maximum allowed upload size in bytes (default: 2MB).
        /// </summary>
        public long MaxAllowedUploadSize => 2097152;

        /// <summary>
        /// Gets the allowed MIME types for file uploads.
        /// </summary>
        public StringCollection AllowedMimeTypes
        {
            get
            {
                var collection = new StringCollection();
                collection.AddRange(new[]
                {
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
                    "application/vnd.ms-excel",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
                    "application/vnd.ms-powerpoint",
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    "application/vnd.openxmlformats-officedocument.presentationml.template",
                    "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
                    "image/bmp",
                    "image/gif",
                    "image/jpeg",
                    "image/svg+xml",
                    "image/tiff",
                    "image/x-icon",
                    "image/png",
                    "application/json",
                    "application/pdf",
                    "text/plain",
                    "application/xml",
                    "text/xml"
                });
                return collection;
            }
        }
    }
}
