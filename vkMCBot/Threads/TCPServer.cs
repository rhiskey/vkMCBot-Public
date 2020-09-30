using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using vkMCBot.Mysql;

namespace vkMCBot.Threads
{
    class TCPServer
    {
        Int32 port, delay;
        Queue<string> chatMessages = new Queue<string>();

        public TCPServer(int portToSet, int delayToSet)
        {
            port = portToSet;
            delay = delayToSet;
        }

        public void TCPServerStart()
        {
            string message;

            TcpListener server = null;

            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);

                // запуск слушателя
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                while (true)
                {

                    Console.WriteLine("Ожидание подключений... ");

                    // получаем входящее подключение
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Подключен клиент. Выполнение запроса...");
                    // получаем сетевой поток для чтения и записи
                    NetworkStream stream = client.GetStream();

                    //Не читается сообщение
                    int i;
                    data = null;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                        List<long?> idList = MySQLClass.GetVKID();

                        //Read messages from PLUGIN CHAT -> Send TO BOT Community VK
                        foreach (long? id in idList)
                        {
                            LPListener.SendMessage(data, id);

                            Thread.Sleep(delay * 1000);
                        }
                    }

                    // получаем сообщение
                    StringBuilder builder = new StringBuilder();
                    //
                    Thread.Sleep(delay * 1000);
                    // закрываем поток
     
                    client.Close();

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
             
            }

      

            }

    }
}
