using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace SQSAlarms
{
    class Program
    {
        static void Main(string[] args)
        {
            var accessKey = args.SkipWhile(x => x != "--accessKey").Skip(1).FirstOrDefault();
            var secretKey = args.SkipWhile(x => x != "--secretKey").Skip(1).FirstOrDefault();
            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                Console.WriteLine("Usage: --accessKey <ACCESS_KEY> --secretKey <SECRET_KEY>");
                return;
            }
            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            var configFile = File.ReadAllText("./config.json");
            var config = JsonConvert.DeserializeObject<Config>(configFile);

            ShowMonitoredQueues(config);

            using (var client = new AmazonSQSClient(credentials, RegionEndpoint.EUWest1))
            {
                while (true)
                {
                    MonitorQueue(config, client);
                    Thread.Sleep(5000);
                }
            }
        }

        private static void MonitorQueue(Config config, AmazonSQSClient client)
        {
            foreach (var @group in config.Groups)
            {
                foreach (var queue in group.Queues)
                {
                    try
                    {
                        var url = client.GetQueueUrl(queue);
                        var attributes = client.GetQueueAttributes(
                            new GetQueueAttributesRequest(url.QueueUrl,
                                new List<string> { "ApproximateNumberOfMessages" }));
                        if (attributes.ApproximateNumberOfMessages >= group.Threshold)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"{queue} has {attributes.ApproximateNumberOfMessages} messages");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Monitor exeption: {e.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        private static void ShowMonitoredQueues(Config config)
        {
            var queues = config.Groups.SelectMany(x => x.Queues);
            if (queues.Any())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Monitor started for SQS:");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                foreach (var queue in queues)
                {
                    Console.WriteLine($"{queue}");
                }
            }
        }
    }
}