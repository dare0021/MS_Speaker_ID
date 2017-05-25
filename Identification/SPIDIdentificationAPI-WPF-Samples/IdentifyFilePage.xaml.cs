﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
// 
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-SpeakerRecognition-Windows
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace SPIDIdentificationAPI_WPF_Samples
{
    /// <summary>
    /// Interaction logic for IdentifyFilePage.xaml
    /// </summary>
    public partial class IdentifyFilePage : Page
    {
        private string _selectedFile = "";

        private SpeakerIdentificationServiceClient _serviceClient;

        /// <summary>
        /// Constructor to initialize the Identify File page
        /// </summary>
        public IdentifyFilePage()
        {
            InitializeComponent();

            _speakersListFrame.Navigate(SpeakersListPage.SpeakersList);

            MainWindow window = (MainWindow)Application.Current.MainWindow;
            _serviceClient = new SpeakerIdentificationServiceClient(window.ScenarioControl.SubscriptionKey);
        }

        private void _loadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV Files(*.wav)|*.wav";
            bool? result = openFileDialog.ShowDialog(window);

            if (!(bool)result)
            {
                window.Log("No File Selected.");
                return;
            }
            window.Log("File Selected: " + openFileDialog.FileName);
            _selectedFile = openFileDialog.FileName;
        }

        private async void identify(string path, bool shortAudio)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            try
            {
                if (path == "")
                    throw new Exception("No File Selected.");

                window.Log("Identifying File...");
                Profile[] selectedProfiles = SpeakersListPage.SpeakersList.GetSelectedProfiles();
                Guid[] testProfileIds = new Guid[selectedProfiles.Length];
                for (int i = 0; i < testProfileIds.Length; i++)
                {
                    testProfileIds[i] = selectedProfiles[i].ProfileId;
                }

                OperationLocation processPollingLocation;
                using (Stream audioStream = File.OpenRead(path))
                {
                    processPollingLocation = await _serviceClient.IdentifyAsync(audioStream, testProfileIds, shortAudio);
                }

                IdentificationOperation identificationResponse = null;
                int numOfRetries = 10;
                TimeSpan timeBetweenRetries = TimeSpan.FromSeconds(5.0);
                while (numOfRetries > 0)
                {
                    await Task.Delay(timeBetweenRetries);
                    identificationResponse = await _serviceClient.CheckIdentificationStatusAsync(processPollingLocation);

                    if (identificationResponse.Status == Status.Succeeded)
                    {
                        break;
                    }
                    else if (identificationResponse.Status == Status.Failed)
                    {
                        throw new IdentificationException(identificationResponse.Message);
                    }
                    numOfRetries--;
                }
                if (numOfRetries <= 0)
                {
                    throw new IdentificationException("Identification operation timeout.");
                }

                window.Log("Identification Done.");

                _identificationResultTxtBlk.Text = identificationResponse.ProcessingResult.IdentifiedProfileId.ToString();
                _identificationResultAliasTxtBlk.Text = AliasFile.RetrieveAlias(identificationResponse.ProcessingResult.IdentifiedProfileId);
                _identificationConfidenceTxtBlk.Text = identificationResponse.ProcessingResult.Confidence.ToString();
                _identificationResultStckPnl.Visibility = Visibility.Visible;
            }
            catch (IdentificationException ex)
            {
                window.Log("Speaker Identification Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                window.Log("Error: " + ex.Message);
            }
        }

        private void _scriptBtn_Click(object sender, RoutedEventArgs e)
        {
            _identificationResultStckPnl.Visibility = Visibility.Visible;
            _identificationResultTxtBlk.Text = "Partitioning input file...";

            string path = _selectedFile;
            _selectedFile = "";
            string parentFolderPath = path.Substring(0, path.LastIndexOf('\\')+1);
            string inFileName = path.Substring(path.LastIndexOf('\\') + 1);
            parentFolderPath += DateTime.Now.ToString("MM.dd_HHmmss");
            Directory.CreateDirectory(parentFolderPath);
            WaveHelper.LoadFile(path);
            int byteLength = WaveHelper.GetAudioByteLength();
            int audioLength = (int)Math.Ceiling(WaveHelper.GetAudioLength());
            // partial copy instruction: sox infile outfile trim startsecond endsecond

            ArrayList files = new ArrayList();
            for (int i=0; i < audioLength - 3; i++)
            {
                string outPath = parentFolderPath + "/" + inFileName + i + ".wav";
                int endTime = i + 3 >= audioLength ? -1 : i + 3;
                files.Add(outPath);
                CopyAudioFileSegment(path, outPath, i, endTime);
            }
        }

        /// <summary>
        /// sox doesn't like it if endsecond > audiolen, but will take a copy without an end second
        /// </summary>
        /// <param name="inPath"></param>
        /// <param name="outPath"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private void CopyAudioFileSegment(string inPath, string outPath, int startTime, int endTime = -1)
        {
            string args = " \"" + inPath + "\" \"" + outPath + "\" trim " + startTime;
            if (endTime >= 0)
            {
                args += " " + endTime;
            }
            RunSox(args);
        }

        private void RunSox(string args)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "C:/Program Files (x86)/sox-14-4-2/sox.exe";
            startInfo.Arguments = args;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            using (Process soxProc = Process.Start(startInfo))
            {
                soxProc.WaitForExit();
            }
        }

        private void _identifyBtn_Click(object sender, RoutedEventArgs e)
        {
            identify(_selectedFile, (sender as Button) == _identifyShortAudioBtn);
            _selectedFile = "";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SpeakersListPage.SpeakersList.SetMultipleSelectionMode();
            SpeakersListPage.SpeakersList.SelectAll();
        }
    }
}