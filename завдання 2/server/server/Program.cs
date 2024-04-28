using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


class CurrencyServer
{
    static Dictionary<string, double> exchangeRates = new Dictionary<string, double>()
    {
        { "USD_EUR", 0.85 },
        { "EUR_USD", 1.18 }
    };
    static Dictionary<string, int> clientRequestCounts = new Dictionary<string, int>();
    static Dictionary<string, DateTime> lastRequestTimes = new Dictionary<string, DateTime>();
    static int maxRequestsPerMinute = 5;
    static void Main(string[] args)
    {
        Console.WriteLine("Виберіть режим введення кількості запитів:");
        Console.WriteLine("1. Введіть кількість запитів за замовчуванням (5)");
        Console.WriteLine("2. Введіть кількість запитів вручну");
        int choice;
        if (!int.TryParse(Console.ReadLine(), out choice) || (choice != 1 && choice != 2))
        {
            Console.WriteLine("Некоректний вибір. Завершення програми.");
            return;
        }
        if (choice == 2)
        {
            Console.WriteLine("Введіть кількість запитів:");
            if (!int.TryParse(Console.ReadLine(), out maxRequestsPerMinute) || maxRequestsPerMinute <= 0)
            {
                Console.WriteLine("Некоректне значення. Встановлено значення за замовчуванням (5).");
                maxRequestsPerMinute = 5;
            }
        }
        Console.WriteLine($"Максимальна кількість запитів на хвилину налаштована на {maxRequestsPerMinute}");
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 1234;
        TcpListener listener = new TcpListener(ipAddress, port);
        listener.Start();
        Console.WriteLine("Сервер запущено...");
        Timer timer = new Timer(ResetRequestCounts, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine($"Клієнт приєднався о {DateTime.Now}");
            IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            string clientKey = $"{clientEndPoint.Address}:{clientEndPoint.Port}";
            if (!clientRequestCounts.ContainsKey(clientKey))
            {
                clientRequestCounts.Add(clientKey, 0);
            }
            if (!lastRequestTimes.ContainsKey(clientKey))
            {
                lastRequestTimes.Add(clientKey, DateTime.Now);
            }
            if (clientRequestCounts[clientKey] >= maxRequestsPerMinute)
            {
                Console.WriteLine("Клієнт перевищив ліміт запитів. Відхиляємо з'єднання.");
                client.Close();
                continue;
            }
            clientRequestCounts[clientKey]++;
            lastRequestTimes[clientKey] = DateTime.Now;

            HandleClient(client);
            Console.WriteLine($"Клієнт відключився о {DateTime.Now}");
        }
    }
    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine("Отримано запит: " + request);
        string response = GetExchangeRate(request);
        byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
        stream.Write(responseBuffer, 0, responseBuffer.Length);
        Console.WriteLine("Надіслано відповідь: " + response);
        client.Close();
    }
    static string GetExchangeRate(string request)
    {
        string[] currencies = request.Split('_');
        string key = currencies[0] + "_" + currencies[1];
        if (exchangeRates.ContainsKey(key))
        {
            double rate = exchangeRates[key];
            return $"{key}: {rate}";
        }
        else
        {
            return "Курс обміну не знайдено.";
        }
    }
    static void ResetRequestCounts(object state)
    {
        foreach (var clientKey in clientRequestCounts.Keys)
        {
            if (DateTime.Now - lastRequestTimes[clientKey] >= TimeSpan.FromMinutes(1))
            {
                clientRequestCounts[clientKey] = 0;
            }
        }
    }
}