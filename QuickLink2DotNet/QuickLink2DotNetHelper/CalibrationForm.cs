﻿#region License

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
        /// A Windows Form that performs an eye tracker calibration routine with either 5, 9, or 16
        /// points.  Each target is displayed for a settable duration with a settable delay for each
        /// target before calibration begins recording.  Optionally, attempts to retry calibration of any
        /// targets whose left-right averaged score exceeds a settable maximum value up to a settable
        /// maximum number of times.
        /// </summary>
        public class CalibrationForm : Form
        {
            // Tells the PictureBox paint event handler to draw the next target.
            private bool _drawTarget;

            // A lock object used for sleep/wake notification between the
            // calibration method and the paint event handler.
            private object _drawTargetLock = new object();

            // Tells the PictureBox paint event handler where to draw the next
            // target.
            private int _targetX, _targetY;

            private int _deviceId;

            private QLCalibrationType _calibrationType;

            private int _targetDuration;

            private int _numberOfTargets;

            private bool _disposed;
            private PictureBox _calibrationPictureBox;

            /// <summary>
            /// After the <see cref="Calibrate"/> method has completed the initial sequence of targets, it
            /// will retry the calibration of any targets whose left-right averaged score exceeds the
            /// value in <see cref="MaximumScore"/> up to this many times, or until the score is below
            /// the specified maximum.  This value may be changed from its default setting after the form
            /// is constructed, but before the <see cref="Calibrate"/> method is called.
            /// </summary>
            public int MaximumRetries
            {
                get { return _maximumRetries; }
                set { _maximumRetries = value; }
            }
            private int _maximumRetries = 2;

            /// <summary>
            /// After the <see cref="Calibrate"/> method has completed the initial sequence of targets, it
            /// will retry the calibration of any targets whose left-right averaged score exceeds this
            /// value up to <see cref="MaximumRetries"/> times, or until the score is below the specified
            /// maximum.  This value may be changed from its default setting after the form is
            /// constructed, but before the <see cref="Calibrate"/> method is called.
            /// </summary>
            public float MaximumScore
            {
                get { return _maximumScore; }
                set { _maximumScore = value; }
            }
            private float _maximumScore = 6.25f;

            /// <summary>
            /// A brief delay (in milliseconds) after each target is displayed, but before the
            /// <see cref="Calibrate"/> method begins to record the calibration of the target.  This is
            /// meant to give the user time to adjust to a new target on the screen.  This value may be
            /// changed from its default setting after the form is constructed, but before the
            /// <see cref="Calibrate"/> method is called.
            /// </summary>
            public int DelayTimePerTarget
            {
                get { return _delayTimePerTarget; }
                set { _delayTimePerTarget = value; }
            }
            private int _delayTimePerTarget = 600;

            /// <summary>
            /// The ID of the calibration container created when the <see cref="Calibrate"/> method calls
            /// the <see cref="QuickLink2DotNet.QuickLink2API.QLCalibration_Create"/> function.  Before
            /// the <see cref="Calibrate"/> method is called, this field has a value of zero.
            /// </summary>
            public int CalibrationId
            {
                get { return _calibrationId; }
            }
            private int _calibrationId;

            /// <summary>
            /// This array holds the target ID values received when the <see cref="Calibrate"/> method
            /// calls the <see cref="QuickLink2DotNet.QuickLink2API.QLCalibration_GetTargets"/> function.
            /// Before the <see cref="Calibrate"/> method is called, this array will contain only zeros.
            /// </summary>
            public QLCalibrationTarget[] Targets
            {
                get { return _targets; }
            }
            private QLCalibrationTarget[] _targets;

            /// <summary>
            /// The left-eye scores for each corresponding target in the <see cref="Targets"/> array.
            /// These values are received after the initial calibration sequence of the
            /// <see cref="Calibrate"/> method is completed and it calls the
            /// <see cref="QuickLink2DotNet.QuickLink2API.QLCalibration_GetScoring"/> function with the
            /// <see cref="QuickLink2DotNet.QLEyeType.QL_EYE_TYPE_LEFT"/> parameter.  Before the
            /// <see cref="Calibrate"/> method is called, this array will contain only zeros.
            /// </summary>
            public QLCalibrationScore[] LeftScores
            {
                get { return _leftScores; }
            }
            private QLCalibrationScore[] _leftScores;

            /// <summary>
            /// The right-eye scores for each corresponding target in the <see cref="Targets"/> array.
            /// These values are received after the initial calibration sequence of the
            /// <see cref="Calibrate"/> method is completed and it calls the
            /// <see cref="QuickLink2DotNet.QuickLink2API.QLCalibration_GetScoring"/> function with the
            /// <see cref="QuickLink2DotNet.QLEyeType.QL_EYE_TYPE_RIGHT"/> parameter.  Before the
            /// <see cref="Calibrate"/> method is called, this array will contain only zeros.
            /// </summary>
            public QLCalibrationScore[] RightScores
            {
                get { return _rightScores; }
            }
            private QLCalibrationScore[] _rightScores;

            /// <summary>
            /// Constructor for a Windows Form that performs an eye tracker calibration routine with
            /// either 5, 9, or 16 points.  Each target is displayed for a settable duration.
            /// </summary>
            /// <seealso cref="T:QuickLink2DotNetHelper.QLHelper.CalibrationForm"/>
            /// <param name="deviceId">
            /// The ID of the device to be calibrated.  This is the value found in the
            /// <see cref="QLHelper.DeviceId"/> field.
            /// </param>
            /// <param name="calibrationType">
            /// The type of calibration to perform (5, 9, or 16-point).  Any member of
            /// <see cref="QuickLink2DotNet.QLCalibrationType"/> is accepted.
            /// </param>
            /// <param name="targetDuration">
            /// The duration, in milliseconds, to show each target on the screen during the calibration
            /// sequence.
            /// </param>
            public CalibrationForm(int deviceId, QLCalibrationType calibrationType, int targetDuration)
            {
                _deviceId = deviceId;
                _calibrationType = calibrationType;
                _targetDuration = targetDuration;

                _calibrationId = 0;

                CalibrationTypeToNumberOfPoints(_calibrationType, out _numberOfTargets);

                _leftScores = new QLCalibrationScore[_numberOfTargets];
                _rightScores = new QLCalibrationScore[_numberOfTargets];
                _targets = new QLCalibrationTarget[_numberOfTargets];

                FormBorderStyle = FormBorderStyle.None;
                Width = Screen.PrimaryScreen.WorkingArea.Width;
                Height = Screen.PrimaryScreen.WorkingArea.Height;
                TopMost = false;
                WindowState = FormWindowState.Minimized;
                Hide();

                _calibrationPictureBox = new PictureBox();
                _calibrationPictureBox.BackColor = SystemColors.ButtonShadow;
                _calibrationPictureBox.Dock = DockStyle.Fill;
                _calibrationPictureBox.ClientSize = new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
                Controls.Add(_calibrationPictureBox);

                _calibrationPictureBox.Paint += CalibrationPictureBoxPaint;
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
                    _calibrationPictureBox.Dispose();

                    _disposed = true;
                }
                base.Dispose(disposing);
            }

            private void CalibrationPictureBoxPaint(object sender, PaintEventArgs e)
            {
                if (_drawTarget)
                {
                    // We have been commanded to draw the current target.

                    TopMost = true;

                    var g = e.Graphics;

                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Paint the target.
                    var radius = 40;
                    var diameter = radius * 2;
                    g.FillEllipse(Brushes.Black, _targetX - radius, _targetY - radius, diameter, diameter);
                    radius = 38;
                    diameter = radius * 2;
                    g.FillEllipse(Brushes.Gray, _targetX - radius, _targetY - radius, diameter, diameter);
                    radius = 10;
                    diameter = radius * 2;
                    g.FillEllipse(Brushes.Yellow, _targetX - radius, _targetY - radius, diameter, diameter);

                    g.Flush();

                    _drawTarget = false;

                    // Tell the calibration thread we are done drawing the target.
                    lock (_drawTargetLock)
                    {
                        Monitor.Pulse(_drawTargetLock);
                    }
                }
            }

            private bool CalibrateTarget(int targetIndex)
            {
                if (!Visible)
                {
                    Show();
                }

                if (!TopMost)
                {
                    TopMost = true;
                }

                if (WindowState != FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Maximized;
                }

                if (ActiveForm != this)
                {
                    Activate();
                }

                _targetX = (int)Math.Truncate((Targets[targetIndex].x / 100f) * Size.Width);
                _targetY = (int)Math.Truncate((Targets[targetIndex].y / 100f) * Size.Height);

                var success = false;

                while (true)
                {
                    // Tell the picture box to draw the new target, and wait for it
                    // to wake us.
                    _drawTarget = true;
                    _calibrationPictureBox.Refresh();
                    lock (_drawTargetLock)
                    {
                        while (_drawTarget)
                        {
                            Monitor.Wait(_drawTargetLock);
                        }
                    }

                    // Give the user time to look at the target.
                    Thread.Sleep(_delayTimePerTarget);

                    // Calibrate the target.
                    var error = QuickLink2API.QLCalibration_Calibrate(_calibrationId, Targets[targetIndex].targetId, _targetDuration, true);
                    if (error != QLError.QL_ERROR_OK)
                    {
                        Console.WriteLine("QLCalibration_Calibrate() returned {0}.", error);
                        success = false;
                        break;
                    }

                    // Get the status of the last target.
                    QLCalibrationStatus status;
                    error = QuickLink2API.QLCalibration_GetStatus(_calibrationId, Targets[targetIndex].targetId, out status);
                    if (error != QLError.QL_ERROR_OK)
                    {
                        Console.WriteLine("QLCalibration_GetStatus() returned {0}.", error);
                        success = false;
                        break;
                    }

                    if (status == QLCalibrationStatus.QL_CALIBRATION_STATUS_NO_LEFT_DATA)
                    {
                        var result = MessageBox.Show("Left eye not found.  Retry?", "Left Eye Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result != DialogResult.Yes)
                        {
                            Console.WriteLine("User cancelled.");
                            success = false;
                            break;
                        }
                    }
                    else if (status == QLCalibrationStatus.QL_CALIBRATION_STATUS_NO_RIGHT_DATA)
                    {
                        var result = MessageBox.Show("Right eye not found.  Retry?", "Right Eye Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result != DialogResult.Yes)
                        {
                            Console.WriteLine("User cancelled.");
                            success = false;
                            break;
                        }
                    }
                    else if (status == QLCalibrationStatus.QL_CALIBRATION_STATUS_NO_DATA)
                    {
                        var result = MessageBox.Show("Neither eye found.  Retry?", "Neither Eye Found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result != DialogResult.Yes)
                        {
                            Console.WriteLine("User cancelled.");
                            success = false;
                            break;
                        }
                    }
                    else if (status == QLCalibrationStatus.QL_CALIBRATION_STATUS_OK)
                    {
                        success = true;
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Calibration failed!");
                        success = false;
                        break;
                    }
                }

                return success;
            }

            private bool UpdateScores()
            {
                // Get scores.
                for (var i = 0; i < _numberOfTargets; i++)
                {
                    var error = QuickLink2API.QLCalibration_GetScoring(_calibrationId, Targets[i].targetId, QLEyeType.QL_EYE_TYPE_LEFT, out LeftScores[i]);
                    if (error != QLError.QL_ERROR_OK)
                    {
                        Console.WriteLine("QLCalibration_GetScoring(left) returned {0}.", error);
                        return false;
                    }

                    error = QuickLink2API.QLCalibration_GetScoring(_calibrationId, Targets[i].targetId, QLEyeType.QL_EYE_TYPE_RIGHT, out RightScores[i]);
                    if (error != QLError.QL_ERROR_OK)
                    {
                        Console.WriteLine("QLCalibration_GetScoring(right) returned {0}.", error);
                        return false;
                    }
                }

                return true;
            }

            private bool ImproveCalibration()
            {
                var retryCount = new int[_numberOfTargets];
                bool finished;
                do
                {
                    finished = true;
                    for (var i = 0; i < _numberOfTargets; i++)
                    {
                        var score = (LeftScores[i].score + RightScores[i].score) / 2f;

                        if (score > _maximumScore && retryCount[i] <= MaximumRetries)
                        {
                            Console.Write("Score {0:f} exceeds maximum of {1:f}.", score, _maximumScore);
                            if (retryCount[i] < MaximumRetries)
                            {
                                Console.WriteLine("  Retrying.");
                                if (!CalibrateTarget(i))
                                {
                                    // Error.
                                    return false;
                                }
                                if (!UpdateScores())
                                {
                                    // Error.
                                    return false;
                                }
                            }
                            else if (retryCount[i] == MaximumRetries)
                            {
                                Console.WriteLine("  Too many retries.  Giving up on this target.");
                            }
                            retryCount[i]++;
                            finished = false;
                        }
                    }
                } while (!finished);

                return true;
            }

            /// <summary>
            /// Performs the calibration sequence, displaying 5, 9, or 16-targets as specified in the
            /// form's constructor.  After each target is displayed, but before each target is
            /// calibrated, the method pauses for <see cref="DelayTimePerTarget"/> milliseconds in order
            /// to give the user time to adjust to the newly displayed target.  After the initial
            /// sequence is complete, this method retries the calibration of any targets whose left-right
            /// averaged score exceeds the value in <see cref="MaximumScore"/> up to
            /// <see cref="MaximumRetries"/> times.  The fields <see cref="CalibrationId"/>,
            /// <see cref="Targets"/>, <see cref="LeftScores"/>, and <see cref="RightScores"/> will be
            /// set to valid values when this method returns successfully.
            /// </summary>
            /// <returns>
            /// True on success (even if some targets exceed <see cref="MaximumScore" />, false on
            /// failure.
            /// </returns>
            /// <exception cref="DllNotFoundException">
            /// The QuickLink2 DLLs ("QuickLink2.dll," "PGRFlyCapture.dll," and "SMX11MX.dll") must be placed
            /// in the same directory as your program's binary executable; otherwise, this exception will be
            /// thrown.
            /// </exception>
            public bool Calibrate()
            {
                // Create a new calibration container.
                var error = QuickLink2API.QLCalibration_Create(0, out _calibrationId);
                if (error != QLError.QL_ERROR_OK)
                {
                    Console.WriteLine("QLCalibration_Create() returned {0}.", error);
                    return false;
                }

                error = QuickLink2API.QLCalibration_Initialize(_deviceId, _calibrationId, _calibrationType);
                if (error != QLError.QL_ERROR_OK)
                {
                    Console.WriteLine("QLCalibration_Initialize() returned {0}.", error);
                    return false;
                }

                var numTargets = _numberOfTargets;
                error = QuickLink2API.QLCalibration_GetTargets(_calibrationId, ref numTargets, Targets);
                if (error != QLError.QL_ERROR_OK)
                {
                    Console.WriteLine("QLCalibration_GetTargets() returned {0}.", error);
                    return false;
                }
                if (numTargets != _numberOfTargets)
                {
                    Console.WriteLine("QLCalibration_GetTargets() returned an unexpected number of targets.  Expected {0}; got {1}.", _numberOfTargets, numTargets);
                    return false;
                }

                //this.TopMost = true;
                //this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                //this.Show();

                // For each target, draw it and then perform calibration.
                for (var i = 0; i < numTargets; i++)
                {
                    if (!CalibrateTarget(i))
                    {
                        return false;
                    }
                }

                if (!UpdateScores())
                {
                    return false;
                }

                if (_maximumScore > 0)
                {
                    if (!ImproveCalibration())
                    {
                        return false;
                    }
                }

                //this.TopMost = false;
                //this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                Hide();

                return true;
            }
        }
    }
}