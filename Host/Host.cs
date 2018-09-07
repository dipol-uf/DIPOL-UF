//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

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
