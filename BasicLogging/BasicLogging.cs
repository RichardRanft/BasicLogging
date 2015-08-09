using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BasicLogging
{
    public class CLog
    {
        private String m_logFileName;
        private StreamWriter m_outputStream;

        public String Filename
        {
            get
            {
                return m_logFileName;
            }
            set
            {
                m_logFileName = value;
                initLogfile();
            }
        }

        public CLog ()
        {
            m_logFileName = "";
            m_outputStream = null;
        }

        private void initLogfile()
        {
            if(m_outputStream != null)
            {
                m_outputStream.Flush();
                m_outputStream.Close();
            }
            try
            {
                String directory = "";
                if (m_logFileName.Contains("\\"))
                    directory = m_logFileName.Remove(m_logFileName.LastIndexOf("\\"));
                directory = Path.GetFullPath(directory);
                if (Directory.Exists(Path.GetDirectoryName(directory)))
                    m_outputStream = new StreamWriter(m_logFileName);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                    m_outputStream = new StreamWriter(m_logFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" -- {0} : Error opening logfile {1} : {2}", DateTime.Now.ToString(), m_logFileName, ex.Message);
            }
        }

        public void WriteLine(String line)
        {
            if (m_outputStream == null)
            {
                initLogfile();
            }

            try
            {
                m_outputStream.WriteLineAsync(String.Format("{0} : {1}",DateTime.Now.ToString(), line));
                m_outputStream.Flush();
            }
            catch(Exception ex)
            {
                Console.WriteLine(" -- {0} : Error writing to logfile  {1} : {2}", DateTime.Now.ToString(), m_logFileName, ex.Message);
            }
        }
    }
}
