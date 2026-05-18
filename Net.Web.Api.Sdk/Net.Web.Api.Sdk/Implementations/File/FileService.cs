using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Net.Web.Api.Sdk.Interfaces.File;

namespace Net.Web.Api.Sdk.Implementations.File
{
    /// <summary>
    /// Class FileService.
    /// </summary>
    public class FileService : IFileService
    {
        #region Constants

        private const string UPLOAD_DIRECTORY_NAME = "Upload";

        #endregion

        #region Private Properties

        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileService"/> class.
        /// </summary>
        public FileService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region IFileService Implementations

        /// <summary>
        /// Uploads the file.
        /// </summary>
        public Uri UploadFile(byte[] content, string fileName)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            var destinationDirectory = Path.Combine(rootPath, UPLOAD_DIRECTORY_NAME);

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            var destinationFile = GetUniqueFileName(Path.Combine(destinationDirectory, fileName));

            System.IO.File.WriteAllBytes(destinationFile, content);

            var context = _httpContextAccessor.HttpContext;
            var baseUrl = context != null
                ? $"{context.Request.Scheme}://{context.Request.Host}"
                : string.Empty;

            return new Uri($"{baseUrl}/{UPLOAD_DIRECTORY_NAME}/{Path.GetFileName(destinationFile)}");
        }

        #endregion

        #region Private Methods

        private static string GetUniqueFileName(string fullFileName)
        {
            if (!System.IO.File.Exists(fullFileName))
                return fullFileName;

            var folder = Path.GetDirectoryName(fullFileName);

            if (folder == null) return fullFileName;

            var filename = Path.GetFileNameWithoutExtension(fullFileName);
            var extension = Path.GetExtension(fullFileName);
            var number = 1;
            var regEx = Regex.Match(fullFileName, @"(.+) \((\d+)\)\.\w+");

            if (regEx.Success)
            {
                filename = regEx.Groups[1].Value;
                number = int.Parse(regEx.Groups[2].Value);
            }

            do
            {
                number++;
                fullFileName = Path.Combine(folder, $"{filename} ({number}){extension}");
            }
            while (System.IO.File.Exists(fullFileName));

            return fullFileName;
        }

        #endregion
    }
}
