//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017-2018, Ilia Kosenkov, Tuorla Observatory, Finland

using System;


namespace Host
{
    internal class Host
    {
        private static readonly object Locker = new object();

        private static void Main(string[] args)
        {
            Console.WindowWidth = 180;
            Console.WindowHeight = 60;

            //Debug();

            using (var host = new DIPOL_Remote.Classes.DipolHost())
            {
                host.Host();
                host.EventReceived += (sender, message)
                    =>
                {
                    if (!(sender is ANDOR_CS.Classes.DebugCamera))
                    {
                        string senderString;
                        if (sender is ANDOR_CS.Classes.CameraBase cam)
                            senderString = $"{cam.CameraModel}/{cam.SerialNumber}";
                        else
                            senderString = sender.ToString();

                        lock (Locker)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("[{0,23:yyyy/MM/dd HH-mm-ss.fff}] @", DateTime.Now);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(" {0, 16}", senderString);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($": { message}");
                        }
                    }

                };


                while (Console.ReadKey().Key != ConsoleKey.Escape)
                { }
            }
        }

        private static void Debug()
        {
            var t = System.Diagnostics.Stopwatch.StartNew();
            using(var cam = new ANDOR_CS.Classes.Camera())
            {
                t.Stop();
                Console.WriteLine(cam.CameraModel + $"\t{t.ElapsedMilliseconds / 1000.0}");
                Console.ReadKey();
            }
        }
    }
}
