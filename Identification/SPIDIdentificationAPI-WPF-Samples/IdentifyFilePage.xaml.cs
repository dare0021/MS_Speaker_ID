// 
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
using System.Globalization;
using System.Threading;

namespace SPIDIdentificationAPI_WPF_Samples
{
    /// <summary>
    /// Interaction logic for IdentifyFilePage.xaml
    /// </summary>
    public partial class IdentifyFilePage : Page
    {
        private string _selectedFile = "";
        private string[] _selectedFilesList = null;

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
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog(window);

            if (!(bool)result)
            {
                window.Log("No File Selected.");
                return;
            }
            if (openFileDialog.FileNames.Length < 2)
            {
                window.Log("File Selected: " + openFileDialog.FileName);
                _selectedFile = openFileDialog.FileName;
                _selectedFilesList = null;
            }
            else
            {
                window.Log("Files Selected:");
                foreach (var s in openFileDialog.FileNames)
                {
                    window.Log(s);
                }
                window.Log("Representative file: " + openFileDialog.FileNames[0]);
                _selectedFile = openFileDialog.FileNames[0];
                _selectedFilesList = openFileDialog.FileNames;
            }
        }

        private async Task<IdentificationOperation> identify(string path, bool shortAudio)
        {
            IdentificationOperation identificationResponse = null;
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            try
            {
                if (path == "")
                {
                    throw new Exception("No File Selected.");
                }

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

            }
            catch (IdentificationException ex)
            {
                window.Log("Speaker Identification Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                window.Log("Error: " + ex.Message);
            }
            return identificationResponse;
        }

        private async void _scriptBtn_Click(object sender, RoutedEventArgs e)
        {
            // maybe rig this in the GUI at some point
            bool childIsCorrect = true;
            bool uploadEnabled = true;
            
            string[] paths;

            if (_selectedFilesList != null)
            {
                paths = _selectedFilesList;
            }
            else
            {
                paths = new string[] { _selectedFile };
            }
            
            _selectedFile = "";
            _selectedFilesList = null;

            foreach (string path in paths)
            {
                string parentFolderPath = path.Substring(0, path.LastIndexOf('.') + 1);
                string inFileName = path.Substring(path.LastIndexOf('\\') + 1);
                parentFolderPath += DateTime.Now.ToString("MM.dd_HHmmss");
                Directory.CreateDirectory(parentFolderPath);
                WaveHelper.LoadFile(path);
                int byteLength = WaveHelper.GetAudioByteLength();
                int audioLength = (int)Math.Ceiling(WaveHelper.GetAudioLength());
                StatsHelper recorder = new StatsHelper(childIsCorrect);

                for (int i = 0; i < audioLength - 3; i++)
                {
                    _scriptTxtBlk.Text = "Running file " + (i + 1) + " / " + (audioLength - 3);
                    string outPath = parentFolderPath + "/" + inFileName + i + ".wav";
                    int endTime = i + 3 >= audioLength ? -1 : i + 3;
                    CopyAudioFileSegment(path, outPath, i, endTime);
                    if (uploadEnabled)
                    {
                        var result = await identify(outPath, true);
                        DisplayResults(result);
                        TrackStats(recorder, result);
                    }
                }

                if (uploadEnabled)
                {
                    recorder.SaveLog(parentFolderPath + "/log.log", true);
                }
            }
        }

        private void TrackStats(StatsHelper recorder, IdentificationOperation result)
        {
            StatsHelper.Result arg;
            string confidence = "null";
            string alias = "null";
            if (result != null)
            {
                confidence = result.ProcessingResult.Confidence.ToString();
            }

            if (result == null || result.ProcessingResult.IdentifiedProfileId == Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                arg = StatsHelper.Result.Neither;
            }
            else
            {
                alias = AliasFile.RetrieveAlias(result.ProcessingResult.IdentifiedProfileId);                
                if (alias[1] == 'M')
                {
                    arg = StatsHelper.Result.Adult;
                }
                else if (alias[1] == 'C')
                {
                    arg = StatsHelper.Result.Child;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            recorder.AddResult(arg, confidence, alias);
        }

        /// <summary>
        /// sox doesn't like it if endsecond > audiolen, but will take a copy without an end second
        /// partial copy instruction: sox infile outfile trim startTime duration
        /// Can handle up to an hour of audio, exclusive
        /// </summary>
        /// <param name="inPath"></param>
        /// <param name="outPath"></param>
        /// <param name="startTime">in seconds</param>
        /// <param name="endTime">in seconds</param>
        private void CopyAudioFileSegment(string inPath, string outPath, int startTime, int endTime = -1)
        {
            string time = SecondsToMMColonSS(startTime);
            string duration = "" + (endTime - startTime);
            string args = " \"" + inPath + "\" \"" + outPath + "\" trim " + startTime;
            if (endTime >= 0)
            {
                args += " " + duration;
            }
            RunSox(args);
        }

        private string SecondsToMMColonSS(int seconds)
        {
            return "" + (seconds / 60) + ":" + (seconds % 60);
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

        private void DisplayResults(IdentificationOperation iop)
        {
            if (iop == null)
            {
                _identificationResultTxtBlk.Text = "Unknown";
                _identificationResultAliasTxtBlk.Text = "";
                _identificationConfidenceTxtBlk.Text = "";
                return;
            }
            _identificationResultTxtBlk.Text = iop.ProcessingResult.IdentifiedProfileId.ToString();
            _identificationResultAliasTxtBlk.Text = AliasFile.RetrieveAlias(iop.ProcessingResult.IdentifiedProfileId);
            _identificationConfidenceTxtBlk.Text = iop.ProcessingResult.Confidence.ToString();
        }

        private async void _identifyBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await identify(_selectedFile, (sender as Button) == _identifyShortAudioBtn);
            _selectedFile = "";
            DisplayResults(result);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SpeakersListPage.SpeakersList.SetMultipleSelectionMode();
            SpeakersListPage.SpeakersList.SelectAll();


            CultureInfo useng = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = useng;
            Thread.CurrentThread.CurrentUICulture = useng;
        }
    }
}
