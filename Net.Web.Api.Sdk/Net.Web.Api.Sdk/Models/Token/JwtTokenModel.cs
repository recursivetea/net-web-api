using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using Net.Web.Api.Sdk.Configurations.Token;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Net.Web.Api.Sdk.Models.Token
{
    internal enum TokenInternalClaimNames
    {
        /// <summary>The token will not be valid before a given utc date/time</summary>
        nbf,
        /// <summary>The token expiration utc date/time</summary>
        exp,
        /// <summary>The token issue utc date/time</summary>
        iat,
        /// <summary>The token issuer</summary>
        iss,
        /// <summary>The token audience</summary>
        aud,
        /// <summary>The token unique identifier</summary>
        jti,
        /// <summary>The token expiration in minutes</summary>
        exm,
        /// <summary>The token name</summary>
        tn,
        /// <summary>One time use token</summary>
        otu
    }

    /// <summary>Enum TokenSecurityType</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenSecurityTypes
    {
        PassPhrase,
        Certificate
    }

    /// <summary>Enum TokenSecurityAlgorithms</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenSecurityAlgorithms
    {
        HmacSha256,
        HmacSha384,
        HmacSha512
    }

    /// <summary>Enum TokenStatus</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenStatus
    {
        AlreadyUsed,
        Expired,
        Invalid,
        InvalidAudience,
        TokenRequired,
        Revoked,
        Valid
    }

    /// <summary>Interface ITokenCredential</summary>
    internal interface ITokenCredential
    {
        SecurityKey? SecurityKey { get; set; }
        SigningCredentials? SigningCredentials { get; set; }
    }

    /// <summary>Class JwtTokenModel.</summary>
    public class JwtTokenModel
    {
        #region Private Constants

        private const string URN_PATTERN = @"^(?<URN>[uU][rR][nN]\:(?<NID>(?!urn\:)[a-zA-Z0-9][a-zA-Z0-9-]{1,31})\:(?<NSS>([a-zA-Z0-9()+,._!*':=@;$-]|%[0-9a-fA-F]{2})+))$";

        #endregion

        #region Public Properties

        public string TokenName { get; }
        public string TokenIssuer { get; }
        public string TokenIntendedAudience { get; }
        public double TokenExpirationInMinutes { get; }
        public bool IsTokenBase64Encoded { get; }
        public bool OneTimeUse { get; }
        public TokenSecurityTypes TokenSecurityType { get; }
        public TokenSecurityAlgorithms? TokenSecurityAlgorithm { get; set; }

        [JsonIgnore]
        internal ITokenCredential? SigningTokenCredential { get; set; }

        [JsonIgnore]
        internal ITokenCredential? ValidatingTokenCredential { get; set; }

        public string? TokenCertificateAlgorithm { get; internal set; }
        public string? CertificateAlgorithm { get; internal set; }

        #endregion

        #region Conditional Serializations

        public bool ShouldSerializeTokenSecurityAlgorithm() => TokenSecurityAlgorithm.HasValue;
        public bool ShouldSerializeTokenCertificateAlgorithm() => !string.IsNullOrEmpty(TokenCertificateAlgorithm);
        public bool ShouldSerializeCertificateAlgorithm() => !string.IsNullOrEmpty(CertificateAlgorithm);

        #endregion

        #region Internal Class

        internal class TokenCredential : ITokenCredential
        {
            public SecurityKey? SecurityKey { get; set; }
            public SigningCredentials? SigningCredentials { get; set; }
        }

        #endregion

        #region Constructors

        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public JwtTokenModel(string tokenName, TokenDefinitionElement definition)
        {
            TokenName = tokenName.ToUpper();

            var validIssuer = Uri.TryCreate(definition.Issuer, UriKind.Absolute, out var uriResult)
                              && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!validIssuer)
                throw new ArgumentException(definition.Issuer);

            TokenIssuer = definition.Issuer;

            if (!Regex.IsMatch(definition.IntendedAudience, URN_PATTERN, RegexOptions.IgnorePatternWhitespace))
                throw new ArgumentException(definition.IntendedAudience);

            TokenIntendedAudience = definition.IntendedAudience;
            TokenExpirationInMinutes = definition.ExpirationInMinute;
            IsTokenBase64Encoded = definition.IsBase64Encoded;
            OneTimeUse = definition.OneTimeUse;

            var passPhrase = definition.Signature.PassPhrase;

            if (!string.IsNullOrEmpty(passPhrase))
            {
                TokenSecurityType = TokenSecurityTypes.PassPhrase;
                SetPassPhraseSignature(passPhrase);
                return;
            }

            TokenSecurityAlgorithm = null;

            if (string.IsNullOrEmpty(definition.Signature.ValidatingCertificate) &&
                string.IsNullOrEmpty(definition.Signature.SigningCertificate))
            {
                throw new ArgumentNullException("ValidatingCertificate");
            }

            byte[]? validatingContent = null;
            byte[]? signingContent = null;

            if (!string.IsNullOrEmpty(definition.Signature.ValidatingCertificate))
            {
                var validationCertificateFile = SearchCertificate(definition.Signature.ValidatingCertificate);

                if (string.IsNullOrEmpty(validationCertificateFile))
                    throw new FileNotFoundException(definition.Signature.ValidatingCertificate);

                validatingContent = File.ReadAllBytes(validationCertificateFile);
            }

            if (!string.IsNullOrEmpty(definition.Signature.SigningCertificate))
            {
                var signingCertificateFile = SearchCertificate(definition.Signature.SigningCertificate);

                if (string.IsNullOrEmpty(signingCertificateFile))
                    throw new FileNotFoundException(definition.Signature.SigningCertificate);

                signingContent = File.ReadAllBytes(signingCertificateFile);
            }

            TokenSecurityType = TokenSecurityTypes.Certificate;
            SetCertificateSignature(validatingContent, signingContent, definition.Signature.SigningCertificatePassword);
        }

        #endregion

        #region Private Methods

        private static string? SearchCertificate(string name)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            return Directory.GetFiles(rootPath, name, SearchOption.AllDirectories).FirstOrDefault();
        }

        private void SetPassPhraseSignature(string passPhrase)
        {
            TokenCertificateAlgorithm = null;
            CertificateAlgorithm = null;

            passPhrase = Convert.ToBase64String(Encoding.UTF8.GetBytes(passPhrase));

            SigningTokenCredential = new TokenCredential
            {
                SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(passPhrase))
            };

            if (!TokenSecurityAlgorithm.HasValue)
                TokenSecurityAlgorithm = TokenSecurityAlgorithms.HmacSha256;

            var algorithm = TokenSecurityAlgorithm == TokenSecurityAlgorithms.HmacSha384
                ? SecurityAlgorithms.HmacSha384
                : TokenSecurityAlgorithm == TokenSecurityAlgorithms.HmacSha512
                    ? SecurityAlgorithms.HmacSha512
                    : SecurityAlgorithms.HmacSha256;

            SigningTokenCredential.SigningCredentials = new SigningCredentials(SigningTokenCredential.SecurityKey, algorithm);
            ValidatingTokenCredential = SigningTokenCredential;
        }

        private static string GetCertificateAlgorithm(X509Certificate2 certificate)
        {
            return certificate.SignatureAlgorithm.FriendlyName?.ToUpper() ?? string.Empty;
        }

        private void SetCertificateSignature(byte[]? validatingContent, byte[]? signingContent, string? signingCertificatePassword = null)
        {
            TokenCertificateAlgorithm = SecurityAlgorithms.RsaSha256;

            if (validatingContent != null && validatingContent.Length > 0)
            {
                var validatingCertificate = new X509Certificate2(validatingContent);

                ValidatingTokenCredential = new TokenCredential
                {
                    SecurityKey = new X509SecurityKey(validatingCertificate)
                };
                ValidatingTokenCredential.SigningCredentials = new SigningCredentials(ValidatingTokenCredential.SecurityKey, SecurityAlgorithms.RsaSha256);
                CertificateAlgorithm = GetCertificateAlgorithm(validatingCertificate);
            }

            if (signingContent == null || signingContent.Length <= 0) return;

            var signingCertificate = string.IsNullOrEmpty(signingCertificatePassword)
                ? new X509Certificate2(signingContent)
                : new X509Certificate2(signingContent, signingCertificatePassword, X509KeyStorageFlags.Exportable);

            SigningTokenCredential = new TokenCredential
            {
                SecurityKey = new X509SecurityKey(signingCertificate)
            };
            CertificateAlgorithm = GetCertificateAlgorithm(signingCertificate);
            SigningTokenCredential.SigningCredentials = new SigningCredentials(SigningTokenCredential.SecurityKey, SecurityAlgorithms.RsaSha256);
        }

        #endregion
    }
}
