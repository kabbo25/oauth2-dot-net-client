using Microsoft.AspNetCore.Mvc;
using OAuth2DotNetClient.Services;
using System.Web;

namespace OAuth2DotNetClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly OAuth2TokenService _tokenService;
        private readonly IConfiguration _configuration;

        public HomeController(OAuth2TokenService tokenService, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var accessToken = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                ViewBag.Authenticated = true;
                ViewBag.AccessToken = accessToken;
                return View();
            }

            // Build authorization URL
            var clientId = _configuration["OAuth2:ClientId"];
            var redirectUri = _configuration["OAuth2:RedirectUri"];
            var scope = _configuration["OAuth2:Scope"];
            var authorizationUri = _configuration["OAuth2:AuthorizationUri"];

            var authorizationUrl = $"{authorizationUri}?" +
                                 $"response_type=code&" +
                                 $"client_id={HttpUtility.UrlEncode(clientId)}&" +
                                 $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                                 $"scope={HttpUtility.UrlEncode(scope)}";

            ViewBag.AuthorizationUrl = authorizationUrl;
            ViewBag.Authenticated = false;
            return View();
        }

        [Route("/callback")]
        public async Task<IActionResult> Callback(string code, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ViewBag.Error = error;
                return View();
            }

            ViewBag.Success = false;
            ViewBag.Error = false;

            Console.WriteLine($"code: {code}");

            if (!string.IsNullOrEmpty(code))
            {
                try
                {
                    var tokenResponse = await _tokenService.ExchangeCodeForTokenAsync(code);

                    HttpContext.Session.SetString("access_token", tokenResponse.AccessToken);
                    HttpContext.Session.SetString("refresh_token", tokenResponse.RefreshToken ?? "");
                    HttpContext.Session.SetString("token_type", tokenResponse.TokenType);
                    HttpContext.Session.SetInt32("expires_in", tokenResponse.ExpiresIn);
                    HttpContext.Session.SetString("id_token", tokenResponse.IdToken ?? "");

                    ViewBag.Success = true;
                    ViewBag.AuthorizationCode = code;
                    ViewBag.AccessToken = tokenResponse.AccessToken;
                    ViewBag.TokenType = tokenResponse.TokenType;
                    ViewBag.ExpiresIn = tokenResponse.ExpiresIn;
                    ViewBag.IdToken = tokenResponse.IdToken;
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Failed to exchange authorization code for token: {ex.Message}";
                }
            }

            return View();
        }

        [Route("/logout")]
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();
            
            // Remove session cookie
            if (Request.Cookies.ContainsKey(".AspNetCore.Session"))
            {
                Response.Cookies.Delete(".AspNetCore.Session");
            }
            
            // Clear any other authentication cookies
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }
            
            return RedirectToAction("Index");
        }
    }
}