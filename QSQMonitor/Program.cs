using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.Runtime;
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

            var configFile = File.ReadAllText("./config.json");
            var config = JsonConvert.DeserializeObject<Config>(configFile);

            var queues = config.Groups.Select(x => x.Queues);
            if (queues.Any())
            {
                Console.WriteLine($"Monitor started for SQS:");
                foreach (var queue in queues)
                {
                    Console.WriteLine($"{queue}");
                }
            }
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            using (var client = new Amazon.SQS.AmazonSQSClient(credentials, RegionEndpoint.EUWest1))
            {
                while (true)
                {
                    foreach (var @group in config.Groups)
                    {
                        foreach (var queue in group.Queues)
                        {
                            var url = client.GetQueueUrl(queue);
                            var attributes = client.GetQueueAttributes(
                                new GetQueueAttributesRequest(url.QueueUrl,
                                    new List<string> { "ApproximateNumberOfMessages" }));
                            if (attributes.ApproximateNumberOfMessages >= int.Parse(group.Threshold))
                            {
                                Console.WriteLine($"{queue} has {attributes.ApproximateNumberOfMessages} messages");
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }                
            }
        }
    }

    public class Config
    {
        public Group[] Groups { get; set; }
        
        public class Group
        {
            public string Name { get; set; }
            public string[] Queues { get; set; }
            public string Threshold { get; set; }
        }
    }
}