namespace Sho2on.Web.Services
{
    public class InternalNotifyClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public InternalNotifyClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task PushAsync(int userId, string title, string message, string icon, string? url)
        {
            var baseUrl = _config["ChatHub:Url"]!.Replace("/chatHub", "");
            Console.WriteLine($"Pushing to: {baseUrl}/api/notify for user {userId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/notify")
            {
                Content = JsonContent.Create(new { UserId = userId, Title = title, Message = message, Icon = icon, Url = url })
            };
            request.Headers.Add("X-Internal-Key", _config["InternalApiKey"]);

            try
            {
                var response = await _http.SendAsync(request);
                Console.WriteLine($"Push response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Push failed: {ex.Message}");
            }
        }
    }
}