# C# Synchronous MySQL Wrapper

### Installation
Get [MySql.Data](https://www.nuget.org/packages/MySql.Data/) from Nuget and palce [MySql.cs](https://github.com/KillStr3aK/Nexd.MySql/blob/main/MySql.cs) into your project folder.

### Initialization
```c#
MySql MySql = new MySql("localhost", "root", "password", "database");
```

### Insert Query
```c#
MySqlQueryValue values = new MySqlQueryValue()
    .Add("Name", "Player Name #1")
    .Add("Identifier", "uniqueidentifier");

MySql.Table("players").Insert(values);
```

### Update Query
```c#
MySqlQueryValue values = new MySqlQueryValue()
    .Add("Name", "Player Name #2");

MySql.Table("players").Where(MySqlQueryCondition.New("Identifier", "=", "uniqueidentifier")).Update(values);
```

### Select Query
```c#
MySqlQueryResult result = MySql.Table("players").Where(MySqlQueryCondition.New("Identifier", "=", "uniqueidentifier")).Get();
int playerId = result.Get<int>(0, "ID");
```

### Delete Query
```c#
MySql.Table("players").Where(MySqlQueryCondition.New("Identifier", "=", "uniqueidentifier")).Delete();
```

### Where condition
```c#
MySqlQueryCondition conditions = new MySqlQueryCondition()
    .Add("ID", ">", "1002")
    .Add("ID", "<=", "1008");

MySqlQueryResult result = MySql.Table("players").Where(conditions).Get();
for(int i = 0; i < result.Rows; i++)
{
    Console.WriteLine($"{result.Get<int>(i, "ID")} {result.Get<string>(i, "Name")} {result.Get<string>(i, "Identifier")}");
}
```
