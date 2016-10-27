using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.VisualBasic;

namespace EconnectTMSservice
{
    class ClsEbankingconnections
    {
        public SqlConnection Conn = new SqlConnection(ConfigurationSettings.AppSettings["eBankConnectionString"]);
        public SqlConnection Conn2 = new SqlConnection(ConfigurationSettings.AppSettings["eBankConnectionString"]);
        public string strConnectionString_ = ConfigurationSettings.AppSettings["eBankConnectionString"];
        public string StrEconnectString = ConfigurationSettings.AppSettings["eConnectConnectionString"];
        public string StrEconnectTestString = ConfigurationSettings.AppSettings["eConnectTestConnectionString"];

        public SqlDataReader RunStoredProcedureReturnDataReader(string str)
        {
            if (Conn.State == ConnectionState.Open)
            {
                Conn.Close();
            }
            Conn.Open();
            SqlCommand sqlDBcommand = new SqlCommand();
            sqlDBcommand.CommandText = (str);
            sqlDBcommand.CommandType = CommandType.StoredProcedure;
            sqlDBcommand.Connection = Conn;
            SqlDataReader strSqlReader = sqlDBcommand.ExecuteReader();
            return strSqlReader;
        }

        public SqlDataReader RunQueryReturnDataReader(string str)
        {
            if (Conn.State == ConnectionState.Open)
            {
                Conn.Close();
            }
            Conn.Open();
            SqlCommand sqlDBcommand = new SqlCommand();
            sqlDBcommand.CommandText = (str);
            sqlDBcommand.CommandType = CommandType.Text;
            sqlDBcommand.Connection = Conn;
            SqlDataReader strSqlReader = sqlDBcommand.ExecuteReader();
            return strSqlReader;
        }
        public bool RunNonQuery(string strUpdateSaveString)
        {
            Int32 success;

            try
            {
                if (Conn2.State == ConnectionState.Open)
                {
                    Conn2.Close();
                }
                Conn2.Open();
                SqlCommand sqlDBcommand = new SqlCommand();
                sqlDBcommand.CommandText = (strUpdateSaveString);
                sqlDBcommand.CommandType = CommandType.Text;
                sqlDBcommand.Connection = Conn2;
                success = sqlDBcommand.ExecuteNonQuery();

                if (success == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (SqlException ErrorUpdate)
            {

               // My.Computer.FileSystem.WriteAllText("c:\\Logs\\ErrorFlani-" + Strings.Format(DateTime.Now, "dd-MM-yyyy") + ".log", "" + ErrorUpdate.Message + "" + "|" + DateTime.Now + Constants.vbCrLf, true);
                return false;
            }
        }
        public string RunStringReturnStringValue(string strStringToRun)
        {
            string strReturnString = "";
            try
            {
                if (Conn.State == ConnectionState.Open)
                {
                    Conn.Close();
                }
                Conn.Open();
                SqlCommand sqlDBcommand = new SqlCommand();
                SqlDataReader strReader1 = default(SqlDataReader);

                sqlDBcommand.CommandText = (strStringToRun);
                sqlDBcommand.CommandType = CommandType.Text;
                sqlDBcommand.Connection = Conn;
                strReader1 = sqlDBcommand.ExecuteReader();
                if (strReader1.HasRows == true)
                {
                    strReader1.Read();
                    strReturnString = (Information.IsDBNull(strReader1[0]) ? "" : strReader1[0].ToString());
                }
                else
                {
                    strReturnString = "";
                }
            }
            catch (SqlException ErrorUpdate)
            {
                strReturnString = "";
            }
            return strReturnString;
        }

        //

        public string InsertString(string table, Dictionary<string, string> collection)
        {
            string sql = "";

            if (collection.Count > 0)
            {
                string keys = "";
                string values = "";
                foreach (var item in collection)
                {
                    keys += item.Key + ",";
                    values += "'" + item.Value + "',";
                }
                keys = keys.Remove(keys.Length - 1);
                values = values.Remove(values.Length - 1);
                sql = "insert into " + table + " (" + keys + ") values(" + values + ")";
            }

            return sql;
        }
        public string UpdateString(string table, Dictionary<string, string> collection, Dictionary<string, string> wherecollection)
        {
            string sql = "";

            if (collection.Count > 0)
            {
                string keys = "";
                string values = "";
                sql = "update " + table + " set ";
                foreach (var item in collection)
                {
                    keys = item.Key + "=";
                    values = "'" + item.Value + "',";
                    sql += keys + values;
                }

                sql = sql.Remove(sql.Length - 1);

                sql += " where ";

                foreach (var item in wherecollection)
                {
                    keys = item.Key + "=";
                    values = "'" + item.Value + "' and ";
                    sql += keys + values;
                }
                sql = sql.Remove(sql.Length - 5);
            }

            return sql;
        }
    }
}
