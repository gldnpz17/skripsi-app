using SkripsiAppBackend.Common.Deserialization;
using SkripsiAppBackend.Common;
using Flurl.Http;

namespace SkripsiAppBackend.Services
{
    public class AccessTokenService
    {
        private readonly Configuration configuration;
        public AccessTokenService(Configuration configuration)
        {
            this.configuration = configuration;
        }
        private Dictionary<string, string> accessTokens = new();
        private Dictionary<string, Task> lifetimeTasks = new();
        public async Task<string> GetToken(string refreshToken)
        {
            if (!lifetimeTasks.ContainsKey(refreshToken))
            {
                var result = await configuration.TokenUrl
                    .PostUrlEncodedAsync(new
                    {
                        client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                        client_assertion = configuration.ClientAppSecret,
                        grant_type = "refresh_token",
                        assertion = refreshToken,
                        redirect_uri = configuration.CallbackUrl
                    })
                    .ReceiveJson<TokenExchangeResult>();

                var lifetimeTask = Task.Run(async () =>
                {
                    await Task.Delay(configuration.AccessTokenLifetime);

                    accessTokens.Remove(refreshToken);

                    var task = lifetimeTasks[refreshToken];
                    lifetimeTasks.Remove(refreshToken);
                    task.Dispose();
                });

                accessTokens[refreshToken] = result.access_token;
                lifetimeTasks[refreshToken] = lifetimeTask;
            }

            return $"Bearer {accessTokens[refreshToken]}";
        }

        public async Task<string> GetToken(HttpContext context)
        {
            return await GetToken(context.User.FindFirst("refreshToken").Value);
        }
    }
}
