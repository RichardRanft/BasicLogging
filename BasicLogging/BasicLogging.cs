using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace BasicLogging
{
    public class CLog
    {
        private String m_logFileName;
        private String m_baseName;
        private String m_path;
        private StreamWriter m_outputStream;
        private bool m_append = true;
        private bool m_rotateLogs = false;
        private RotateInterval m_interval = RotateInterval.DAILY;
        private DateTime m_logTrigger;
        private List<String> m_msgQueue;

        public String Filename
        {
            get
            {
                return m_logFileName;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;
                else
                    setLogName(value);
            }
        }

        public bool RotateLog
        {
            set 
            {
                m_rotateLogs = value;
                fixFilename();
            }
            get { return m_rotateLogs; }
        }

        public bool Append
        {
            set { m_append = value; }
            get { return m_append; }
        }

        public RotateInterval RotateInterval
        {
            set
            {
                m_interval = value;
                fixFilename();
            }
            get { return m_interval; }
        }

        private CLog (){}

        public CLog(String filename)
        {
            m_rotateLogs = false;
            m_interval = RotateInterval.DAILY; 
            setLogName(filename);
            m_outputStream = null;
            m_msgQueue = new List<String>();
        }

        private void setLogName(String filename)
        {
            String path = ".\\";
            if (!filename.Contains(".\\") && !filename.Contains(":\\"))
                path = ".\\";
            if (filename.Contains("\\"))
            {
                int lastBackslash = filename.LastIndexOf("\\");
                if(lastBackslash > 0)
                    path = filename.Remove(lastBackslash);
            }
            String baseName = Path.GetFileName(filename);
            String logfilename = path + "\\" + baseName;

            if (String.IsNullOrEmpty(m_path) || m_path != path)
                m_path = path;
            if (m_baseName == null || m_baseName != baseName)
                m_baseName = baseName;
            if (m_logFileName == null || m_logFileName != logfilename)
                m_logFileName = logfilename;
            
            if (m_rotateLogs)
            {
                fixFilename();
            }
        }

        private void fixFilename()
        {
            if (m_rotateLogs)
            {
                switch (m_interval)
                {
                    case RotateInterval.HOURLY:
                        {
                            m_logTrigger = DateTime.Now.AddMinutes(-DateTime.Now.Minute);
                            m_logTrigger = m_logTrigger.AddSeconds(-m_logTrigger.Second);
                            String hour = (m_logTrigger.ToShortTimeString().Contains("PM") ? (m_logTrigger.Hour + 12).ToString() : m_logTrigger.Hour.ToString());
                            String time = m_logTrigger.Year + "_" + m_logTrigger.Month.ToString().PadLeft(2, '0') + "_" + m_logTrigger.Day.ToString().PadLeft(2, '0') + "_" + m_logTrigger.Hour.ToString().PadLeft(2, '0');
                            m_logFileName = m_path + "\\" + time + "_" + m_baseName;
                            m_logTrigger = m_logTrigger.AddHours(1.0);
                            initLogfile();
                            WriteLine("Scheduled Log Rollover is at " + m_logTrigger.ToString());
                        }
                        break;
                    case RotateInterval.DAILY:
                        {
                            DateTime now = DateTime.Now;
                            DateTime tomorrow = DateTime.Today.AddDays(1);
                            m_logTrigger = now;
                            String time = m_logTrigger.Year + "_" + m_logTrigger.Month.ToString().PadLeft(2, '0') + "_" + m_logTrigger.Day.ToString().PadLeft(2, '0');
                            m_logFileName = m_path + "\\" + time + "_" + m_baseName;
                            m_logTrigger = m_logTrigger.Add(tomorrow - now);
                            initLogfile();
                            WriteLine("Scheduled Log Rollover is at " + m_logTrigger.ToString());
                        }
                        break;
                    case RotateInterval.WEEKLY:
                        {
                            m_logTrigger = DateTime.Today;
                            String time = m_logTrigger.Year + "_" + m_logTrigger.Month.ToString().PadLeft(2, '0') + "_" + ((m_logTrigger.Day / 7) + 1).ToString().PadLeft(2, '0');
                            m_logFileName = m_path + "\\" + time + "_" + m_baseName;
                            m_logTrigger = m_logTrigger.AddDays(7.0);
                            initLogfile();
                            WriteLine("Scheduled Log Rollover is at " + m_logTrigger.ToString());
                        }
                        break;
                    default:
                        m_logFileName = m_path + "\\" + m_baseName;
                        break;
                }
            }
            else
                m_logFileName = m_path + "\\" + m_baseName;
        }

        private void initLogfile()
        {
            if(m_outputStream != null)
            {
                if (m_outputStream.BaseStream.Position < m_outputStream.BaseStream.Length)
                    m_outputStream.Flush();
                m_outputStream.Close();
            }
            bool success = false;
            DateTime now = DateTime.Now;
            String msg = "";
            int failCount = 0;
            while (!success && failCount < 30)
            {
                try
                {
                    String directory = Path.GetFullPath(m_path);
                    if (!Directory.Exists(Path.GetDirectoryName(directory)))
                        Directory.CreateDirectory(Path.GetDirectoryName(directory));

                    m_outputStream = new StreamWriter(m_logFileName, m_append);
                    success = true;
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    Thread.Sleep(250);
                }
            }
            if (!success)
            {
                Console.WriteLine(" -- {0} : Error opening logfile {1} : {2}", now.ToString(), m_logFileName, msg);
                Environment.Exit(1);
            }
        }

        public void WriteLine(String line)
        {
            if (m_msgQueue == null)
                m_msgQueue = new List<String>();
            m_msgQueue.Add(line);
            if (m_rotateLogs)
            {
                DateTime now = DateTime.Now;
                if(now > m_logTrigger)
                {
                    m_outputStream.Dispose();
                    m_outputStream = null;
                    fixFilename();
                }
            }
            if (m_outputStream == null)
            {
                initLogfile();
            }

            if (m_msgQueue.Count > 0)
            {
                try
                {
                    while (m_msgQueue.Count > 0)
                    {
                        m_outputStream.WriteLineAsync(String.Format("{0} : {1}", DateTime.Now.ToString(), m_msgQueue[0]));
                        m_outputStream.Flush();
                        m_msgQueue.RemoveAt(0);
                        Thread.Sleep(100);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(" -- {0} : Error writing to logfile  {1} : {2}", DateTime.Now.ToString(), m_logFileName, ex.Message);
                    retryWrite();
                }
            }
        }

        private void retryWrite()
        {
            try
            {
                while (m_msgQueue.Count > 0)
                {
                    Thread.Sleep(100);
                    m_outputStream.WriteLineAsync(String.Format("{0} : {1}", DateTime.Now.ToString(), m_msgQueue[0]));
                    m_outputStream.Flush();
                    m_msgQueue.RemoveAt(0);
                }
            }
            catch
            {
                retryWrite();
            }
        }
    }

    public enum RotateInterval
    {
        HOURLY,
        DAILY,
        WEEKLY
    }
}
