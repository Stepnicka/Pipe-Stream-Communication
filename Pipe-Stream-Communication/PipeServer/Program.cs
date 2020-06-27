using DataSharingLibrary;
using DataSharingLibrary.Interfaces;
using DataSharingLibrary.Server;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PipeServerApp
{
    class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!You are server!");

            var caller = new methodCaller();

            var logger = new logger();
            var methods = new MethodStack();
            methods.TryAddMethod<PipeRequest<string>, string, int>(caller.WriteCall);

            var server = new PipeServer(logger, "listener", methods);

            server.Start(new System.Threading.CancellationToken());

            Console.WriteLine("..");
            Console.ReadKey();

            server.Stop();
        }
    }

    public class methodCaller
    {
        public int WriteCall(string call)
        {
            Console.WriteLine(call);

            return 1;
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
