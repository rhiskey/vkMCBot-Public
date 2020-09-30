using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace vkMCBot.Threads
{
    class TCPOtpSender
    {
        string nickPlusOTP;
        static int port;
        byte[] utf8bytes;
        private int delay; //Delay for sending messages in seconds

        private static Queue<string> otpAndNameQueue = new Queue<string>();

        public TCPOtpSender(int portTo, int delayTo/*, String message*/) /*throws UnsupportedEncodingException*/
        {
            port = portTo;

            delay = delayTo; //in seconds

        }
        public void TCPSenderRun()
        {
            string server = "127.0.0.1";


            //Извлекать в цикле из очереди OTP и слать плагину
            while (true)
            {
               
                try
                {
                    Program P = new Program();

                    TcpClient client = new TcpClient(server, port);

                    //Чтобы завершить сокет правильно, посылаем команду на выключение
                    Byte[] closeConn = System.Text.Encoding.ASCII.GetBytes("exit");

                    // Get a client stream for reading and writing.
                    //Stream stream = client.GetStream();

                    NetworkStream stream = client.GetStream();
                    string line = null;
                    StringBuilder nickOtp = new StringBuilder();
                    if (otpAndNameQueue.Count > 0)
                    {
                        line = otpAndNameQueue.Dequeue();
                        nickOtp.Append(line).Append("\n");
                    }
                    if (!String.IsNullOrEmpty(nickOtp.ToString().Replace("\n", "")))
                    {
                       // Translate the passed message into ASCII and store it as a Byte array.
                        //Byte[] data = System.Text.Encoding.UTF8.GetBytes(nickOtp.ToString());
                        utf8bytes = System.Text.Encoding.UTF8.GetBytes(nickOtp.ToString());
                        // Send the message to the connected TcpServer. 
                        if (utf8bytes.Length < 2048)
                            stream.Write(utf8bytes, 0, utf8bytes.Length);
                        Console.WriteLine("Sent: {0}", nickOtp);
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
                Thread.Sleep(delay*1000);
            }
        }

        public void TCPSenderSend(string otpnick)
        {
            string server = "127.0.0.1";
            Program P = new Program();
            TcpClient client = new TcpClient(server, port);

            //Чтобы завершить сокет правильно, посылаем команду на выключение
            Byte[] closeConn = System.Text.Encoding.ASCII.GetBytes("exit");

            NetworkStream stream = client.GetStream();
            //string line = null;
            try { 
            utf8bytes = System.Text.Encoding.UTF8.GetBytes(otpnick.ToString());
            // Send the message to the connected TcpServer. 
            if (utf8bytes.Length < 2048)
                stream.Write(utf8bytes, 0, utf8bytes.Length);
            Console.WriteLine("Sent: {0}", otpnick);

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
        }

        public static Queue<string> GetOtpAndNameQueue()
        {
            return otpAndNameQueue;
        }

        public static void AddToOtpAndNameQueue(string stringToAdd)
        {
            otpAndNameQueue.Enqueue(stringToAdd);
        }

    }
}
