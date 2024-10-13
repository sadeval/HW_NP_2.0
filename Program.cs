using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CurrencyExchangeServer
{
    class Program
    {
        private static readonly Dictionary<string, double> exchangeRates = new Dictionary<string, double>
        {
            { "USD_EUR", 0.95 }, 
            { "EUR_USD", 1.05 } 
        };

        private static readonly string logFilePath = "connection_log.txt";

        static void Main()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 5500);
            server.Start();
            Console.WriteLine("Сервер запущен и ожидает подключений...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private static void HandleClient(TcpClient client)
        {
            string clientEndPoint = client.Client.RemoteEndPoint.ToString();
            LogConnection(clientEndPoint, "подключен");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"Получен запрос: {request} от {clientEndPoint}");

                string response = GetExchangeRate(request);
                byte[] responseData = Encoding.UTF8.GetBytes(response);
                stream.Write(responseData, 0, responseData.Length);

                LogConnection(clientEndPoint, $"запрошен курс: {request} => {response}");
            }

            LogConnection(clientEndPoint, "отключен");
            client.Close();
        }

        private static string GetExchangeRate(string request)
        {
            if (exchangeRates.TryGetValue(request.ToUpper().Replace("=>", "_"), out double rate))
            {
                return $"Курс {request}: {rate}";
            }
            else
            {
                return "Курс валют не найден. Доступны только USD и EUR.";
            }
        }

        private static void LogConnection(string client, string message)
        {
            string logMessage = $"{DateTime.Now}: Клиент {client} {message}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
