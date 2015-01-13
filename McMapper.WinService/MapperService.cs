using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McMapper.WinService
{
    public partial class McMapperService : ServiceBase
    {
        static ILog _log = LogManager.GetLogger(typeof(McMapperService));
        static List<Mapper> _mappers = new List<Mapper>();

        public McMapperService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _log.Info("Starting McMapper Service");
            ThreadPool.QueueUserWorkItem(StartMapper);
        }

        protected override void OnStop()
        {
            _log.Info("Stopping McMapper Service");
            foreach (Mapper mapper in _mappers)
            {
                mapper.Stop();
            }
        }

        void StartMapper(object state)
        {
            try
            {
                log4net.Config.XmlConfigurator.Configure();

                string worldPath = ConfigurationManager.AppSettings["world"];
                string worldReadPath = ConfigurationManager.AppSettings["worldReadLocation"];
                string chunkyScenePath = ConfigurationManager.AppSettings["chunkyScene"];
                string javaRuntime = ConfigurationManager.AppSettings["javaRuntime"];
                string chunkyRuntime = ConfigurationManager.AppSettings["chunkyRuntime"];

                Mapper mapper = new Mapper(worldPath, chunkyScenePath, javaRuntime, chunkyRuntime, worldReadPath);
                _mappers.Add(mapper);
                mapper.Start();
            }
            catch (Exception ex)
            {
                _log.Error("Error starting McMapper Service", ex);
            }
        }
    }
}
