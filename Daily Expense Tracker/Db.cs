// Db.cs
using System.IO;
using System;
using System.Data.SQLite;

public static class Db
{
    private static readonly string _dbPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");

    public static string ConnectionString => $"Data Source={_dbPath};";

    public static SQLiteConnection GetConn()
    {
       
        var firstTime = !File.Exists(_dbPath);
        var conn = new SQLiteConnection(ConnectionString);
        conn.Open();
        if (firstTime) Bootstrap(conn);
        return conn;
    }

    private static void Bootstrap(SQLiteConnection conn)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  username TEXT NOT NULL UNIQUE,
                  password_hash TEXT NOT NULL,
                  full_name TEXT
                );
                CREATE TABLE IF NOT EXISTS categories (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  name TEXT NOT NULL,
                  user_id INTEGER NULL,
                  UNIQUE(name, user_id)
                );
                CREATE TABLE IF NOT EXISTS expenses (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  user_id INTEGER NOT NULL,
                  category_id INTEGER NOT NULL,
                  amount REAL NOT NULL,
                  note TEXT,
                  spent_on DATE NOT NULL DEFAULT (DATE('now'))
                );
                CREATE TABLE IF NOT EXISTS income (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  user_id INTEGER NOT NULL,
                  amount REAL NOT NULL,
                  note TEXT,
                  received_on DATE NOT NULL DEFAULT (DATE('now')),
                  FOREIGN KEY(user_id) REFERENCES users(id)
                );
                INSERT OR IGNORE INTO categories (name, user_id) VALUES
                ('Food', NULL), ('Transport', NULL), ('Groceries', NULL),
                ('Bills', NULL), ('Entertainment', NULL);
            ";
            cmd.ExecuteNonQuery();
        }
        // Optional: seed one user for testing
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
              INSERT OR IGNORE INTO users (username, password_hash, full_name)
              VALUES (@u, @p, @n);
            ";
            cmd.Parameters.AddWithValue("@u", "admin");
            cmd.Parameters.AddWithValue("@p", "admin"); // change later to a hash
            cmd.Parameters.AddWithValue("@n", "Test User");
            cmd.ExecuteNonQuery();
        }
    }
}