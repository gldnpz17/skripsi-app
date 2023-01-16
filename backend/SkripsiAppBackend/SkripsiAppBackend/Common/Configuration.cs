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
            TimeSpan accessTokenLifetime
        )
        {
            JwtSigningSecret = jwtSigningSecret;
            ClientAppId = clientAppId;
            AuthUrl = authUrl;
            Scope = scope;
            CallbackUrl = callbackUrl;
            TokenUrl = tokenUrl;
            ClientAppSecret = clientAppSecret;

            Environment = environment switch
            {
                "Development" => ExecutionEnvironment.Development,
                "Production" => ExecutionEnvironment.Production,
                _ => throw new ArgumentException("Invalid environment value."),
            };

            AccessTokenLifetime = accessTokenLifetime;
        }
        public string JwtSigningSecret { get; private set; }
        public string ClientAppId { get; private set; }
        public string AuthUrl { get; private set; }
        public string Scope { get; private set; }
        public string CallbackUrl { get; private set; }
        public string TokenUrl { get; private set; }
        public string ClientAppSecret { get; private set; }
        public ExecutionEnvironment Environment { get; private set; }
        public TimeSpan AccessTokenLifetime { get; private set; }
    }
}
