using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Bars.RabbitMq;
using Bars.Worker;

namespace PublisherTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var producer = new Producer("localhost", "reports");

            ExecutionArguments message = new ExecutionArguments {TaskName = "SimpleReport"};

            var t = new ConsoleKeyInfo();
            do
            {
                producer.SendMessage(message);
                Console.WriteLine("Message is published");
                t = Console.ReadKey();
            } while (t.Key == ConsoleKey.Enter);
        }
    }
}
