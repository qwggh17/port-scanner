using System;
using System.Linq.Expressions;
using System.Net.Sockets;
using Microsoft.Data.Sqlite;

class Program
{
    static string connectionString = "Data Source=scan_history.db";

    static void Main()
    { //создаем базу данных
        CreateDatabase();
        //через цикл создаем меню программы
        while (true)
        {
            Console.WriteLine("\n=== PORT SCANNER ===");
            Console.WriteLine("1 - проверить ссылку");
            Console.WriteLine("2 - показать историю запросов");
            Console.WriteLine("0 - выход");
            Console.WriteLine("выберите действие: ");

            string choice = Console.ReadLine() ?? "";
            //прописываем условия выбора
            if (choice == "1")
            {
                ScanURLMenu();
            }
            else if (choice == "2")
            {
                ShowHistory();
            }
            else if (choice == "3")
            {
                break;
            }
            else
            {
                Console.WriteLine("неверный выбор");
            }
        }
    }
    //прописываем тело базы данных
    static void CreateDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS scan_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                url TEXT NOT NULL,
                host TEXT NOT NULL,
                port INTEGER NOT NULL,
                result TEXT NOT NULL,
                scan_time TEXT NOT NULL
            );
            ";
        command.ExecuteNonQuery();
    }
    //прописываем тело меню
    static void ScanURLMenu()
    {
        Console.Write("введите URL-адрес сайта:");
        string input = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("ошибка! ссылка не может быть пустой.");
            return;
        }
        if (!input.StartsWith("http://") && !input.StartsWith("https://"))
                {
            input = "http://" + input;
        }
        Uri uri;

        try
        {
            uri = new Uri(input);
        }
        catch {
            Console.WriteLine("ошибка! неверный формат ссылки.");
            return;
        }
        
        string url = uri.ToString();
        string host = uri.Host;
        int port = uri.Port;

        string result = CheckPort(host, port);

        Console.WriteLine($"URL:{url}");
        Console.WriteLine($"Хост: {host}");
        Console.WriteLine($"Определенный порт: {port}");
        Console.WriteLine($"Результат: {result}");

        SaveResult(url, host, port, result);
        Console.WriteLine();
        Console.WriteLine("нажмите любую клавишу для возврата в меню");
        Console.ReadKey();
    }

    static string CheckPort(string host, int port)
    {
        try
        {
            using TcpClient client = new TcpClient();

            var connectTask = client.ConnectAsync(host, port);

            bool connected = connectTask.Wait(2000);
            
            if (connected && client.Connected)
            {
                return "Открыт";
            }
            else
            {
                return "закрыт / недоступен";
            }
            
        }
        catch
        {
            return "ошибка подключения";
        }
    }
    static void SaveResult(string url, string host, int port, string result)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            @"
            INSERT INTO scan_history (url, host, port, result, scan_time)
            VALUES ($url, $host, $port, $result, $scan_time);
            ";

        command.Parameters.AddWithValue("$url", url);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$port", port);
        command.Parameters.AddWithValue("$result", result);
        command.Parameters.AddWithValue("$scan_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        command.ExecuteNonQuery();
    }
    static void ShowHistory()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            @"
            SELECT id, url, host, port, result, scan_time
            FROM scan_history
            ORDER BY id DESC;
            ";
        using var reader = command.ExecuteReader();

        Console.WriteLine("\n=== история запросов ===");

        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string url = reader.GetString(1);
            string host = reader.GetString(2);
            int port = reader.GetInt32(3);
            string result = reader.GetString(4);
            string scanTime = reader.GetString(5);

            Console.WriteLine($"{id}. {url} | {host}:{port} | {result} | {scanTime}");
        }

    }
}
