using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ByteSizeLib;
using Microsoft.AspNetCore.Http;
using Net.Web.Api.Sdk.Properties;

namespace Net.Web.Api.Sdk.Attributes.Validations
{
    /// <summary>
    /// Validates an uploaded file for allowed MIME types and size limits.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UploadFileAttribute : ValidationAttribute
    {
        #region Properties

        /// <summary>
        /// Gets the allowed MIME types.
        /// </summary>
        public IList<string> AllowedMimeTypes { get; }

        /// <summary>
        /// Gets the file size limit in bytes.
        /// </summary>
        public long FileSizeLimit { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFileAttribute"/> class.
        /// </summary>
        public UploadFileAttribute(string? allowedMimeTypes = null, long fileSizeLimit = 0)
        {
            AllowedMimeTypes = string.IsNullOrEmpty(allowedMimeTypes)
                ? Settings.Default.AllowedMimeTypes.Cast<string>().ToList()
                : allowedMimeTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim()).ToList();

            FileSizeLimit = fileSizeLimit <= 0 ? Settings.Default.MaxAllowedUploadSize : fileSizeLimit;
        }

        #endregion

        #region ValidationAttribute Overrides

        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var name = string.IsNullOrEmpty(validationContext.DisplayName)
                ? validationContext.MemberName
                : validationContext.DisplayName;

            if (value is not IFormFile fileInformation)
                return ValidationResult.Success;

            var mimeType = fileInformation.ContentType;

            if (!AllowedMimeTypes.Contains(mimeType))
                return new ValidationResult(string.Format(Resources.MimeTypeNotAllowedText, name, mimeType));

            var length = fileInformation.Length;
            var friendlyLength = ByteSize.FromBytes(length).ToString("#.#");
            var friendlyLimit = ByteSize.FromBytes(FileSizeLimit).ToString("#.#");

            return length > FileSizeLimit
                ? new ValidationResult(string.Format(Resources.FileSizeLimitReachedText, friendlyLength, friendlyLimit))
                : ValidationResult.Success;
        }

        #endregion
    }
}
