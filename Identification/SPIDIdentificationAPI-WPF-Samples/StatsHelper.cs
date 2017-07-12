using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIDIdentificationAPI_WPF_Samples
{
    class StatsHelper
    {
        public bool childIsCorrect = true;

        static private List<string> validNoiseClasses = new List<string>() { "Living1", "Living10", "Car1", "Car10" };

        private int total = 0;
        private int correctChild = 0;
        private int correctAdult = 0;
        private int totalTruthChild = 0;
        private int totalTruthAdult = 0;
        private int totalDetectChild = 0;
        private int totalDetectUnknown = 0;
        private int totalDetectNoise = 0;
        private int totalDetectAdult = 0;
        
        private class ResultItem
        {
            public ResultItem(Result result, string confidence, string alias)
            {
                this.result = result;
                this.confidence = confidence;
                this.alias = alias;
            }

            public Result result;
            public string confidence;
            public string alias;

            public override string ToString()
            {
                string retval = "\nConfidence: " + confidence;
                switch (result)
                {
                    case Result.Adult:
                        retval = "Result: Adult" + retval;
                        break;
                    case Result.Child:
                        retval = "Result: Child" + retval;
                        break;
                    case Result.Unknown:
                        retval = "Result: Neither" + retval;
                        break;
                    case Result.Noise:
                        retval = "Result: Noise" + retval;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                retval += "\nAlias: " + alias;
                return retval;
            }
        }

        private ArrayList results = new ArrayList();

        public enum Result
        {
            Child, Adult, Noise, Unknown
        }

        static public bool IsValidNoiseClass(string input)
        {
            return validNoiseClasses.Contains(input);
        }

        public StatsHelper(bool childIsCorrect)
        {
            this.childIsCorrect = childIsCorrect;
        }

        public float AddResult(Result result, string confidence, string alias)
        {
            total++;

            results.Add(new ResultItem(result, confidence, alias));

            Result truth = childIsCorrect ? Result.Child : Result.Adult;

            switch (result)
            {
                case Result.Child:
                    totalDetectChild++;
                    break;
                case Result.Adult:
                    totalDetectAdult++;
                    break;
                case Result.Noise:
                    totalDetectNoise++;
                    break;
                case Result.Unknown:
                    totalDetectUnknown++;
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
                case Result.Noise:
                    //fall through
                case Result.Unknown:
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
            return (float)(correctAdult + correctChild) / (total - totalDetectUnknown - totalDetectNoise);
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
            retval += "totalDetectUnknown : " + totalDetectUnknown + "\n";
            retval += "totalDetectNoise : " + totalDetectNoise + "\n";
            retval += "totalDetectAdult : " + totalDetectAdult + "\n";
            retval += "accuracy : " + GetTotalAccuracy() + "\n";
            retval += "accuracy sans neither : " + GetTotalAccuracyIgnoreNeither() + "\n";
            return retval;
        }

        public String SaveLog(string path, bool verbose = false)
        {
            string retval = ToString();
            if (verbose)
            {
                int iNo = 0;
                foreach (var item in results)
                {
                    retval += "iter " + iNo + "\n" + item + "\n\n";
                    iNo++;
                }
            }
            using (StreamWriter fs = new StreamWriter(File.OpenWrite(path)))
            {
                fs.WriteLine(retval);
            }
            return retval;
        }
    }
}
