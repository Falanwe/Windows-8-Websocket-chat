using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Messages
{
    public class ChatMessage:Message
    {
        public ChatMessage()
        {
            Type = "Chat.Say";
        }

        public User User { get; set; }

        public string Message { get; set; }
    }
}
