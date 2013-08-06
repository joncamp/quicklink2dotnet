﻿#region License

/* QuickLink2DotNet VideoViewer2 Example: Displays a list of available devices
 * and prompts the user to select one.  Then displays video from the selected
 * device.  Requires password and calibration files.
 *
 * Copyright © 2011-2013 Justin Weaver
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

#endregion License

#region Header Comments

/* $Id: QLTypes.cs 38 2011-05-09 01:07:39Z piranther $
 *
 * Author: Justin Weaver
 * Homepage: http://quicklinkapi4net.googlecode.com
 */

#endregion Header Comments

using System;
using System.IO;
using System.Windows.Forms;
using QLExampleHelper;
using QuickLink2DotNet;

namespace VideoViewer2
{
    internal static class Program
    {
        private static string dirname = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickLink2DotNet");

        // The file used to store the password.
        private static string filename_Password = System.IO.Path.Combine(dirname, "qlsettings.txt");

        private static void WaitForKeyPress()
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int[] deviceIDs = QLHelper.GetDeviceIDs();
            int deviceID = QLHelper.ChooseDevice(deviceIDs);
            string password = QLHelper.ReadPasswordFromFile(deviceID, filename_Password);
            if (password == null)
            {
                WaitForKeyPress();
                return;
            }

            QLError error = QuickLink2API.QLDevice_SetPassword(deviceID, password);
            if (error != QLError.QL_ERROR_OK)
            {
                Console.WriteLine("QLDevice_SetPassword() returned {0}.", error.ToString());
                WaitForKeyPress();
                return;
            }

            // Start the device.
            error = QuickLink2API.QLDevice_Start(deviceID);
            if (error != QLError.QL_ERROR_OK)
            {
                Console.WriteLine(string.Format("QLDevice_Start() returned {0}", error.ToString()));
                WaitForKeyPress();
                return;
            }
            Console.WriteLine("Device has been started.\n");

            Application.Run(new MainForm(deviceID));

            // Stop the device.
            error = QuickLink2API.QLDevice_Stop(deviceID);
            if (error != QLError.QL_ERROR_OK)
            {
                Console.WriteLine("QLDevice_Stop() returned {0}", error.ToString());
            }
            else
            {
                Console.WriteLine("Stopped Device.\n");
            }

            WaitForKeyPress();
        }
    }
}