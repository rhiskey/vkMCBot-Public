using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using vkMCBot.Threads;


namespace vkMCBot
{
    class Program
    {
        List<string> otpList = new List<string>();
        static int delay = 1; //1 sec
        private static int port_tcp_chat_uplink = 8335; //FROM BOT TO PLUGIN
        private static int port_tcp_console_uplink = 8337; //From BOT TO PLUGIN
        public static int port_tcp_chat_downlink = 8334; //FROM PLUGIN TO BOT (Current)
        static int port_tcp_console_downlink = 8336; //FROM  PLUGIN TO BOT
        public int port_tcp_otp_uplink = 8340; //FROM BOT TO PLUGIN

        //Queue for sending Messages to VK/From VK
        private Queue<string> consoleMessageQueue = new Queue<string>();
        private Queue<string> userMessageQueue = new Queue<string>();

        //TODO message distribution list (VK) 
        //private Queue<string> otpAndNameQueue = new Queue<string>();

        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            Program P = new Program();
            //TCP сервер и TCP клиент в отдельных потоках
            //TCPClient tcp_client = new TCPClient(port_tcp_chat_uplink, delay); // if false = user Community BOT
            //TCPOtpListenerServer tcp_otp_server = new TCPOtpListenerServer(port_tcp_otp);

            //Send OTP through TCP
            TCPOtpSender tCPOtpSender = new TCPOtpSender(P.port_tcp_otp_uplink, delay);
            Thread optSenderThread = new Thread(tCPOtpSender.TCPSenderRun);
            optSenderThread.Start();

            //Read OTP (UNUSED)
            //TCPOtpListenerServer tcp_otp = new TCPOtpListenerServer();
            //Thread otpServerThread = new Thread(tcp_otp.TCPOtpListenerServerStart);
            //otpServerThread.Start(port_tcp_otp_uplink);

            TCPClient tcp_c = new TCPClient(port_tcp_chat_uplink, delay);
            Thread tcpClientThread = new Thread(tcp_c.TCPClientRun);
            tcpClientThread.Start();

            //new Thread(() =>
            //{
            //    Thread.CurrentThread.IsBackground = true;
            //    TCPClient.Connect("127.0.0.1", port_tcp_chat_uplink);
            //}).Start();

            //TCPServer tcp_s = new TCPServer(port_tcp_chat_downlink, delay);
            //Thread tcpServerThread = new Thread(tcp_s.TCPServerStart);
            //tcpServerThread.Start();

            //AsynchronousSocketListener.StartListening();

            //Сервер для чтения чата/консоли и отправки в ВК каждому привязанному юзеру
            Thread t = new Thread(delegate ()
            {
                // replace the IP with your system IP Address...
                Server myserver = new Server("127.0.0.1", port_tcp_chat_downlink, delay);
            });
            t.Start();

            Console.WriteLine("Server Started...!");

            //Old 1 thread
            LPListener LPL = new LPListener();
            //LongPollListener в отдельном потоке
            Thread LPthread = new Thread(LPL.LongPollListener);
            LPthread.Start();

            ////Each message or action = New Thread (multitask)
            ///Получается что при новом событии запускается новый поток с бесконечным циклом, в котором обрабатываются ВСЕ события, а не только инициатора (юзера)
            ///Нужно как-то передать в обработчик юзера ?
            ///
            
            //LPListener manager = new LPListener();
            //manager.OnNewMessage += (message, sender) =>
            //{
            //    //Обрабатываем входящее сообщение
            //};
            //manager.StartMessagesHandling();

            //manager.OnGroupJoin += (message, sender) =>
            //{
            //    //Обработка вступления в паблик
            //};
            //manager.StartGroupJoinHandling();

            //manager.OnGroupLeave += (message, sender) =>
            //{
            //    //Обработка выхода из паблика
            //};
            //manager.StartGroupLeaveHandling();

            //manager.OnMessageAllow += (message, sender) =>
            //{
            //    //Подписка на сообщения сообщества
            //};
            //manager.StartMessageAllowHandling();
        }

        //public List<string> GetOtpList()
        //{
        //    return otpList;
        //}
        //public void SetOtpList(List<string>otpListToSet)
        //{
        //    otpList = otpListToSet;
        //}
        //public void AddToOtpList(string ToAdd)
        //{
        //    otpList.Add(ToAdd);
        //}
        //public void RemoveFromOtpList(string ToRemove)
        //{
        //    otpList.Remove(ToRemove);
        //}

        //public Queue<string> getUserMessageQueue()
        //{
        //    return userMessageQueue;
        //}
        //public void setUserMessageQueue(Queue<string> userMessageQueue)
        //{
        //    this.userMessageQueue = userMessageQueue;
        //}

        //public Queue<string> getConsoleMessageQueue()
        //{
        //    return consoleMessageQueue;
        //}

        //public void setConsoleMessageQueue(Queue<string> consoleMessageQueue)
        //{
        //    this.consoleMessageQueue = consoleMessageQueue;
        //}
    }
}
