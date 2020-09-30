using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace vkMCBot.Utilites
{
    class OTPGenerator
    {
        string OTP;
        public string GenerateOTP()
        {
            //Generate OTP
            Random rnd = new Random();
            OTP = rnd.Next(1000, 9999).ToString();
            return OTP;
        }
    }
}
