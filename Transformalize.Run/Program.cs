﻿/*
Transformalize - Replicate, Transform, and Denormalize Your Data...
Copyright (C) 2013 Dale Newman

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using Transformalize.Core;
using Transformalize.Core.Process_;
using Transformalize.Libs.NLog;
using Transformalize.Runner;

namespace Transformalize.Run
{
    class Program
    {
        private static readonly System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static Options _options = new Options();

        static void Main(string[] args)
        {
            var process = new Process();

            if (args.Length == 0)
            {
                Log.Error("Please provide the process name (e.g. Tfl MyProcess)");
                return;
            }

            var arg = args[0];

            Timer.Start();

            var configuration = arg.EndsWith(".xml") ? new ProcessXmlConfigurationReader(arg).Read() : new ProcessConfigurationReader(arg).Read();

            if (OptionsMayExist(args))
            {
                _options = new Options(CombineArguments(args));
                if (_options.Valid())
                {
                    process = new ProcessReader(configuration, _options).Read();
                }
                else
                {
                    foreach (var problem in _options.Problems)
                    {
                        Log.Error(arg + " | " + problem);
                    }
                    Log.Warn(arg + " | Aborting process.");
                    Environment.Exit(1);
                }
            }
            else
            {
                process = new ProcessReader(configuration).Read();
            }

            new ProcessRunner(process).Run();

            Timer.Stop();

            Log.Info("{0} | Process completed in {1}.", arg, Timer.Elapsed);

            if (_options.Mode != Modes.Test) return;

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static string CombineArguments(IEnumerable<string> args)
        {
            var options = new List<string>(args);
            options.RemoveAt(0);
            return string.Join(string.Empty, options);
        }

        private static bool OptionsMayExist(ICollection<string> args)
        {
            return args.Count > 1;
        }
    }
}
