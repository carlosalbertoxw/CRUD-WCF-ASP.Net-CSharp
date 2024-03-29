﻿using System;
using MySql.Data.MySqlClient;

namespace Data
{
    class DataAccess
    {
        private String host = "localhost";
        private String dbname = "notes";
        private String user = "root";
        private String password = "qwerty";

        public MySqlConnection openConnection()
        {
            MySqlConnection connection;
            try
            {
                connection = new MySqlConnection("Server=" + host + "; Database=" + dbname + "; User id=" + user + "; Pwd=" + password);
                connection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ha surgido un error y no se puede abrir la conexión a la base de datos. Detalles: " + ex.ToString());
                connection = null;
            }

            return connection;
        }

        public void closeConnection(MySqlConnection connection)
        {
            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ha surgido un error y no se puede cerrar la conexión a la base de datos. Detalles: " + ex.ToString());
                connection = null;
            }
        }
    }
}
