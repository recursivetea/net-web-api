using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Net.Web.Api.Sdk.Common.Http
{
    /// <summary>
    /// An IActionResult that returns a response with a specific status code and optional content.
    /// </summary>
    public class ResponseActionResult : IActionResult
    {
        #region Private Properties

        private readonly HttpStatusCode _statusCode;
        private readonly object? _content;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseActionResult"/> class.
        /// </summary>
        public ResponseActionResult(HttpStatusCode statusCode, object? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        #endregion

        #region IActionResult Implementation

        /// <inheritdoc />
        public Task ExecuteResultAsync(ActionContext context)
        {
            var result = new ObjectResult(_content) { StatusCode = (int)_statusCode };
            return result.ExecuteResultAsync(context);
        }

        #endregion
    }
}
