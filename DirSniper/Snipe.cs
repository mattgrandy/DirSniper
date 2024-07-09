using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirSniper;

namespace DirSniper
{
    internal class Snipe
    {
        public string Url, Output, Agent;
        public int Delay, Threads, Timeout;

        public List<string> Directories;

        public Snipe()
        {
            this.Url = Program.Options.Instance.Url;
            this.Output = Program.Options.Instance.Output;
            this.Threads = Program.Options.Instance.Threads;
            this.Delay = Program.Options.Instance.Delay;
            this.Agent = Program.Options.Instance.Agent;
            this.Timeout = Program.Options.Instance.Timeout;
            this.Directories = new List<string>();
        }
    }
}
