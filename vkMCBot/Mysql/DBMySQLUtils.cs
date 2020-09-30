using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace vkMCBot.Mysql
{
    class DBMySQLUtils
    {
        public static MySqlConnection GetDBConnection(string host, int port, string database, string username, string password)
        {
            // Connection String.

            String connString = "Server=" + host + ";Port=" + port + ";Database=" + database
                + ";Uid=" + username + ";Pwd=" + password;

            MySqlConnection conn = new MySqlConnection(connString);

            return conn;
        }
    }
}
