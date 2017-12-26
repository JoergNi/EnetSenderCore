using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            new SynchronousSocketClient().StartClient();
        }
    }
}
