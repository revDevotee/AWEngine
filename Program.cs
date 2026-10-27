using System.Diagnostics;
using System.Text;

namespace AWEngine
{
    
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("??????????");
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://i.pinimg.com/736x/75/1c/aa/751caa6c10cded1a7b525979a32a8ae9.jpg",
                UseShellExecute = true
            });
        }
    }
}
