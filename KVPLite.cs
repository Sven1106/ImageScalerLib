using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace KVPLiteLib
{
    public class KVPLite
    {
        private static string DBFILENAME = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "KVPLite.sqlite");
        private SQLiteConnection _dbConnection = null;
        public KVPLite()
        {
            bool speedCompetition = false;
            string pragmaSettings = "PRAGMA auto_vacuum = FULL;";
            if (speedCompetition)
            {
                pragmaSettings += " PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";
            }

            if (this._dbConnection == null)
            {
                this._dbConnection = new SQLiteConnection("Data Source=" + DBFILENAME + ";Version=3;");
                if (System.IO.File.Exists(DBFILENAME) == false)
                {
                    SQLiteConnection.CreateFile(DBFILENAME);
                    this._dbConnection.Open();
                    using (var cmd = new SQLiteCommand(pragmaSettings, this._dbConnection))
                    {
                        cmd.CommandText += "CREATE TABLE Kvp (key char(8) primary key, value varchar(20000));";
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    this._dbConnection.Open();
                    using (var cmd = new SQLiteCommand(pragmaSettings, this._dbConnection))
                    {
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "SELECT COUNT(*) as COUNT FROM sqlite_master;";
                        if ((long)cmd.ExecuteScalar() == 0)
                        {
                            cmd.CommandText = "CREATE TABLE Kvp (key char(8) primary key, value varchar(20000));";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        ~KVPLite()
        {
            this._dbConnection.Clone();
        }

        public bool SetKvp(KeyValuePair<string, string> keyValuePair)
        {
            if (KvpExists(keyValuePair.Key))
            {
                return false;
            }
            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Kvp (key, value) values (@key, @value)", this._dbConnection))
            {
                cmd.Parameters.AddWithValue("@key", keyValuePair.Key);
                cmd.Parameters.AddWithValue("@value", keyValuePair.Value);
                cmd.ExecuteNonQuery();
            }
            return true;
        }
        public bool RemoveKvp(string key)
        {
            if (KvpExists(key) == false)
            {
                return false;
            }
            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Kvp WHERE key=@key", this._dbConnection))
            {
                cmd.Parameters.AddWithValue("@key", key);
                cmd.ExecuteNonQuery();
            }
            return true;
        }
        public bool RemoveAllKvp()
        {
            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Kvp", this._dbConnection))
            {
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT COUNT(*) as COUNT FROM Kvp;";
                if ((long)cmd.ExecuteScalar() > 0)
                {
                    return false;
                }
            }
            return true;
        }
        public KeyValuePair<string, string> GetKvp(string key)
        {
            KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>();
            if (KvpExists(key) == false)
            {
                return keyValuePair;
            }
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Kvp WHERE key=@key", this._dbConnection))
            {
                cmd.Parameters.AddWithValue("@key", key);
                using (var dbresult = cmd.ExecuteReader())
                {
                    dbresult.Read();
                    keyValuePair = new KeyValuePair<string, string>(dbresult.GetString(0), dbresult.GetString(1));
                }
            }
            return keyValuePair;
        }
        private bool KvpExists(string key)
        {
            bool exists = false;
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT EXISTS(SELECT 1 FROM Kvp WHERE key=@key LIMIT 1);", this._dbConnection))
            {
                cmd.Parameters.AddWithValue("@key", key);
                exists = (long)cmd.ExecuteScalar() == 1;
            }
            return exists;
        }
    }
}
