using DataSharingLibrary;
using DataSharingLibrary.Client;
using DataSharingLibrary.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace PipeClientApp
{
    class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        static async Task Main(string[] args)
        {

            Console.WriteLine("Hello World!You are client!");

            var logger = new logger();
            var client = new PipeClient("listener", logger);

            try
            {
                var token = new CancellationToken();

                await client.Start(token);

                var request = new PipeRequest<string>()
                {
                    RequestName = "WriteCall",
                    Parameter = "Kabooom!"
                };

                var result = await client.SendMessage<int, string>(request, token);

                Console.WriteLine("Result:");
                Console.WriteLine($"\t IS succes? {result.IsSuccess}");
                Console.WriteLine($"\t Message: {result.ErrorMessage}");
                Console.WriteLine($"\t Data: {result.Data}");

                client.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR : {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("...");
            Console.ReadKey();
        }
    }

    public class logger : IPipeLogger
    {
        public void Debug(object message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }

        public void Error(object message)
        {
            Console.WriteLine($"ERROR: {message}");
        }

        public void Info(object message)
        {
            Console.WriteLine($"INFO: {message}");
        }
    }
}
