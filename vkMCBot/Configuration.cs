namespace hvmbot
{

    public static class Configuration

    {

        public readonly static string AccessToken = "Telegram accesstoken";
        public readonly static string BotToken = "12345:token";

//#if USE_PROXY

        public static class Proxy

        {

            public readonly static string Host = "127.0.0.1";

            public readonly static int Port = 9150;

        }

//#endif
        public static class vkAuth
        {
            public readonly static string vkLogin  = "login";
            public readonly static string vkPassword = "pass";
            public readonly static ulong kateMobileAppID = 2685278;

            public readonly static string groupToken = "grouptoken"; //new acc + new app (для работы автоответчика - чат бота группы)
    
            public readonly static string userToken = "usertoken"; //New acc + new app

            //Получуется на vknet.github.io
           
            public readonly static ulong groupID = 12345;
        }

        public static class dbAuth
        {
            public readonly static string host = "localhost"; 
            public readonly static int port = 3306;
            public readonly static string database = "dbname";
            public readonly static string username = "user";
            public readonly static string password = "";
        }

    }
}
