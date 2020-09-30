using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using vkMCBot.Mysql;
using VkNet;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace vkMCBot.Threads
{
    class Server
    {
        TcpListener server = null;
        private int delay;
        static Queue<VKparams> chatMessages = new Queue<VKparams>();
        //Когда привязан уже
        KeyboardBuilder key_linked = new KeyboardBuilder();
        public static VkApi api = new VkApi();


        public Server(string ip, int port, int delay)
        {
            this.delay = delay;
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);

            //Start VK Send Scheduler очередь отправки сообщений из чата юзерам
            Thread scheduler = new Thread(VKScheduler);
            scheduler.Start();

            server.Start();
            StartListener();


        }
        public void StartListener()
        {
            try
            {
                while (true)
                {

                    TcpClient client = server.AcceptTcpClient();

                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
               
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);

                server.Stop();
            }
        }
        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string imei = String.Empty;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, i);
                    Console.WriteLine("{1}, Queue {2}: Received: {0}", data, Thread.CurrentThread.ManagedThreadId, chatMessages.Count);


                    if (data != null)
                    {
                        List<long?> idList = MySQLClass.GetVKID();

                        //Read messages from PLUGIN CHAT -> Send TO BOT Community VK
                        foreach (long? id in idList)
                        {
                            //Add Message+ID to Queue
                            VKparams vkQ = new VKparams()
                            {
                                messageToSend = data,
                                vkID = id
                            };

                            //Добавляем в конец очереди рассылки
                            chatMessages.Enqueue(vkQ);

                        }
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());

                client.Close();
            }
            //finally { client.Close(); }
        }

        public void VKScheduler()
        {
            //LPListener lpl = new LPListener();
            key_linked.AddButton("Удалить привязку", null, LPListener.decine);
            MessageKeyboard keyboard_linked = key_linked.Build();
            //Очищать очередь сообщений - отправлять в ВК
            while (true)
            {
                try
                {

                    if (chatMessages.Count > 0)

                    {

                        //Берем элемент в начале очереди
                        VKparams el = chatMessages.Peek();
                        string msg = el.messageToSend;
                        long? vkid = el.vkID;
                        bool sended=false;
                        if (msg != null && vkid != null)
                            sended = SendMessage(msg, vkid, keyboard_linked, null);

                        if(sended==true)
                            el = chatMessages.Dequeue();

                        Thread.Sleep(delay * 1000);
                    }
                }
                catch (Exception ex)
                {
                    LPListener.ErrorLogging(ex);
                    LPListener.ReadError();
                }
            }
        }

        //После успешной отправки возвращает true
        private static bool SendMessage(string message, long? userID, VkNet.Model.Keyboard.MessageKeyboard keyboard, string payload)
        {
            bool isSended;
            try
            {
                api.Authorize(new ApiAuthParams() { AccessToken = LPListener.groupToken });
                Random rnd = new Random();

                //One of the parameters specified was missing or invalid: you should specify peer_id, user_id, domain, chat_id or user_ids param
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = rnd.Next(),
                    UserId = userID,
                    Message = message,

                    Payload = payload,


                });
                isSended = true;
            }
            catch (Exception ex)
            {
                LPListener.ErrorLogging(ex);
                LPListener.ReadError();
                isSended = false;
            }
            return isSended;

        }
    }

    //Хранит очередь в виде сообщение + айди
    class VKparams
    {
        public long? vkID { get; set; }
        public string messageToSend { get; set; }

        //Деструктор
        ~VKparams() { }
        }
}
