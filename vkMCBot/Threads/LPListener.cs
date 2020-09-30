using hvmbot;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using vkMCBot.Mysql;
using vkMCBot.Utilites;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace vkMCBot.Threads
{
    public class LPListener
    {
        public static VkApi api = new VkApi();
        //https://vkhost.github.io

        public static string groupToken =Configuration.vkAuth.groupToken; //new acc + new app
        //Получается https://oauth.vk.com/authorize?client_id=1234&scope=notify,wall,groups,messages,notifications,offline&redirect_uri=http://api.vk.com/blank.html&display=page&response_type=token                                                    
    
        public static string userToken = Configuration.vkAuth.userToken; //New acc + new app
        public static ulong groupID = Configuration.vkAuth.groupID;

        static List<string> otpList = new List<string>();
        //Add your objects for each string 
        List<string> removeMe = new List<string>();
        List<VKLink> removeVK = new List<VKLink>();
        static string logfile = "log.txt";
        KeyboardButtonColor agree = KeyboardButtonColor.Positive;
        public static KeyboardButtonColor decine = KeyboardButtonColor.Negative;

        //New part
        private ulong ts;
        private ulong? pts;
        //Событие для уведомления о новом сообщении (ЛС Юзер, а надо группы)
        public event Action<Message, User> OnNewMessage;

        public event Action<GroupJoin, User> OnGroupJoin;
        public event Action<GroupLeave, User> OnGroupLeave;
        public event Action<MessageAllow, User> OnMessageAllow;

        static LongPollServerResponse longPoolServerResponse;
        static List<VKLink> vKLinks = new List<VKLink>();


        public void StartMessagesHandling()  //ответ юзеру инициатору
        {

            //В отдельном потоке запускаем метод, который будет постоянно опрашивать Long Poll сервер на наличие новых сообщений
            Thread lpEL = new Thread(LongPollListener);
            lpEL.Start();
        }

        public void StartGroupJoinHandling()
        {
            Thread groupJoin = new Thread(GroupJoinHandler);
            groupJoin.Start();
        }
        public void StartGroupLeaveHandling()
        {
            Thread groupLeave = new Thread(GroupLeaveHandler);
            groupLeave.Start();
        }
        public void StartMessageAllowHandling()
        {
            Thread messageAllow = new Thread(MessageAllowHandler);
            messageAllow.Start();
        }
        //OLD Working
        public void LongPollListener()
        {

            //Default menu
            KeyboardBuilder key = new KeyboardBuilder();
            //Когда привязан уже
            KeyboardBuilder key_linked = new KeyboardBuilder();
            //Когда удалили привязку
            KeyboardBuilder key_unlinked = new KeyboardBuilder();

            key.AddButton("Привязать аккаунт", null, agree);
            key.AddButton("Удалить привязку", null, decine);
            MessageKeyboard keyboard = key.Build();

            ////Когда привязан уже
            //KeyboardBuilder key_linked = new KeyboardBuilder();
            key_linked.AddButton("Удалить привязку", null, decine);
            MessageKeyboard keyboard_linked = key_linked.Build();

            ////Когда удалили привязку
            //KeyboardBuilder key_unlinked = new KeyboardBuilder();
            key_unlinked.AddButton("Привязать аккаунт", null, agree);
            MessageKeyboard keyboard_unlinked = key_unlinked.Build();

            HashSet<string> messages = new HashSet<string>();


            try
            {
                while (true) // Бесконечный цикл, получение обновлений
                {
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = userToken,

                    }); 

                    longPoolServerResponse = new LongPollServerResponse();
                    longPoolServerResponse = api.Groups.GetLongPollServer(groupID); //id группы
            
                    BotsLongPollHistoryResponse poll = null;
                    try
                    {
                        poll = api.Groups.GetBotsLongPollHistory(
                           new BotsLongPollHistoryParams()

                           { Server = longPoolServerResponse.Server, Ts = longPoolServerResponse.Ts, Key = longPoolServerResponse.Key, Wait = 25 });
                        pts = longPoolServerResponse.Pts;
                        if (poll?.Updates == null) continue; // Проверка на новые события
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging(ex);
                        ReadError();
                    }
                    foreach (var a in poll.Updates)
                    {
                        
                        //OLD 1 Thread VERSION

                        if (a.Type == GroupUpdateType.MessageAllow) //Подписка на сообщения сообщества
                        {
                            long? userID = a.MessageAllow.UserId;
                            SendMessage("&#128521; Спасибо за подписку!", userID, keyboard, null);

                        }
                        if (a.Type == GroupUpdateType.GroupJoin) //Вступление юзера в группу
                        {

                            long? userID = a.GroupJoin.UserId;

                            SendMessage("&#127881; Добро пожаловать в наш игровой паблик!\nМы разработали собственную программу для синхронизации игрового чата с ВК!\nПосле привязки аккаунта, ты сможешь читать и отправлять сообщения в игровой чат! Круто же!!!", userID, keyboard, null);

                        }
                        if (a.Type == GroupUpdateType.GroupLeave) //Выход из группы
                        {

                            long? userID = a.GroupLeave.UserId;

                            SendMessage("&#128532; Очень жаль(\nЗаходи к нам ещё!&#128281;", userID, keyboard, null);

                        }

                        if (a.Type == GroupUpdateType.MessageNew)
                        {

                            string userMessage = a.Message.Text.ToLower();
                            long? userID = a.Message.FromId;
                            string payload = a.Message.Payload;
                            long? chatID = a.Message.ChatId;
                            long? msgID = a.Message.Id;
                            var markAsRead = a.Message.ReadState;

                            Console.WriteLine(userMessage);
                            // извлекает первый при сообщении, а нужно все получить, прогнаться по всем
                            Program P = new Program();

                            //Очистка списка кодов
                            try
                            {
                                foreach (string code in removeMe)
                                    otpList.Remove(code);
                                removeMe.Clear();
                            }
                            catch (Exception ex)
                            {
                                ErrorLogging(ex);
                                ReadError();
                            }



                            bool isLinked = MySQLClass.IsVKLinked(userID);
                            //Обработка  входящих сообщений
                            if (payload != null)//если пришло нажатие кнопки
                            {
                                //В зависимости от того какая кнопка нажата, отправляем новое меню - Если нажали првязать и успешно привязано, то удаляем кнопку"Привязать" ?! Надо ли
                                //SendMessage("Ну на кнопки ты нажимать умеешь", userID, keyboard, null);
                                
                                // АНТИСПАМ: Нажали кнопку - ждем, чтобы постоянно не тыкали, сделать таймер повторной отправки для конкретного юзера

                                switch (userMessage)
                                {
                                    case "привязать аккаунт": //Сделать обработку если после привязать отправили сообщение а не ник

                                        if (isLinked == true)
                                        {
                                            SendMessage("&#8252; Вы уже привязали аккаунт!", userID, keyboard_linked, payload);  
                                            //Таймер действия - если в течение 10 минут не произошло действие, сбросить меню
                                        } else {

                                            SendMessage("&#8987; Зайди на сервер и напиши свой ник в игре &#128172; :", userID, null, payload);
                                        }
                                        //Thread.Sleep(500);
                                        break;
                                    case "удалить привязку":
                                        //Если учетка не был привязана, проверить, если да - то проверяем

                                        if (isLinked == true)
                                        {
                                            string playerName = MySQLClass.GetNickFromID(userID);
                                            bool isSuccess2 = MySQLClass.RemoveVKID(userID);
                                            if (isSuccess2 == true)
                                            { //Успешно удалили привязку
                                                SendMessage("&#10060; Учетная запись отвязана", userID, keyboard_unlinked, payload);
                                                //Понижае привиллегии
                                                string unlink = "-" + playerName;
                                                TCPOtpSender.AddToOtpAndNameQueue(unlink);
                                            }
                                            else
                                            {
                                                SendMessage("&#8252; Ошибка удаления привязки", userID, keyboard_linked, payload);
                                            }

                                        }
                                        else
                                        { //Учетная запись не привязана - ничего не делать, отправить клавиатуру с привязкой
                                            SendMessage("&#9888; Учетная запись ещё не привязана.", userID, keyboard_unlinked, null);
                                            break;
                                        }


                                        break;


                                }

                            }
                            else //Если кнопку не нажимали, написали любое сообщение, отправляем клавиатуру?!
                            {
                                switch (userMessage)
                                {
                                    case "начать":
                                        SendMessage("Приступим, держи меню-клавиатуру!", userID, keyboard, null);
                                        break;
                                    case "Начать":
                                        SendMessage("Приступим, держи меню-клавиатуру!", userID, keyboard, null);
                                        break;

                                }

                                ////Проверяем есть ли юзер в базе данных и отправляем его сообщение в чат сервера 
                                List<long?> idList = MySQLClass.GetVKID();
                                //СДЕЛАТЬ АНТИСПАМ, если несколько раз ввели разные ники, сделать таймер повторной отправки
                                if (checkID(idList, userID) == true)
                                {
                                    // Получаем ник игрока из БД и добавляем к сообщению
                                    string plName = MySQLClass.GetNickFromID(userID);
                                    if ((plName != null) && (userMessage.StartsWith('/') == false)) //Если не консоль И юзер привязан, экранируем  
                                    {// Добавляем сообщение в очередь

                                        string combo = null;
                                        combo = plName + ": " + userMessage;

                                        //Сделать проверку на OTP после привязки шлет в чат код
                                        foreach (string otp in otpList)
                                        {
                                            int dividerIndex = otp.IndexOf(":");

                                            string otpString = otp.Substring(dividerIndex + 1, otp.Length - 1 - dividerIndex);
                                            if (userMessage == otpString)
                                            {

                                                combo = plName + " только что привязал свой ВК!";
                                            }
   
                                        }
                                        //Сделать проверку на привязку, если привязан - клавиатуру не слать.
                                        TCPClient.AddToUserMessageQueue(combo);
                                        //}
                                    }
                                    else if (plName == null) SendMessage("&#9888; Вы не привязали аккаунт! Игрок не найден.", userID, keyboard_unlinked, null);

                                }
                            
                                //Проверка ника (В любом случае) СПАМ или привязка не к тому аккаунту не того игрока!! (например в вк перебор ников идет, отправка кодов всем юзерам на серве)
                                List<string> nickNameList = MySQLClass.GetNicknames();
                                //TODO
                                foreach (string nick in nickNameList)
                                {
                                    if (userMessage == nick)
                                    {
                                        //Сгенерировать временный код
                                        OTPGenerator generator = new OTPGenerator();
                                        string otpCode = generator.GenerateOTP();

                                        string nickNameOtp = nick + ":" + otpCode;

                                        TCPOtpSender.AddToOtpAndNameQueue(nickNameOtp);

                                        otpList.Add(nickNameOtp);


                                        Console.WriteLine(nickNameOtp);
              
                                        int time = 2;
                                        string sendVK = string.Format("&#9993; Отправь мне временный код из игры\n &#9200; (действителен в течение {0} минут):&#128071;", time);
                                        SendMessage(sendVK, userID, null, null);
                                        // Начать отсчет таймера 
                                        //Таймер действия - если в течение 10 минут не произошло действие, сбросить меню
                                        Thread otpDeleter = new Thread(OTPCleaner);
                                        otpDeleter.Start(nickNameOtp);

                                        break;
                                    }
                                }

                                //Проверка одноразового пароля В ЛЮБОМ СЛУЧАЕ
                                foreach (string otp in otpList)
                                {
                                    if (otp != null)
                                    {
                                        int dividerIndex = otp.IndexOf(":");
                                        string playerName = otp.Substring(0, dividerIndex);
                                        string otpString = otp.Substring(dividerIndex + 1, otp.Length - 1 - dividerIndex);
   
                                        if (userMessage == otpString) //Временный код для авторизации юзера (получить из Майнкрафт-сервера) должен меняться, постоянно обновлять здесь
                                        {
                                            try
                                            {
          
                                                if (isLinked == true)
                                                {
                                                    SendMessage("&#9888; Учетная запись уже была привязана.", userID, keyboard_linked, null);
                    
                                                }
                                                else
                                                {

                                                    //Send userId to MYSQL 
                                                    bool isSuccess = MySQLClass.InsertVKID(userID, playerName);
                                                    // Нужна проверка на уже привязанную (если есть VKID напротив ника)
                                                    if (isSuccess == true) //Всегда успешно
                                                    { //Если в базе нет привязки ника к айди ВК
                                                        SendMessage("&#9989; Учетная запись успешно привязана!\nВы начнете получать сообщения из чата &#128173; и сможете писать сразу в игру!", userID, keyboard_linked, null);
                    
                                                        string successfull = "+" + playerName;
                                                        TCPOtpSender.AddToOtpAndNameQueue(successfull);
                                                      //Если успешно привязали - удалить из списка юзера

                                                    }
                                                    else
                                                    {
                                                        SendMessage("&#9940; Невозможно проверить по базе данных, обратитесь к админу.", userID, keyboard, null);
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                ErrorLogging(ex);
                                                ReadError();
                                            }
                                            // Извлекаем из списка найденный одноразовый код
                                            finally
                                            {
                                                removeMe.Add(otp);


                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }
        }

        private void GroupJoinHandler()
        {
            //Default menu
            KeyboardBuilder key = new KeyboardBuilder();

            key.AddButton("Привязать аккаунт", null, agree);
            key.AddButton("Удалить привязку", null, decine);
            MessageKeyboard keyboard = key.Build();

            try
            {
                while (true) // Бесконечный цикл, получение обновлений
                {
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = userToken,

                    }); 

                    longPoolServerResponse = new LongPollServerResponse();
                    longPoolServerResponse = api.Groups.GetLongPollServer(groupID); //id группы
            
                    BotsLongPollHistoryResponse poll = null;
                    try
                    {
                        poll = api.Groups.GetBotsLongPollHistory(
                           new BotsLongPollHistoryParams()

                           { Server = longPoolServerResponse.Server, Ts = longPoolServerResponse.Ts, Key = longPoolServerResponse.Key, Wait = 25 });
                        pts = longPoolServerResponse.Pts;
                        if (poll?.Updates == null) continue; // Проверка на новые события
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging(ex);
                        ReadError();
                    }
                    foreach (var a in poll.Updates)
                    {
                        if (a.Type == GroupUpdateType.GroupJoin) //Вступление юзера в группу
                        {
                            long? userID = a.GroupJoin.UserId;
  
                            SendMessage("&#127881; Добро пожаловать в наш игровой паблик!\nМы разработали собственную программу для синхронизации игрового чата с ВК!\nПосле привязки аккаунта, ты сможешь читать и отправлять сообщения в игровой чат! Круто же!!!", userID, keyboard, null);
    
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }
        }
        private void GroupLeaveHandler()
        {
            //Default menu
            KeyboardBuilder key = new KeyboardBuilder();

            key.AddButton("Привязать аккаунт", null, agree);
            key.AddButton("Удалить привязку", null, decine);
            MessageKeyboard keyboard = key.Build();

            try
            {
                while (true) // Бесконечный цикл, получение обновлений
                {
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = userToken,

                    }); 
                    longPoolServerResponse = new LongPollServerResponse();
                    longPoolServerResponse = api.Groups.GetLongPollServer(groupID); //id группы
                                                                                    //var s = api.Groups.GetLongPollServer(groupID);               
                    BotsLongPollHistoryResponse poll = null;
                    try
                    {
                        poll = api.Groups.GetBotsLongPollHistory(
                           new BotsLongPollHistoryParams()

                           { Server = longPoolServerResponse.Server, Ts = longPoolServerResponse.Ts, Key = longPoolServerResponse.Key, Wait = 25 });
                        pts = longPoolServerResponse.Pts;
                        if (poll?.Updates == null) continue; // Проверка на новые события
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging(ex);
                        ReadError();
                    }
                    foreach (var a in poll.Updates)
                    {
                        if (a.Type == GroupUpdateType.GroupLeave) //Выход из группы
                        {
                            long? userID = a.GroupLeave.UserId;
                            SendMessage("&#128532; Очень жаль(\nЗаходи к нам ещё!&#128281;", userID, keyboard, null);
 
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }
        }
        private void MessageAllowHandler()
        {
            //Default menu
            KeyboardBuilder key = new KeyboardBuilder();

            key.AddButton("Привязать аккаунт", null, agree);
            key.AddButton("Удалить привязку", null, decine);
            MessageKeyboard keyboard = key.Build();

            try
            {
                while (true) // Бесконечный цикл, получение обновлений
                {
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = userToken,

                    }); 

                    longPoolServerResponse = new LongPollServerResponse();
                    longPoolServerResponse = api.Groups.GetLongPollServer(groupID);             
                    BotsLongPollHistoryResponse poll = null;
                    try
                    {
                        poll = api.Groups.GetBotsLongPollHistory(
                           new BotsLongPollHistoryParams()

                           { Server = longPoolServerResponse.Server, Ts = longPoolServerResponse.Ts, Key = longPoolServerResponse.Key, Wait = 25 });
                        pts = longPoolServerResponse.Pts;
                        if (poll?.Updates == null) continue; // Проверка на новые события
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging(ex);
                        ReadError();
                    }
                    foreach (var a in poll.Updates)
                    {
                        if (a.Type == GroupUpdateType.MessageAllow) //Подписка на сообщения сообщества
                        {
                            long? userID = a.MessageAllow.UserId;
      

                            SendMessage("&#128521; Спасибо за подписку!", userID, keyboard, null);

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }
        }

        //Удаляет одноразовый код по истечении N- минут
        private static void OTPCleaner(object nickAndOtp/*, int minutes*/)
        {
            string toRemove = nickAndOtp.ToString();
            int minutes = 2;
            //while(true)
            //{
            Thread.Sleep(minutes * 60000); //мин

            otpList.Remove(toRemove);

            //}
        }

        //Удалять юзера, если неуспешно привязал (не захотел)
        private static void WantToLinkCleaner(object vkObject)
        {
            long? vkID = Convert.ToInt32(vkObject);
            int minutes = 2;
            VKLink vkuser = new VKLink();
            vkuser.vkID = vkID;

            Thread.Sleep(minutes * 60000);
            vKLinks.Remove(vkuser);
        }
        private static void SendMessage(string message, long? userID, VkNet.Model.Keyboard.MessageKeyboard keyboard, string payload)
        {
            try
            {
                api.Authorize(new ApiAuthParams() { AccessToken = groupToken });
                Random rnd = new Random();

                //One of the parameters specified was missing or invalid: you should specify peer_id, user_id, domain, chat_id or user_ids param
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = rnd.Next(),
                    UserId = userID,
                    Message = message,

                    Payload = payload,
                    Keyboard = keyboard,
 

                });
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }

        }
        public static void SendMessage(string message, long? userID)
        {
            try
            {
                api.Authorize(new ApiAuthParams() { AccessToken = groupToken });
                Random rnd = new Random();

                //One of the parameters specified was missing or invalid: you should specify peer_id, user_id, domain, chat_id or user_ids param
                api.Messages.Send(new MessagesSendParams
                {
                    RandomId = rnd.Next(),
                    UserId = userID,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                ErrorLogging(ex);
                ReadError();
            }

        }
        private static bool checkID(List<long?> idList, long? toCheckValue)
        {
            // check if the specified element 
            // is present in the array or not 
            // using Linear Search method 
            bool test = false;
            foreach (long? element in idList)
            {
                if (element == toCheckValue)
                {
                    test = true;
                    break;
                }
            }

            return test;
        }

        public static void ErrorLogging(Exception ex)
        {
            string strPath = logfile;
            if (!File.Exists(strPath))
            {
                File.Create(strPath).Dispose();
            }
            using (StreamWriter sw = File.AppendText(strPath))
            {
                sw.WriteLine("=============Error Logging ===========");
                sw.WriteLine("===========Start============= " + DateTime.Now);
                sw.WriteLine("Error Message: " + ex.Message);
                sw.WriteLine("Stack Trace: " + ex.StackTrace);
                sw.WriteLine("===========End============= " + DateTime.Now);

            }
        }

        public static void ReadError()
        {
            string strPath = logfile;
            using (StreamReader sr = new StreamReader(strPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
            }
        }

    }

    [DataContract]
    public class VKLink
    {
        [DataMember]
        public long? vkID { get; set; }

        [DataMember]
        public bool isPushedLink { get; set; }
        [DataMember]
        public bool isOTPgenerated { get; set; }
    }
}
