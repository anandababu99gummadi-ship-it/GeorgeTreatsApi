using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AuthController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Toggle: production enable or disable swithc
            if (!_config.GetValue<bool>("AzureAd:EnableLoginEndpoint"))
                return NotFound();

            var tenantId = _config["AzureAd:TenantId"];
            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var body = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", _config["AzureAd:ClientId"]! },
            { "client_secret", _config["AzureAd:ClientSecret"]! },
            { "scope", _config["AzureAd:Scope"]! },
            { "username", request.Username },
            { "password", request.Password }
        };

            var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(body));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Unauthorized(content); // AADSTS error details comes here

            return Content(content, "application/json");
        }
    }

    public record LoginRequest(string Username, string Password);
}