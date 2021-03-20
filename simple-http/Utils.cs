using System;
using System.IO;
using System.Collections.Generic;

namespace simple_http
{    
    public class Utils
    {
        public static void SendStatsToBigQuery(List<string> arr, string fileName, string table)
        {
            string os = Environment.GetEnvironmentVariable("OS");

            string fname = fileName;
            if (os == null)
            {
                //Linux
                fname = String.Format("/tmp/{0}", fileName);
            }
            
            using (FileStream fs = File.Open(fname, FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(fs);
                foreach (string line in arr)
                {                 
                    sw.WriteLine(line);
                }
                
                sw.Flush();
                sw.Close();
            }

            string cmd = "bq.cmd";
            if (os == null)
            {
                //Unix
                cmd = "bq";
            }
            string arg = String.Format("load --headless=true --project_id=gcp-dmp-devops --autodetect --source_format=NEWLINE_DELIMITED_JSON {0} {1}", table, fname);
            using(System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
            {
                pProcess.StartInfo.FileName = cmd;
                pProcess.StartInfo.Arguments = arg;
                pProcess.StartInfo.UseShellExecute = true;
                pProcess.StartInfo.RedirectStandardOutput = false;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                pProcess.Start();
                pProcess.WaitForExit();
            }

            Console.WriteLine("Sent file [{0}] to BigQuery table [{1}]", fname, table);
        }

        public static void AuthenToGCP()
        {
            string keyFile = Environment.GetEnvironmentVariable("GCP_KEY_FILE_PATH");
            if (keyFile != null)
            {
                string gcloudArg = String.Format("auth activate-service-account --key-file={0}", keyFile);
                using(System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
                {
                    pProcess.StartInfo.FileName = "gcloud";
                    pProcess.StartInfo.Arguments = gcloudArg;
                    pProcess.StartInfo.UseShellExecute = true;
                    pProcess.StartInfo.RedirectStandardOutput = false;
                    pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                    pProcess.Start();
                    pProcess.WaitForExit();
                }

                Console.WriteLine("Authenticated to GCloud using key file [{0}]", keyFile);
            }
        }
    }
}
