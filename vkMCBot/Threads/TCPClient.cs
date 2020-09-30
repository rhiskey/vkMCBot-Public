using MySql.Data.MySqlClient.Memcached;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using VkNet.Categories;

namespace vkMCBot.Threads
{
    class TCPClient
    {
        static int port;
        int sleeptime = 30; //Seconds for sleep if cant connect
        int delay; //Delay for sending messages 
                   //String message;
        byte[] utf8bytes;
        static string server = "127.0.0.1";
        static Queue<string> userMessageQueue = new Queue<string>();


        public TCPClient(int portTo, int delayTo/*, String message*/) /*throws UnsupportedEncodingException*/
        {
            port = portTo;
            //this.admin = admin;
            delay = delayTo; //in seconds

        }

        public void TCPClientRun()
        {

            TcpClient client = null;
            Program P = new Program();
            while (true) //endless cycle
            {
                if (userMessageQueue.Count > 0)
                    try
                    {
                        client = new TcpClient(server, port);

                        //Чтобы завершить сокет правильно, посылаем команду на выключение
                        Byte[] closeConn = System.Text.Encoding.ASCII.GetBytes("exit");

                        //byte[] data = new byte[256];

                        NetworkStream stream = client.GetStream();
                        string line = null;
                        StringBuilder message = new StringBuilder();


                        line = userMessageQueue.Dequeue();
                        message.Append(line).Append("\n");
                        //}
                        if (!String.IsNullOrEmpty(message.ToString().Replace("\n", "")))
                        //Send to PLUGIN CHAT
                        {
                            string vkMessage = message.ToString();
                            utf8bytes = System.Text.Encoding.UTF8.GetBytes(vkMessage);

                            // Send the message to the connected TcpServer. 
                            if (utf8bytes.Length < 2048)
                                stream.Write(utf8bytes, 0, utf8bytes.Length);

                        }
                        stream.Write(closeConn, 0, closeConn.Length);
                        stream.Close();

                        client.Close();

                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("ArgumentNullException: {0}", e);

                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("SocketException: {0}", e);

                    }
                    finally
                    {
                        Thread.Sleep(delay * 1000);
                    }

            }
        }

        public static void TCPSend(string message)
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    Console.Write(message);

                    byte[] data = Encoding.Unicode.GetBytes(message);
                    // отправка сообщения
                    stream.Write(data, 0, data.Length);

                    // получаем ответ
                    data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    message = builder.ToString();
                    Console.WriteLine("Сервер: {0}", message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            finally
            {
                client.Close();
            }
        }

        public static void Connect(String server, Int32 port)
        {
            try
            {

                TcpClient client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    string line = null;
                    StringBuilder message = new StringBuilder();

                    // TODO Send messages from VK to PLUGIN CHAT by TCP
                    if (userMessageQueue.Count > 0)
                    {
                        line = userMessageQueue.Dequeue();
                        message.Append(line).Append("\n");
                    }
                    if (!String.IsNullOrEmpty(message.ToString().Replace("\n", "")))
                    //Send to PLUGIN CHAT
                    {
                        string vkMessage = message.ToString();
                        // Translate the Message into UTF8.
                        Byte[] data = System.Text.Encoding.UTF8.GetBytes(vkMessage);
                        // Send the message to the connected TcpServer. 
                        stream.Write(data, 0, data.Length);
                        Console.WriteLine("Sent: {0}", message);
                    }


                    Thread.Sleep(1000);

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
                //LPListener.ErrorLogging(e);
            }
            Console.Read();
        }

        // Function return true if given element 
        // found in array 
        private static bool checkQ(Queue<string> queueOTP, string toCheckValue)
        {
            // check if the specified element 
            // is present in the array or not 
            // using Linear Search method 
            bool test = false;
            foreach (string element in queueOTP)
            {
                if (element == toCheckValue)
                {
                    test = true;
                    break;
                }
            }


            return test;
        }

        private static bool checkL(List<string> ListOTP, string toCheckValue)
        {
            // check if the specified element 
            // is present in the array or not 
            // using Linear Search method 
            bool test = false;
            foreach (string element in ListOTP)
            {
                if (element == toCheckValue)
                {
                    test = true;
                    break;
                }
            }

            return test;
        }

        public static Queue<string> getUserMessageQueue()
        {
            return userMessageQueue;
        }
        public static void setUserMessageQueue(Queue<string> userMessageQueuetoSet)
        {
            userMessageQueue = userMessageQueuetoSet;
        }
        public static void AddToUserMessageQueue(string strToAdd)
        {
            userMessageQueue.Enqueue(strToAdd);
 
        }

    }
}
