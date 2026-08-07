using Microsoft.AspNetCore.SignalR.Client;

namespace Sho2on.Web.Services
{
    public class ChatConnectionService : IAsyncDisposable
    {
        private readonly ChatTokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatConnectionService> _logger;
        private HubConnection? _connection;
        private bool _isConnecting = false;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public event Action<int, int, string, DateTime, int>? OnMessageReceived;
        public event Action<int, string, DateTime, int>? OnMessageSent;
        public event Action<int, int, string, DateTime, int>? OnGroupMessageReceived;

        public ChatConnectionService(ChatTokenService tokenService, IConfiguration config, ILogger<ChatConnectionService> logger)
        {
            _tokenService = tokenService;
            _config = config;
            _logger = logger;
        }

        public async Task ConnectAsync(int userId)
        {
            if (_connection != null || _isConnecting) return;

            var hubUrl = _config["ChatHub:Url"];

            if (string.IsNullOrEmpty(hubUrl))
            {
                _logger.LogWarning("ChatHub URL not configured. Chat disabled.");
                return;
            }

            _isConnecting = true;

            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(_tokenService.GenerateToken(userId));
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _connection.Closed += async (error) =>
                {
                    _logger.LogWarning("ChatHub connection closed.");
                    await Task.Delay(1000);
                    if (_connection == null)
                        return;
                    try { await _connection.StartAsync(); } catch { }
                };

                _connection.On<int, int, string, DateTime, int>("ReceiveMessage", (from, to, msg, sentAt, id) =>
                    OnMessageReceived?.Invoke(from, to, msg, sentAt, id));

                _connection.On<int, string, DateTime, int>("MessageSent", (to, msg, sentAt, id) =>
                    OnMessageSent?.Invoke(to, msg, sentAt, id));

                _connection.On<int, int, string, DateTime, int>("ReceiveGroupMessage", (groupId, from, msg, sentAt, id) =>
                    OnGroupMessageReceived?.Invoke(groupId, from, msg, sentAt, id));

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _connection.StartAsync(cts.Token);
                _logger.LogInformation("ChatHub connected successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("ChatHub connection timed out (2s). Chat disabled.");
                await DisposeConnection();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatHub connection failed. Chat disabled.");
                await DisposeConnection();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private async Task DisposeConnection()
        {
            if (_connection != null)
            {
                try { await _connection.DisposeAsync(); } catch { }
                _connection = null;
            }
        }

        public async Task SendMessageAsync(int toUserId, string message)
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            try { await _connection.InvokeAsync("SendMessageToUser", toUserId, message); } catch { }
        }

        public async Task SendGroupMessageAsync(int groupId, string message)
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            try { await _connection.InvokeAsync("SendGroupMessage", groupId, message); } catch { }
        }

        public async Task JoinGroupAsync(int groupId)
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            try { await _connection.InvokeAsync("JoinGroup", groupId); } catch { }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeConnection();
        }
    }
}