using System.Net.Http.Headers;

namespace ToanHocHay.WebApp.Services.Implementations
{
    public abstract class BaseApiService
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly IHttpContextAccessor _httpContext;

        protected BaseApiService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContext)
        {
            _httpClientFactory = httpClientFactory;
            _httpContext = httpContext;
        }

        protected HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            var token = _httpContext.HttpContext?
                .Session.GetString("AccessToken");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}
