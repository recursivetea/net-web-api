using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Net.Web.Api.Sdk.Attributes.Validations;

namespace Net.Web.Api.Sdk.Web.Examples.Models
{
    /// <summary>
    /// Class UploadRequest.
    /// </summary>
    public class UploadRequest
    {
        #region Public Properties

        /// <summary>
        /// File to upload.
        /// </summary>
        [Required]
        [UploadFile]
        public IFormFile FileInformation { get; set; } = null!;

        #endregion
    }
}
