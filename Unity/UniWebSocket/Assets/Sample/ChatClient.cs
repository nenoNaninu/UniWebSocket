using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using Utf8Json;

namespace UniWebSocket.Sample
{
    public class ChatClient : IChatClient
    {
        private IWebSocketClient _webSocketClient;
        private readonly ILogger _logger;
        private readonly Subject<ChatMessage> _receivedSubject = new Subject<ChatMessage>();
        private readonly Subject<WebSocketCloseStatus> _errorSubject = new Subject<WebSocketCloseStatus>();
        public IObservable<ChatMessage> OnReceived => _receivedSubject.AsObservable();
        public IObservable<WebSocketCloseStatus> OnError => _errorSubject.AsObservable();

        public ChatClient(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task Connect(string name, string uri)
        {
            _webSocketClient = new WebSocketClient(new Uri(uri), _logger);
            _webSocketClient.BinaryMessageReceived
                .Select(bin => JsonSerializer.Deserialize<ChatMessage>(bin))
                .Subscribe(x => _receivedSubject.OnNext(x));

            _webSocketClient.DisconnectionHappened
                .Do(x => _logger?.Log("DisconnectionHappened.Do()..." + x.ToString()))
                .Subscribe(x => _errorSubject.OnNext(x));


            await _webSocketClient.ConnectAndStartListening();
            _webSocketClient.Send(Encoding.UTF8.GetBytes(name));
        }

        public async Task Close()
        {
            if (_webSocketClient != null)
            {
                Debug.Log("ChatClient will be closed!!");
                await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", true);
            }
        }

        public Task Send(string message)
        {
            _webSocketClient.Send(Encoding.UTF8.GetBytes(message));
            // _webSocketClient.Send(Encoding.UTF8.GetBytes(message + " 1"));
            // _webSocketClient.Send(Encoding.UTF8.GetBytes(message + " 2"));
            // _ = _webSocketClient.SendInstant(Encoding.UTF8.GetBytes(message + " instant0"));
            // _webSocketClient.Send(Encoding.UTF8.GetBytes(message + " 3"));
            // _webSocketClient.Send(Encoding.UTF8.GetBytes(message + " 4"));
            // _webSocketClient.Send(Encoding.UTF8.GetBytes(message + " 5"));
            // _ = _webSocketClient.SendInstant(Encoding.UTF8.GetBytes(message + " instant1"));
            return Task.CompletedTask;
        }

        public async void Dispose()
        {
            await Close();
            _receivedSubject?.Dispose();
            _errorSubject?.Dispose();
        }
    }
}