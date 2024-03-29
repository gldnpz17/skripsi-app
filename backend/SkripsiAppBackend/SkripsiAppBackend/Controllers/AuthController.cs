﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Jose;
using SkripsiAppBackend.Common;
using System.Text;
using SkripsiAppBackend.Services;
using System.Text.Json;
using System.Web;
using Flurl.Http;
using SkripsiAppBackend.Common.Deserialization;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Configuration configuration;
        private readonly IKeyValueService sessionIdKeyValueService;
        public AuthController(Configuration configuration, IKeyValueService sessionIdKeyValueService)
        {
            this.configuration = configuration;
            this.sessionIdKeyValueService = sessionIdKeyValueService;
        }

        public class PreSessionToken
        {
            public Guid sessionId { get; set; }
            public DateTime createdAt { get; set; }
        }

        // TODO: This probably should be moved somewhere more appropriate.
        public class SessionToken
        {
            public Guid sessionId { get; set; }
            public string profileId { get; set; }
            public string publicAlias { get; set; }
            public string refreshToken { get; set; }
            public string displayName { get; set; }
        }

        [HttpGet("create-session")]
        public ActionResult CreateSession()
        {
            var payload = new PreSessionToken()
            {
                sessionId = Guid.NewGuid(),
                createdAt = DateTime.UtcNow
            };

            HttpContext.Response.Cookies.Append("pre-auth", EncodeToken(payload));

            return new RedirectResult("/api/auth/authorize");
        }

        [HttpGet("destroy-session")]
        public ActionResult DestroySession()
        {
            HttpContext.Response.Cookies.Delete("auth");
            HttpContext.Response.Cookies.Delete("pre-auth");
            return new RedirectResult($"https://app.vssps.visualstudio.com/_signout");
        }

        [HttpGet("authorize")]
        public ActionResult Authorize()
        {
            var preSessionToken = HttpContext.Request.Cookies["pre-auth"];

            if (preSessionToken == null)
            {
                return BadRequest("Empty pre-session token.");
            }

            PreSessionToken token = DecodeToken<PreSessionToken>(preSessionToken);

            sessionIdKeyValueService.Set(token.sessionId.ToString(), "Unauthorized");

            UriBuilder uriBuilder = new(configuration.AuthUrl);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query ?? "");

            queryParams["client_id"] = configuration.ClientAppId;
            queryParams["response_type"] = "Assertion";
            queryParams["state"] = token.sessionId.ToString();
            queryParams["scope"] = configuration.Scope;
            queryParams["redirect_uri"] = configuration.CallbackUrl;

            uriBuilder.Query = queryParams.ToString();

            return new RedirectResult(uriBuilder.ToString());
        }

        [HttpGet("callback")]
        public async Task<ActionResult> Callback([FromQuery]string code, [FromQuery]Guid state)
        {
            var rawPreSessionToken = HttpContext.Request.Cookies["pre-auth"];

            if (rawPreSessionToken == null)
            {
                return BadRequest("Empty pre-session token.");
            }

            var preSessionToken = DecodeToken<PreSessionToken>(rawPreSessionToken);

            HttpContext.Response.Cookies.Delete("pre-auth");

            if (preSessionToken.sessionId != state)
            {
                return BadRequest("Session ID mismatch");
            }

            if (sessionIdKeyValueService.Get(preSessionToken.sessionId.ToString()) != "Unauthorized")
            {
                return BadRequest("Invalid session ID.");
            }

            sessionIdKeyValueService.Delete(preSessionToken.sessionId.ToString());

            var tokenResponse = await configuration.TokenUrl
                .PostUrlEncodedAsync(new {
                    client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    client_assertion = configuration.ClientAppSecret,
                    grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    assertion = code,
                    redirect_uri = configuration.CallbackUrl
                })
                .ReceiveJson<TokenExchangeResponse>();

            var profileResponse = await "https://app.vssps.visualstudio.com/_apis/profile/profiles/me?api-version=7.1-preview.3"
                .WithHeader("Authorization", $"Bearer {tokenResponse.access_token}")
                .GetAsync()
                .ReceiveJson<ProfileResponse>();

            // TODO: Might need to implement a refresh and access token system in the future.
            // Because things like the display name may change.
            var sessionTokenPayload = new SessionToken()
            {
                sessionId = preSessionToken.sessionId,
                profileId = profileResponse.id,
                publicAlias = profileResponse.publicAlias,
                refreshToken = tokenResponse.refresh_token,
                displayName = profileResponse.displayName
            };

            var sessionToken = EncodeToken(sessionTokenPayload);

            HttpContext.Response.Cookies.Append("auth", $"Bearer {sessionToken}");

            return new RedirectResult("/oauth-success");
        }

        private string EncodeToken<T>(T payload)
        {
            var secretBytes = Encoding.UTF8.GetBytes(configuration.JwtSigningSecret);

            return JWT.Encode(payload, secretBytes, JwsAlgorithm.HS256);
        }

        private T DecodeToken<T>(string token)
        {
            var secretBytes = Encoding.UTF8.GetBytes(configuration.JwtSigningSecret);

            var payloadString = JWT.Decode(token, secretBytes, JwsAlgorithm.HS256);

            return JsonSerializer.Deserialize<T>(payloadString);
        }
    }
}
