using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Messages
{
    public class ConnectMessage : Message
    {
        public ConnectMessage()
        {
            Type = "User.Connect";
        }
        public User User { get; set; }
    }
    public class AddUserMessage : Message
    {
        public AddUserMessage()
        {
            Type = "User.Add";
        }


        public User[] Users;
    }

    public class RemoveUserMessage : Message
    {
        public RemoveUserMessage()
        {
            Type = "User.Remove";
        }

        public string[] Users;
    }

    public class User
    {
        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }
}
