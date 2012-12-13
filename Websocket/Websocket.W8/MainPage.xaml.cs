using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime; //The hidden namespace to get IBuffer related extension methods!
using Windows.Networking.Sockets;
using Newtonsoft.Json;
using Messages;


namespace Websocket.W8
{        
    public sealed partial class MainPage : Page
    {
        private Uri url = new Uri("ws://localhost/Server/chat");
        MessageWebSocket _ws;
        private User _currentUser;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            users.Items.Clear();
           
            await Login();
            _ws = new Windows.Networking.Sockets.MessageWebSocket();
            _ws.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Utf8;
            _ws.Closed += ws_Closed;
            _ws.MessageReceived += ws_MessageReceived;
            await _ws.ConnectAsync(url);

            await Send(new ConnectMessage() { User = _currentUser });
            text.KeyUp += text_KeyUp;
            text.IsEnabled = true;            
        }


        async void text_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var text = this.text.Text;
                this.text.Text = string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    await Send(new ChatMessage { User = _currentUser, Message = text });
                }
            }
        }


        async void ws_MessageReceived(Windows.Networking.Sockets.MessageWebSocket sender, Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs args)
        {
            var reader = args.GetDataReader();

            var length = reader.UnconsumedBufferLength;
            var str = reader.ReadString(length);

            var baseMessage = JsonConvert.DeserializeObject<Message>(str);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    switch (baseMessage.Type)
                    {
                        case "Chat.Say":
                            {
                                var message = JsonConvert.DeserializeObject<ChatMessage>(str);
                                messages.Items.Add(message);

                                //Wait for the message appearance animation to complete
                                await Task.Delay(100);

                                messages.ScrollIntoView(message);
                                break;
                            }
                        case "User.Add":
                            {
                                var message = JsonConvert.DeserializeObject<AddUserMessage>(str);
                                foreach (var user in message.Users)
                                {

                                    users.Items.Add(user);
                                }

                            }
                            break;
                        case "User.Remove":
                            {
                                var message = JsonConvert.DeserializeObject<RemoveUserMessage>(str);
                                foreach (var id in message.Users)
                                {
                                    var user = users.Items.Cast<User>().Where(u => u.ConnectionId == id);
                                    users.Items.Remove(user);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                });
        }

        void ws_Closed(Windows.Networking.Sockets.IWebSocket sender, Windows.Networking.Sockets.WebSocketClosedEventArgs args)
        {
            _ws.Dispose();
        }

        private async Task Send<T>(T data)
        {
            var message = JsonConvert.SerializeObject(data);
            await _ws.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(message).AsBuffer());
        }

        //private LiveConnectSession _session;
        private async Task Login()
        {
            //Uncomment the following code and add a reference to the Microsoft Live Sdk to use Microsoft Live Authentication

            /*
            var authClient = new LiveAuthClient();

            LiveLoginResult authResult = await authClient.LoginAsync(new List<string>() { "wl.signin", "wl.basic" });

            if (authResult.Status == LiveConnectSessionStatus.Connected)
            {
                _session = authResult.Session;
                var connect = new LiveConnectClient(_session);
                LiveOperationResult operationResult = await connect.GetAsync("me");

                var result = operationResult.Result;


                _currentUser = new User { Name = (string)result["name"], Image = string.Format("https://apis.live.net/v5.0/{0}/picture", result["id"]) };
            }
            */

            _currentUser = new User { Name = "Me", Image= "ms-appx:///Assets/Logo.png" };
        }
    }
}
