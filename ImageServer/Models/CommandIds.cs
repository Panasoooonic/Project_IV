namespace ImageServer.Models
{
    public static class CommandIds
    {
        public const int Login = 1;
        public const int RequestImage = 2;
        public const int Logout = 3;

        public const int Ack = 100;
        public const int Error = 101;
        public const int ImageChunk = 102;
        public const int ImageComplete = 103;
    }
}