using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace McMapper.McService
{
    public partial class McService : ServiceBase
    {
        static ILog _log = LogManager.GetLogger(typeof(McService));
        static bool _restart = true;

        static System.Diagnostics.Process _minecraftProcess;
        static System.Threading.Thread _processThread;
        static System.Threading.Timer _processTimer;

        public McService()
        {
            InitializeComponent();
            log4net.Config.XmlConfigurator.Configure();
            base.Disposed += McService_Disposed;
        }

        void McService_Disposed(object sender, EventArgs e)
        {
            _log.Info("McService Disposed!!");

            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            _log.Info("Starting McService");

            _processThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(_startMinecraft));
            _processThread.Start();
        }

        void _startMinecraft(object state)
        {
            try
            {
                string javaRuntime = ConfigurationManager.AppSettings["javaRuntime"];
                string minecraftRuntime = ConfigurationManager.AppSettings["minecraftRuntime"];

                if (minecraftRuntime == null)
                    throw new ArgumentException("Expected Minecraft runtime setting");

                if (javaRuntime == null)
                    throw new ArgumentException("Expected Java runtime setting");

                FileInfo minecraftFile = new FileInfo(minecraftRuntime);

                LogProcess("");

                _restart = true;

                System.Diagnostics.ProcessStartInfo minecraftProcessInfo = new ProcessStartInfo(javaRuntime, string.Format("-Xmx1024M -Xms1024M -jar \"{0}\" nogui", minecraftRuntime));
                minecraftProcessInfo.WorkingDirectory = minecraftFile.Directory.FullName;
                minecraftProcessInfo.CreateNoWindow = true;
                minecraftProcessInfo.UseShellExecute = false;
                minecraftProcessInfo.RedirectStandardInput = true;
                minecraftProcessInfo.RedirectStandardOutput = true;
                minecraftProcessInfo.RedirectStandardError = true;

                _log.Info(string.Format("Starting process '{0}' with args '{1}'", minecraftProcessInfo.FileName, minecraftProcessInfo.Arguments));

                _minecraftProcess = new System.Diagnostics.Process();
                _minecraftProcess.StartInfo = minecraftProcessInfo;
                _minecraftProcess.Exited += _minecraftProcess_Exited;
                _minecraftProcess.OutputDataReceived += _minecraftProcess_OutputDataReceived;
                _minecraftProcess.ErrorDataReceived += _minecraftProcess_ErrorDataReceived;

                _minecraftProcess.Start();
                _minecraftProcess.BeginOutputReadLine();

                _log.Info(string.Format("Process started, process Id {0}", _minecraftProcess.Handle));

                _processTimer = new System.Threading.Timer(new System.Threading.TimerCallback(_processMonitor), null, 1 * 60 * 1000, 1 * 60 * 1000);
            }
            catch (Exception ex)
            {
                _log.Error("Startup error occured", ex);
                Stop();
            }
        }

        void _stopMinecraft()
        {
            if (!_minecraftProcess.HasExited)
            {
                _log.Info("Stopping process");
                try
                {
                    using (StreamWriter writer = _minecraftProcess.StandardInput)
                    {
                        writer.WriteLine("stop\n");
                    }

                    while (!_minecraftProcess.HasExited)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error stopping service", ex);
                    _log.Info("Killing process");
                    _minecraftProcess.Kill();
                }
            }
        }

        void _minecraftProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogProcess(e.Data);
            _log.Error(e.Data);
        }

        void _minecraftProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogProcess(e.Data);
            _log.Debug(e.Data);
        }

        void _minecraftProcess_Exited(object sender, EventArgs e)
        {
            _log.Info("Process stopped");
            if (_restart)
            {
                _log.Info("Restarting process");
                _minecraftProcess.Start();
            }
        }

        void _processMonitor(object state)
        {
            try
            {
                if (_restart)
                {
                    _log.Info("Process timer - check");

                    try
                    {
                        Process jvm = System.Diagnostics.Process.GetProcessById(_minecraftProcess.Id);
                        if (jvm != null && !jvm.HasExited)
                            return;
                    }
                    catch (ArgumentException) { }

                    Process[] jvms = System.Diagnostics.Process.GetProcessesByName("java");

                    for (int i = 0; i < jvms.Length; i++)
                    {
                        _log.DebugFormat("Checking process '{0}', CPU {1}, Private Set {2}, Peak Set {3}", jvms[i].ProcessName, jvms[i].TotalProcessorTime, jvms[i].PrivateMemorySize64, jvms[i].PeakWorkingSet64);
                        if (jvms[i].PeakWorkingSet64 > 200 * 1024 * 1024)
                            return;
                    }

                    _minecraftProcess_Exited(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error monitoring process", ex);
            }
        }

        void LogProcess(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format("\n{0}", message));
            using (FileStream fs = new FileStream(ConfigurationManager.AppSettings["runtimeLog"] ?? "process.log", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        protected override void OnStop()
        {
            _processTimer.Dispose();
            _restart = false;

            _log.Info("Stopping McService");

            _stopMinecraft();
        }
    }
}
