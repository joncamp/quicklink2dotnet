#region License

/* QLHelper: A class library containing some helper methods for use with
 * QuickLink2DotNet.
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using QuickLink2DotNet;

namespace QuickLink2DotNetHelper
{
    public partial class QLHelper
    {
        /// <summary>
        /// A Windows Form that displays a live video stream from the eye
        /// tracker device in quarter-size, half-size, or full size.  Press
        /// the space bar with the form in focus to cycle through sizes.
        /// </summary>
        public class VideoForm : Form
        {
            private QLHelper _helper;

            private Thread _frameReader_Thread;

            private QLFrameData _frameData;

            private object _frameData_Lock = new object();

            private bool _nextSize;
            private float _videoScale;

            private Bitmap _latestImage;

            private bool _disposed;

            private PictureBox _videoPictureBox;

            /// <summary>
            /// Constructor for a Windows Form that displays a live video stream from the eye tracker.
            /// This constructor allows for the specification of an initial display size.
            /// </summary>
            /// <seealso cref="T:QuickLink2DotNetHelper.QLHelper.VideoForm"/>
            /// <seealso cref="VideoForm(QLHelper)"/>
            /// <param name="helper">
            /// The <see cref="T:QuickLink2DotNetHelper.QLHelper" /> object of the device from which live video will be displayed.
            /// This object can be obtained by calling <see cref="QLHelper.ChooseDevice"/> or
            /// <see cref="QLHelper.FromDeviceId"/>.
            /// </param>
            /// <param name="sizeDivisor">
            /// Specifies the initial size of the display.  A value of 1 causes the full size to be used,
            /// 2 is half size, and 4 is quarter size.  Passing other values cause the default of '1' to
            /// be used.
            /// </param>
            public VideoForm(QLHelper helper, int sizeDivisor)
            {
                _helper = helper;

                switch (sizeDivisor)
                {
                    case 1:
                        _videoScale = 1f;
                        break;

                    case 2:
                        _videoScale = 0.5f;
                        break;

                    case 4:
                        _videoScale = 0.25f;
                        break;
                }

                _frameData = new QLFrameData();
                _latestImage = null;
                _nextSize = false;

                ClientSize = new Size((int)(helper._deviceInfo.sensorWidth * _videoScale), (int)(helper._deviceInfo.sensorHeight * _videoScale));
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;

                _videoPictureBox = new PictureBox();
                _videoPictureBox.Dock = DockStyle.Fill;
                _videoPictureBox.Paint += VideoForm_VideoPictureBox_Paint;
                Controls.Add(_videoPictureBox);

                FormClosing += VideoForm_FormClosing;
                KeyUp += VideoForm_KeyUp;
                Shown += VideoForm_Shown;

                _frameReader_Thread = new Thread(FrameReader);
            }

            /// <summary>
            /// Constructor for a Windows Form that displays a live video stream from the eye tracker
            /// device in quarter-size, half-size, or full size.  Press the space bar with the form in
            /// focus to cycle through sizes.  This constructor always makes a video form with an
            /// initially full-sized display.
            /// </summary>
            /// <seealso cref="VideoForm(QLHelper, int)"/>
            /// <param name="helper">
            /// The <see cref="T:QuickLink2DotNetHelper.QLHelper" /> object of the device from which live video will be displayed.
            /// This object can be obtained by calling <see cref="QLHelper.ChooseDevice"/> or
            /// <see cref="QLHelper.FromDeviceId"/>.
            /// </param>
            public VideoForm(QLHelper helper)
                : this(helper, 1)
            {
            }

            /// <summary>
            /// Clean up any resources being used.
            /// </summary>
            /// <param name="disposing">
            /// True if managed resources should be disposed; otherwise, false.
            /// </param>
            protected override void Dispose(bool disposing)
            {
                if (disposing && (!_disposed))
                {
                    _videoPictureBox.Dispose();
                    _disposed = true;
                }
                base.Dispose(disposing);
            }

            private void VideoForm_KeyUp(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Space)
                {
                    _nextSize = true;
                }
            }

            private void VideoForm_Shown(object sender, EventArgs e)
            {
                _frameReader_Thread.Start();
                while (!_frameReader_Thread.IsAlive) ;
            }

            private void VideoForm_FormClosing(object sender, FormClosingEventArgs e)
            {
                _frameReader_Thread.Abort();
                _frameReader_Thread.Join();
            }

            private void VideoForm_VideoPictureBox_Paint(object sender, PaintEventArgs e)
            {
                if (_nextSize)
                {
                    if (_videoScale == 1f)
                    {
                        _videoScale = 0.25f;
                    }
                    else if (_videoScale == 0.25f)
                    {
                        _videoScale = 0.5f;
                    }
                    else if (_videoScale == 0.5f)
                    {
                        _videoScale = 1f;
                    }

                    ClientSize = new Size((int)(_helper.DeviceInfo.sensorWidth * _videoScale), (int)(_helper.DeviceInfo.sensorHeight * _videoScale));
                    _nextSize = false;
                    _videoPictureBox.Invalidate();
                }
                else
                {
                    if (_latestImage == null)
                    {
                        return;
                    }

                    var g = e.Graphics;

                    g.DrawImage(_latestImage, 0, 0, _videoPictureBox.Width, _videoPictureBox.Height);

                    g.Flush(FlushIntention.Sync);

                    _latestImage.Dispose();
                    _latestImage = null;

                    lock (_frameData_Lock)
                    {
                        Monitor.Pulse(_frameData_Lock);
                    }
                }
            }

            private void FrameReader()
            {
                while (true)
                {
                    var error = QuickLink2API.QLDevice_GetFrame(_helper.DeviceId, 2000, ref _frameData);
                    if (error != QLError.QL_ERROR_OK)
                    {
                        Console.WriteLine("QLDevice_GetFrame() returned {0}.", error);
                        continue;
                    }

                    _latestImage = BitmapFromQLImageData(ref _frameData.ImageData);

                    _videoPictureBox.Invalidate();

                    lock (_frameData_Lock)
                    {
                        Monitor.Wait(_frameData_Lock);
                    }
                }
            }
        }
    }
}