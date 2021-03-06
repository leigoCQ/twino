using Test.Mq.Internal;

namespace Test.Mq.Models
{
    public class MessageA
    {
        public static readonly ushort ContentType = 1001;

        public string Username { get; set; }

        public int No { get; set; }

        public MessageA()
        {
        }

        public MessageA(string username)
        {
            Username = username;
            No = Helpers.GetRandom();
        }
    }
}