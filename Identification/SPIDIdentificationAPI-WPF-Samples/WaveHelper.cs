using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIDIdentificationAPI_WPF_Samples
{
    static class WaveHelper
    {
        static string loadedFilePath = "";
        static byte[] loadedFileHeader = new byte[44];

        public static byte[] GenerateHeader(int totalAudioLen, int samplingRate)
        {
            byte[] header = new byte[44];
            int totalDataLen = totalAudioLen + 36;
            int byteRate = samplingRate * 16 / 8;

            header[0] = (byte)'R';  // RIFF/WAVE header
            header[1] = (byte)'I';
            header[2] = (byte)'F';
            header[3] = (byte)'F';
            header[4] = (byte)(totalDataLen & 0xff);
            header[5] = (byte)((totalDataLen >> 8) & 0xff);
            header[6] = (byte)((totalDataLen >> 16) & 0xff);
            header[7] = (byte)((totalDataLen >> 24) & 0xff);
            header[8] = (byte)'W';
            header[9] = (byte)'A';
            header[10] = (byte)'V';
            header[11] = (byte)'E';
            header[12] = (byte)'f';  // 'fmt ' chunk
            header[13] = (byte)'m';
            header[14] = (byte)'t';
            header[15] = (byte)' ';
            header[16] = 16;  // 4 bytes: size of 'fmt ' chunk
            header[17] = 0;
            header[18] = 0;
            header[19] = 0;
            header[20] = 1;  // format = 1
            header[21] = 0;
            header[22] = (byte)1; // # of channels
            header[23] = 0;
            header[24] = (byte)(samplingRate & 0xff);
            header[25] = (byte)((samplingRate >> 8) & 0xff);
            header[26] = (byte)((samplingRate >> 16) & 0xff);
            header[27] = (byte)((samplingRate >> 24) & 0xff);
            header[28] = (byte)(byteRate & 0xff);
            header[29] = (byte)((byteRate >> 8) & 0xff);
            header[30] = (byte)((byteRate >> 16) & 0xff);
            header[31] = (byte)((byteRate >> 24) & 0xff);
            header[32] = (byte)2;  // block align = NumChannels * BitsPerSample / 8
            header[33] = 0;
            header[34] = 16;  // bits per sample
            header[35] = 0;
            header[36] = (byte)'d';
            header[37] = (byte)'a';
            header[38] = (byte)'t';
            header[39] = (byte)'a';
            header[40] = (byte)(totalAudioLen & 0xff);
            header[41] = (byte)((totalAudioLen >> 8) & 0xff);
            header[42] = (byte)((totalAudioLen >> 16) & 0xff);
            header[43] = (byte)((totalAudioLen >> 24) & 0xff);
            return header;
        }

        /// <summary>
        /// What if the file already exists?
        /// Why would you do that
        /// </summary>
        /// <param name="path"></param>
        /// <param name="audio"></param>
        public static void SaveFile(string path, byte[] audio, int samplingRate)
        {
            int totalAudioLen = audio.Length / (samplingRate * 16 / 8);
            using (Stream fs = File.OpenWrite(path))
            {
                fs.Write(GenerateHeader(totalAudioLen, samplingRate), 0, 44);
                fs.Write(audio, 44, audio.Length);
            }
        }

        /// <summary>
        /// Loads a file for further use
        /// </summary>
        /// <param name="path"></param>
        public static void LoadFile(string path)
        {
            loadedFilePath = path;
            using (Stream fs = File.OpenRead(path))
            {
                for (int i=0; i<44; i++)
                {
                    loadedFileHeader[i] = (byte) fs.ReadByte();
                }
            }
        }

        public static void UnloadFile()
        {
            loadedFilePath = "";
            loadedFileHeader = new byte[44];
        }

        private static int BytesToInt(int startIndex)
        {
            int retval = loadedFileHeader[startIndex];
            retval += loadedFileHeader[++startIndex] << 8;
            retval += loadedFileHeader[++startIndex] << 16;
            retval += loadedFileHeader[++startIndex] << 24;
            return retval;
        }

        public static int GetAudioByteLength()
        {
            return BytesToInt(4);
        }

        public static int GetBitDepth()
        {
            return (int)loadedFileHeader[34];
        }

        public static int GetSamplingRate()
        {
            return BytesToInt(24);
        }

        public static int GetByteRate()
        {
            return BytesToInt(28);
        }

        public static float GetAudioLength()
        {
            return (float)GetAudioByteLength() / GetByteRate();
        }
    }
}
