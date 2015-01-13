using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McMapper.Console
{
    public class Program
    {
        static bool _stopped = false;
        static ILog _log = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            bool generateZoomOnly = false;
            bool hideWindows = true;
            bool forceUpdate = false;
            int? xMin = null;
            int? zMin = null;
            int? xMax = null;
            int? zMax = null;

            if (args.Length > 0)
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToUpper() == "-ZOOMONLY")
                        generateZoomOnly = true;
                    if (args[i].ToUpper() == "-FORCEUPDATE")
                        forceUpdate = true;
                    if (args[i].ToUpper() == "-SHOWWINDOWS")
                        hideWindows = false;
                    if (args[i].ToUpper() == "-XMIN")
                    {
                        i++;
                        xMin = int.Parse(args[i]);
                    }
                    if (args[i].ToUpper() == "-ZMIN")
                    {
                        i++;
                        zMin = int.Parse(args[i]);
                    }
                    if (args[i].ToUpper() == "-XMAX")
                    {
                        i++;
                        xMax = int.Parse(args[i]);
                    }
                    if (args[i].ToUpper() == "-ZMAX")
                    {
                        i++;
                        zMax = int.Parse(args[i]);
                    }
                }


            string worldPath = ConfigurationManager.AppSettings["world"];
            string worldReadPath = ConfigurationManager.AppSettings["worldReadLocation"];
            string chunkyScenePath = ConfigurationManager.AppSettings["chunkyScene"];
            string javaRuntime = ConfigurationManager.AppSettings["javaRuntime"];
            string chunkyRuntime = ConfigurationManager.AppSettings["chunkyRuntime"];

            Mapper mapper = new Mapper(worldPath, chunkyScenePath, javaRuntime, chunkyRuntime, worldReadPath, generateZoomOnly: generateZoomOnly, hideWindows: hideWindows, forceUpdate: forceUpdate, xMin: xMin, zMin: zMin, xMax: xMax, zMax: zMax);
            mapper.Start();

            while (true) { }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _stopped = true;
        }
    }
}
