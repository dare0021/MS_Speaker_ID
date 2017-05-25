using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIDIdentificationAPI_WPF_Samples
{
    class StatsHelper
    {
        private string name;
        public bool childIsCorrect = true;
        private int total = 0;
        private int correctChild = 0;
        private int correctAdult = 0;
        private int totalTruthChild = 0;
        private int totalTruthAdult = 0;
        private int totalDetectChild = 0;
        private int totalDetectNeither = 0;
        private int totalDetectAdult = 0;

        public enum Result
        {
            Child, Adult, Neither
        }

        public StatsHelper(string name, bool childIsCorrect)
        {
            this.name = name;
            this.childIsCorrect = childIsCorrect;
        }

        public float AddResult(string filePath, Result result)
        {
            total++;

            Result truth = childIsCorrect ? Result.Child : Result.Adult;

            switch (result)
            {
                case Result.Child:
                    totalDetectChild++;
                    break;
                case Result.Adult:
                    totalDetectAdult++;
                    break;
                case Result.Neither:
                    totalDetectNeither++;
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (truth)
            {
                case Result.Child:
                    totalTruthChild++;
                    break;
                case Result.Adult:
                    totalTruthAdult++;
                    break;
                case Result.Neither:
                //fall through
                default:
                    throw new NotImplementedException();
            }

            if (truth == result)
            {
                if (truth == Result.Child)
                {
                    correctChild += 1;
                }
                else if (truth == Result.Adult)
                {
                    correctAdult += 1;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return GetTotalAccuracy();
        }

        public float GetTotalAccuracy()
        {
            return (float)(correctAdult + correctChild) / total;
        }

        public float GetTotalAccuracyIgnoreNeither()
        {
            return (float)(correctAdult + correctChild) / (total - totalDetectNeither);
        }

        
        public override String ToString()
        {
            String retval = "";
            retval += "childIsCorrect : " + childIsCorrect + "\n";
            retval += "total : " + total + "\n";
            retval += "correctChild : " + correctChild + "\n";
            retval += "correctAdult : " + correctAdult + "\n";
            retval += "totalTruthChild : " + totalTruthChild + "\n";
            retval += "totalTruthAdult : " + totalTruthAdult + "\n";
            retval += "totalDetectChild : " + totalDetectChild + "\n";
            retval += "totalDetectNeither : " + totalDetectNeither + "\n";
            retval += "totalDetectAdult : " + totalDetectAdult + "\n";
            retval += "accuracy : " + GetTotalAccuracy() + "\n";
            retval += "accuracy sans neither : " + GetTotalAccuracyIgnoreNeither() + "\n";
            return retval;
        }

        public string GetName()
        {
            return name;
        }

        public String SaveLog(string path)
        {
            using (StreamWriter fs = new StreamWriter(File.OpenWrite(path)))
            {
                fs.WriteLine(ToString());
            }
            return ToString();
        }
    }
}
