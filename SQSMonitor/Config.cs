namespace SQSAlarms
{
    public class Config
    {
        public Group[] Groups { get; set; }
        
        public class Group
        {
            public string Name { get; set; }
            public string[] Queues { get; set; }
            public int Threshold { get; set; }
        }
    }
}