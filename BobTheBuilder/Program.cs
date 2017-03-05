using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARGus;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace BobTheBuilder
{
    
    class Program
    {

        static void Main(string[] args)
        {
            bool succ;
            var res = Argus.Parse<Args>(args, out succ);
            if (!succ)
            {
                Argus.PrintUsage<Args>();
                return;
            }

            var error = new List<string>();
            var warning = new List<string>();

            var exceptions = res?.Exceptions?.ToArray() ?? new string[0];
            var projs = Directory.GetFiles(res.Path, "*", res.TopOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories).Where(file => file.EndsWith("proj") && exceptions.All(item => !IsSubDirectoryOf(file, item))).ToArray();


            foreach (var proj in projs)
            {
                try
                {
                    var project = new Project(proj);
                    if (!project.Build(new ConsoleLogger(LoggerVerbosity.Quiet)))
                    {
                        error.Add(proj);
                    }
                    
                }
                catch
                {
                    warning.Add(proj);
                }
            }
            Console.WriteLine();

            if (error.Count == 0 && warning.Count == 0)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All " + projs.Length + " projects successful!");
                Console.ForegroundColor = color;
            }
            else
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var s in warning)
                {
                    Console.WriteLine(s);
                }
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var s in error)
                {
                    Console.WriteLine(s);
                }
                Console.ForegroundColor = color;
            }
        }

        public static bool IsSubDirectoryOf(string candidate, string other)
        {
            var isChild = false;
            try
            {
                var candidateInfo = new DirectoryInfo(candidate);
                var otherInfo = new DirectoryInfo(other);

                while (candidateInfo.Parent != null)
                {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName)
                    {
                        isChild = true;
                        break;
                    }
                    candidateInfo = candidateInfo.Parent;
                }
            }
            catch { }

            return isChild;
        }
    }

    class Args
    {
        [ImplicitArgument(0, false, "Path", "Path to build")]
        public string Path;

        [ImplicitArgument(1, true, "Exceptions", "Paths that should not be built")]
        public IEnumerable<string> Exceptions;

        [SwitchArgument("OnlyTop", 't', "Current level only (no recursion)")]
        public bool TopOnly;
    }
}
