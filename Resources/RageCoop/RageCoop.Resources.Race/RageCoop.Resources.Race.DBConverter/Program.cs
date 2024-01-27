using LiteDB;
using RageCoop.Resources.Race.Objects;
using System.Data.SQLite;

var filename = "times.db";
if (!File.Exists(filename))
{
    Console.WriteLine($"\n{filename} not found, press enter to exit");
    Console.ReadLine();
    Environment.Exit(0);
}

var Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder()
{
    DataSource = filename,
    Version = 3
}.ToString());
Connection.Open();

var newFile = "Records.db";
File.Delete(newFile);
var newDb = new LiteDatabase(newFile);
var newRecords = newDb.GetCollection<Record>();
new SQLiteCommand(@"
    CREATE TABLE IF NOT EXISTS `times` (
        `Id` INTEGER PRIMARY KEY AUTOINCREMENT,
        `Race` TEXT,
        `Player` TEXT,
        `Time` INTEGER,
        `Win` INTEGER
    );"
, Connection).ExecuteNonQuery();
var reader = new SQLiteCommand("SELECT * FROM `times`;", Connection).ExecuteReader();
int i = 0;
while (reader.Read())
{
    newRecords.Insert(new Record()
    {
        Race = reader["Race"].ToString(),
        Player = reader["Player"].ToString(),
        Time = (long)reader["Time"],
        Win = reader["Win"].ToString() == "1"
    });
    i++;
    Console.Write($"\rMigrated {i} records to {newFile}");
}
Console.WriteLine("\nMigration has completed, press enter to exit");
Console.ReadLine();
Connection.Close();
newDb.Dispose();

