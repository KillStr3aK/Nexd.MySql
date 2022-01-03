namespace Nexd.MySql
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Collections.Generic;

    using global::MySql.Data.MySqlClient;

    public class MySqlQueryValue
    {
        public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();

        public MySqlQueryValue()
            { }

        public MySqlQueryValue(string column, string value)
            => this.Add(column, value);

        public MySqlQueryValue Add(string column, string value)
        {
            this.Values.Add(column, value);
            return this;
        }
    }

    public class MySqlQueryResult
    {
        private Dictionary<int, Dictionary<string, string>> Results { get; set; } = new Dictionary<int, Dictionary<string, string>>();

        public int Rows
            => this.Results.Count;

        public List<string> Columns
            => this.Results[0].Keys.ToList<string>();

        public MySqlQueryResult(Dictionary<int, Dictionary<string, string>> query)
            => this.Results = query;

        public T Get<T>(int row, string column)
            => (T)Convert.ChangeType(this.Results[row][column], typeof(T));
    }

    public class MySqlQueryCondition
    {
        public Dictionary<string, Dictionary<string, string>> List { get; private set; } = new Dictionary<string, Dictionary<string, string>>();

        public MySqlQueryCondition()
            { }

        public MySqlQueryCondition(string column, string expression, string value)
            => this.Add(column, expression, value);

        public MySqlQueryCondition(MySqlQueryCondition condition)
            => this.List = condition.List;

        public MySqlQueryCondition Add(string column, string expression, string value)
        {
            if(!this.List.ContainsKey(column))
            {
                this.List.Add(column, new Dictionary<string, string> { [expression] = value });
            } else
            {
                this.List[column].Add(expression, value);
            }

            return this;
        }

        public static MySqlQueryCondition New(string column, string expression, string value)
            => new MySqlQueryCondition(column, expression, value);

        public static MySqlQueryCondition operator +(MySqlQueryCondition left, MySqlQueryCondition right)
        {
            MySqlQueryCondition newConditions = new MySqlQueryCondition();
            foreach (KeyValuePair<string, Dictionary<string, string>> condition in right.List)
            {
                if (!newConditions.List.ContainsKey(condition.Key))
                {
                    newConditions.Add(condition.Key, condition.Value.First().Key, condition.Value.First().Value);
                } else
                {
                    newConditions.List[condition.Key].Add(condition.Value.First().Key, condition.Value.First().Value);
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> condition in left.List)
            {
                if (!newConditions.List.ContainsKey(condition.Key))
                {
                    newConditions.Add(condition.Key, condition.Value.First().Key, condition.Value.First().Value);
                } else
                {
                    newConditions.List[condition.Key].Add(condition.Value.First().Key, condition.Value.First().Value);
                }
            }

            return newConditions;
        }
    }

    public class MySqlConfig
    {
        public string Hostname { get; set; } = "localhost";

        public string Database { get; set; }

        public string Username { get; set; } = "root";

        public string Password { get; set; } = "";

        public int Port { get; set; } = 3306;

        public string SSLMode { get; set; } = "none";

        public MySqlConfig(string hostname, string database, string username, string password, int port, string sslmode)
        {
            this.Hostname = hostname; this.Database = database; this.Username = username; this.Password = password; this.Port = port; this.SSLMode = sslmode;
        }

        public override string ToString()
            => $"Server={this.Hostname};Database={this.Database};port={this.Port};User Id={this.Username};password={this.Password};SslMode={this.SSLMode};";
    }

    public class MySql
    {
        private MySqlConnection Connection { get; set; }

        private string Query { get; set; }

        private string TableName { get; set; }

        private string WhereQuery { get; set; }

        public ConnectionState State
            => this.Connection.State;

        public bool IsConnected
            => this.State == ConnectionState.Open;

        public bool IsPasswordExpired
            => this.Connection.IsPasswordExpired;

        public MySql(MySqlConfig config) : this(config.ToString())
            { }

        public MySql(string hostname, string username, string password, string database, int port = 3306, string sslmode = "none")
        {
            this.Connection = new MySqlConnection($"Server={hostname};Database={database};port={port};User Id={username};password={password};SslMode={sslmode};");
            this.Connection.Open();
        }

        public MySql(string input)
        {
            this.Connection = new MySqlConnection(input);
            this.Connection.Open();
        }

        public MySql Table(string name)
        {
            this.TableName = name;
            return this;
        }

        public MySql Where(MySqlQueryCondition condition)
        {
            this.WhereQuery = "WHERE " + string.Join(" AND ", condition.List.Select(x => string.Join(" OR ", x.Value.Select(y => "`" + x.Key + "` " + y.Key + " '" + y.Value + "'").ToArray())).ToArray());
            return this;
        }

        public void Insert(MySqlQueryValue data)
        {
            this.Query = "INSERT INTO `" + this.TableName + "`(" + string.Join(", ", data.Values.Select(x => x.Key).ToArray()) + ") VALUES (" + string.Join(", ", data.Values.Select(x => "'" + x.Value + "'").ToArray()) + ")";
            this.ExecuteQuery();
        }

        public void InsertIfNotExist(MySqlQueryValue data, string onduplicate = "")
        {
            this.Query = "INSERT INTO `" + this.TableName + "`(" + string.Join(", ", data.Values.Select(x => x.Key).ToArray()) + ") VALUES (" + string.Join(", ", data.Values.Select(x => "'" + x.Value + "'").ToArray()) + ") ON DUPLICATE KEY UPDATE " + onduplicate;
            this.ExecuteQuery();
        }

        public MySqlQueryResult Get(string fields = "*", int rowLimit = 0)
        {
            string limit = rowLimit > 0 ? " LIMIT " + rowLimit : "";
            this.Query = "SELECT " + fields + " FROM `" + this.TableName + "` " + this.WhereQuery + limit;
            return this.ExecuteQuery();
        }

        public void Update(MySqlQueryValue data)
        {
            this.Query = "UPDATE `" + this.TableName + "` SET " + string.Join(", ", data.Values.Select(x => "`" + x.Key + "` = '" + x.Value + "'").ToArray()) + " " + this.WhereQuery;
            this.ExecuteQuery();
        }

        public void Delete()
        {
            this.Query = "DELETE FROM `" + this.TableName + "` " + this.WhereQuery;
            this.ExecuteQuery();
        }

        private void ResetQuery()
            => this.Query = this.WhereQuery = this.TableName = string.Empty;

        public void Ping()
            => this.Connection.Ping();

        private MySqlQueryResult ExecuteQuery()
        {
            Dictionary<int, Dictionary<string, string>> result = new Dictionary<int, Dictionary<string, string>>();

            using (MySqlCommand cmd = new MySqlCommand(this.Query, this.Connection))
            {
                using(MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        int rowCol = 0;
                        while (reader.Read())
                        {
                            Dictionary<string, string> fieldValue = new Dictionary<string, string>();

                            for (int col = 0; col < reader.FieldCount; col++)
                            {
                                fieldValue.Add(reader.GetName(col).ToString(), reader.GetValue(col).ToString());
                            }

                            result.Add(rowCol, fieldValue);
                            rowCol++;
                        }
                    }
                }
            }

            this.ResetQuery();
            return new MySqlQueryResult(result);
        }
    }
}