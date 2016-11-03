using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalNicoMyList
{
    class DBAccessor : IDisposable
    {
        SQLiteConnection _conn;

        public DBAccessor()
        {
            _conn = new SQLiteConnection();
            _conn.ConnectionString = "Data Source=localNicoMyList.db";
            _conn.Open();
            SQLiteCommand command = _conn.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS folder (" +
                "id INTEGER  PRIMARY KEY, " +
                "name TEXT, " +
                "count INTEGER NOT NULL" +
            ")";
            command.ExecuteNonQuery();

            command = _conn.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS myListItem (" +
                    "videoId text, " +
                    "createTime real, " +
                    "folderId INTEGER, " +
                    "foreign key(folderId) references folder(id)" +
            ")";
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _conn.Close();
        }

        public long addFolder(string name)
        {
            SQLiteCommand command;
            using (SQLiteTransaction trans = _conn.BeginTransaction())
            {
                command = _conn.CreateCommand();
                command.CommandText = string.Format("INSERT INTO folder (name, count) VALUES ('{0}', {1})", 
                    name, 0);
                command.ExecuteNonQuery();
                trans.Commit();
            }

            command = _conn.CreateCommand();
            command.CommandText = "SELECT last_insert_rowid()";
            return (long)command.ExecuteScalar();
        }

        public class FolderRecord
        {
            public long id;
            public string name;
            public int count;
        }

        public List<FolderRecord> getFolder()
        {
            List<FolderRecord> ret = new List<FolderRecord>();
            SQLiteCommand cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM folder";
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ret.Add(new FolderRecord() {
                        id = Convert.ToInt32(reader["id"].ToString()),
                        name = reader["name"].ToString(),
                        count = Convert.ToInt32(reader["count"].ToString()),
                    });
                }
            }
            return ret;
        }

        public class MyListItemRecord
        {
            public string videoId;
            public DateTime createTime;
        }

        public List<MyListItemRecord> getMyListItem(long folderId)
        {
            List<MyListItemRecord> ret = new List<MyListItemRecord>();
            SQLiteCommand cmd = _conn.CreateCommand();
            cmd.CommandText = string.Format("SELECT * FROM myListItem WHERE folderId = {0}", folderId);
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ret.Add(new MyListItemRecord()
                    {
                        videoId = reader["videoId"].ToString(),
                        createTime = DateTimeExt.fromUnixTime((long)((double)reader["createTime"]))
                    });
                }
            }
            return ret;
        }

        public long addMyListItem(MyListItem item, long folderId)
        {
            SQLiteCommand command;
            using (SQLiteTransaction trans = _conn.BeginTransaction())
            {
                command = _conn.CreateCommand();
                command.CommandText = string.Format("INSERT INTO myListItem VALUES ('{0}', {1}, {2})",
                        item.videoId,
                        item.createTime.toUnixTime(),
                        folderId);
                command.ExecuteNonQuery();
                trans.Commit();
            }

            command = _conn.CreateCommand();
            command.CommandText = "SELECT last_insert_rowid()";
            return (long)command.ExecuteScalar();
        }

        public void addMyListItems(IEnumerable<MyListItem> items, long folderId)
        {
            SQLiteCommand command;
            using (SQLiteTransaction trans = _conn.BeginTransaction())
            {
                foreach (var item in items)
                {
                    command = _conn.CreateCommand();
                    command.CommandText = string.Format("INSERT INTO myListItem VALUES ('{0}', {1}, {2})",
                            item.videoId,
                            item.createTime.toUnixTime(),
                            folderId);
                    command.ExecuteNonQuery();
                }
                trans.Commit();
            }
        }

        public void updateCount(long folderId, int count)
        {
            using (SQLiteTransaction trans = _conn.BeginTransaction())
            {
                SQLiteCommand command = _conn.CreateCommand();
                command.CommandText = string.Format("UPDATE folder SET count = {0} WHERE id = {1}",
                        count,
                        folderId);
                command.ExecuteNonQuery();
                trans.Commit();
            }
        }

    }
}