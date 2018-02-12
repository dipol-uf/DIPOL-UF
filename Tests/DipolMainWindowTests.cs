using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using DIPOL_UF.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using TestStack.White.Factory;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;

namespace Tests
{
    [TestClass]
    public class DipolMainWindowTests
    {
        public static TestContext Context;
        public static string ExePath;

        [ClassInitialize]
        public static void Test_ClassInitialize(TestContext context)
        {
            Context = context;
            var list = Context.TestDir.Split('/', '\\');
            var pathCollection = list.Take(list.Length - 2).ToList();
            pathCollection.Add(@"DIPOL-UF");
            pathCollection.Add(@"bin");
            pathCollection.Add(@"x64");
            pathCollection.Add(@"Debug");
            pathCollection.Add(@"DIPOL-UF.exe");

            ExePath = pathCollection.Aggregate((sum, x) => $"{sum}\\{x}");
        }

        [TestInitialize]
        public void Test_Initialize()
        {
           
        }

        [TestMethod]
        public void Test_WindowInitialState()
        {

            using (var app = Application.Launch(ExePath))
            {

                var winds = app.GetWindows();
                Window main;

                Assert.IsNotNull((main = winds.FirstOrDefault(w => w.Title =="Dipol-UF")));

                var connectButton = main.Items.FirstOrDefault(item => item is Button b && b.Text == "Connect");

                Assert.IsNotNull(connectButton);

                SpinWait.SpinUntil(() => connectButton.Enabled, TimeSpan.FromSeconds(20));

                connectButton.Click();

                Assert.AreNotEqual(app.GetWindows().Count, winds.Count);
                app.Close();
            }
        }
    }
}
