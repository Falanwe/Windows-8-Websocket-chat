using Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using System.Linq;

namespace Server
{
    public class WebSocketHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            
            if (!context.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
            context.AcceptWebSocketRequest(Connect);


        }

        #endregion

        private uint BufferSize = 10240;
        private ConcurrentDictionary<string, User> _users = new ConcurrentDictionary<string, User>();
        private ConcurrentDictionary<string, AspNetWebSocketContext> clients = new ConcurrentDictionary<string, AspNetWebSocketContext>();
        private async Task Connect(AspNetWebSocketContext context)
        {
            var guid = Guid.NewGuid().ToString();
            clients.TryAdd(guid, context);
            var buffer = new byte[BufferSize];
            
            
            while (context.WebSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var cts = new CancellationTokenSource();

                var segment = new ArraySegment<byte>(buffer);
                try
                {
                var result = await context.WebSocket.ReceiveAsync(segment, cts.Token);
                switch (result.MessageType)
                {
                    case System.Net.WebSockets.WebSocketMessageType.Text:
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var t = MessageReceived(guid, message);
                        break;
                    case System.Net.WebSockets.WebSocketMessageType.Close:
                        OnSocketClosed(guid);
                        break;
                    default:
                        throw new NotSupportedException();

                }
                }
                catch (Exception ex)
                {
                    OnSocketClosed(guid);
                }

            }
            //OnSocketClosed(guid);

        }

        private async Task SendMessage<T>(string connectionId, T data)
        {
            var message = JsonConvert.SerializeObject(data);
            var ws = clients[connectionId].WebSocket;
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            var cts = new CancellationTokenSource();
            if (ws.State == System.Net.WebSockets.WebSocketState.Open)
            {
            await ws.SendAsync(new ArraySegment<byte>(buffer), System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
            }

        }
        private async Task BroadCast<T>(T data)
        {
            var message = JsonConvert.SerializeObject(data);
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            var tasks = new List<Task>();
            var cts = new CancellationTokenSource();
            foreach (var ws in clients.Values)
            {
                if (ws.WebSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                tasks.Add(ws.WebSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token));
            }
            }
            await Task.WhenAll(tasks);
        }

        private void OnSocketClosed(string connectionId)
        {
            AspNetWebSocketContext _;
            clients.TryRemove(connectionId, out _);
            User user;
            _users.TryRemove(connectionId, out user);

            var t = BroadCast(new RemoveUserMessage { Users = new string[] { connectionId } });

        }

        private async Task MessageReceived(string connectionId, string data)
        {
            var baseMessage = JsonConvert.DeserializeObject<Message>(data);
            switch (baseMessage.Type)
            {
                case "User.Connect":
                    {
                        var message = JsonConvert.DeserializeObject<ConnectMessage>(data);
                        var user = message.User;
                        user.ConnectionId = connectionId;
                        var users = new List<User>();
                        foreach (var u in _users.Values)
                        {
                            users.Add(u);
                        }
                        _users.TryAdd(connectionId, user);
                        await BroadCast(new AddUserMessage { Users = new User[] { user } });
                        if (users.Any())
                        {
                            await SendMessage(connectionId, new AddUserMessage { Users = users.ToArray() });
                        }
                    }
                    break;
                case "Chat.Say":
                    {
                        var message = JsonConvert.DeserializeObject<ChatMessage>(data);
                        var t = BroadCast(message);
                    }
                    break;
                default:
                    throw new NotSupportedException(baseMessage.Type);
            }
        }
    }
}
