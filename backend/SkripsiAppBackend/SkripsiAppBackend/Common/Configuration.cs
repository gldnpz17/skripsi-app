﻿using Microsoft.IdentityModel.Tokens;

namespace SkripsiAppBackend.Common
{
    public class Configuration
    {
        public enum ExecutionEnvironment
        {
            Development,
            Production
        }

        public Configuration(
            string jwtSigningSecret,
            string clientAppId,
            string authUrl,
            string scope,
            string callbackUrl,
            string tokenUrl,
            string clientAppSecret,
            string environment,
            string connectionString,
            TimeSpan accessTokenLifetime,
            double timelinessMarginFactor,
            string enableTls,
            string useBuiltFrontend,
            string tlsCertificatePath,
            string tlsPrivateKeyPath
        )
        {
            JwtSigningSecret = jwtSigningSecret;
            ClientAppId = clientAppId;
            AuthUrl = authUrl;
            Scope = scope;
            CallbackUrl = callbackUrl;
            TokenUrl = tokenUrl;
            ClientAppSecret = clientAppSecret;
            ConnectionString = connectionString;

            Environment = environment switch
            {
                "Development" => ExecutionEnvironment.Development,
                "Production" => ExecutionEnvironment.Production,
                _ => throw new ArgumentException("Invalid environment value."),
            };

            AccessTokenLifetime = accessTokenLifetime;

            TimelinessMarginFactor = timelinessMarginFactor;

            EnableTls = enableTls != null ? bool.Parse(enableTls) : false;
            UseBuiltFrontend = useBuiltFrontend != null ? bool.Parse(useBuiltFrontend) : false;

            TlsCertificatePath = tlsCertificatePath;
            TlsPrivateKeyPath = tlsPrivateKeyPath;
        }
        public string JwtSigningSecret { get; private set; }
        public string ClientAppId { get; private set; }
        public string AuthUrl { get; private set; }
        public string Scope { get; private set; }
        public string CallbackUrl { get; private set; }
        public string TokenUrl { get; private set; }
        public string ClientAppSecret { get; private set; }
        public string ConnectionString { get; private set; }
        public ExecutionEnvironment Environment { get; private set; }
        public TimeSpan AccessTokenLifetime { get; private set; }
        public double TimelinessMarginFactor { get; private set; }
        public bool EnableTls { get; private set; }
        public bool UseBuiltFrontend { get; private set; }
        public string TlsCertificatePath { get; private set; }
        public string TlsPrivateKeyPath { get; private set; }
    }
}
