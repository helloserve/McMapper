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

namespace McMapper
{
    public class Mapper
    {
        ILog _log = LogManager.GetLogger(typeof(Mapper));

        bool _forceUpdate;
        bool _generateZoomOnly;
        bool _hideWindows;
        int? _xMinOverride;
        int? _zMinOverride;
        int? _xMaxOverride;
        int? _zMaxOverride;

        Region _region;
        SaveData _saveData;
        string _saveDataName;

        string _worldPath;
        string _worldReadPath;
        string _mapName;
        string _chunkyScenePath;
        string _javaRuntime;
        string _chunkyRuntime;
        int _cores;

        string _mapFilePattern;
        string _mapFile;

        bool _stop = false;
        object _processLock = new object();
        object _timerLock = new object();
        System.Threading.Timer _stepTimer;
        DateTime _lastUpdate;

        List<Process> _processes;

        class StepData
        {
            public int X;
            public int Z;
            public ChunkyScene Scene;
        }

        public Mapper(string worldPath, string chunkyScenePath, string javaRuntime, string chunkyRuntime, string worldReadLocation, int? cores = null, bool generateZoomOnly = false, bool hideWindows = true, bool forceUpdate = false, int? xMin = null, int? zMin = null, int? xMax = null, int? zMax = null)
        {
            _cores = cores ?? 1;// Environment.ProcessorCount / 2;

            _worldPath = worldPath; //ConfigurationManager.AppSettings["world"];            
            _chunkyScenePath = chunkyScenePath; //ConfigurationManager.AppSettings["chunkyScene"];
            _javaRuntime = javaRuntime; //ConfigurationManager.AppSettings["javaRuntime"]
            _chunkyRuntime = chunkyRuntime; //ConfigurationManager.AppSettings["chunkyRuntime"];
            _generateZoomOnly = generateZoomOnly;
            _hideWindows = hideWindows;
            _forceUpdate = forceUpdate;
            _xMinOverride = xMin;
            _zMinOverride = zMin;
            _xMaxOverride = xMax;
            _zMaxOverride = zMax;

            _mapName = _worldPath.Substring(_worldPath.LastIndexOf("\\") + 1, _worldPath.Length - _worldPath.LastIndexOf("\\") - 1);
            _mapFilePattern = "{0}_{1}_{2}";

            _worldReadPath = Path.Combine(worldReadLocation, _mapName);
            if (!Directory.Exists(_worldReadPath))
                Directory.CreateDirectory(_worldReadPath);

            _saveData = new SaveData() { WorldLocation = _mapName, LastPolledDate = DateTime.Today.AddDays(-10) };
            _saveDataName = string.Format("{0}.json", _mapName);
            string savePath = Path.Combine(_worldReadPath, _saveDataName);
            if (File.Exists(savePath))
                _saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(savePath));

            _log.Info(string.Format("Starting for '{0}', last polled at {1}", _mapName, _saveData.LastPolledDate));

            _processes = new List<Process>();
        }

        public void Start()
        {
            Update();

            _stop = false;

            _lastUpdate = DateTime.MinValue;
            Process();
        }

        void Update_Timer(object state)
        {
            Update();
        }

        private void Update()
        {
            try
            {
                _log.Info(string.Format("Copying for '{0}', from '{1}' to '{2}'", _mapName, _worldPath, _worldReadPath));
                ProcessStartInfo copyProcessInfo = new ProcessStartInfo("xcopy", string.Format("/Y /S \"{0}\" \"{1}\"", Path.Combine(_worldPath, "*.*"), Path.Combine(_worldReadPath, "*.*")));
                copyProcessInfo.UseShellExecute = false;
                copyProcessInfo.CreateNoWindow = true;
                copyProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Process copyProcess = System.Diagnostics.Process.Start(copyProcessInfo);

                while (!copyProcess.HasExited)
                    Thread.Sleep(5);

                copyProcess.Dispose();
            }
            catch (Exception ex)
            {
                _log.Error("Error running copy process", ex);
                return;   //continue since this tile's not updated, and we can't do anything else
            }

            if (_region == null)
                _region = new Region(_worldReadPath);

            if (_forceUpdate)
            {
                _log.Info("Updating everything (force)");
                _region.LoadAll();
            }
            else
            {
                _log.Info(string.Format("Updating for '{0}', last polled at {1}", _mapName, _saveData.LastPolledDate));
                _region.LoadChangedSince(_saveData.LastPolledDate);
            }

            lock (_processLock)
            {
                _saveData.MinX = _region.MinX;
                _saveData.MinZ = _region.MinZ;
                _saveData.MaxX = _region.MaxX;
                _saveData.MaxZ = _region.MaxZ;

                int tileX;
                int tileZ;
                int count = 0;
                foreach (Chunk chunk in _region.Chunks)
                {
                    tileX = chunk.X / 5;
                    tileZ = chunk.Z / 5;

                    if (tileX == _saveData.CurrentX && tileZ == _saveData.CurrentZ)
                        continue;

                    if (!_saveData.Tiles.ContainsKey(tileX))
                        _saveData.Tiles.Add(tileX, new Dictionary<int, Tile>());
                    if (!_saveData.Tiles[tileX].ContainsKey(tileZ))
                        _saveData.Tiles[tileX].Add(tileZ, new Tile(tileX, tileZ));

                    _saveData.Tiles[tileX][tileZ].AddChunk(chunk);
                    count++;
                }

                _log.Info(string.Format("Added {0} chunks to changed list", count));
            }
        }

        public void Stop()
        {
            lock (_processLock)
            {
                _stop = true;
                _log.Info("Mapper Stopped");
            }

            _stepTimer_Callback(null);
        }

        void Process()
        {
            Tile tile;

            if (_xMinOverride.HasValue)
            {
                if (_saveData.CurrentX < _xMinOverride)
                    _saveData.CurrentX = _xMinOverride.Value;
            }

            if (_zMinOverride.HasValue)
            {
                if (_saveData.CurrentZ < _zMinOverride)
                    _saveData.CurrentZ = _zMinOverride.Value;
            }

            if (_xMaxOverride.HasValue || _xMinOverride.HasValue)
            {
                if (_saveData.CurrentX > (_xMaxOverride ?? _xMinOverride))
                    _saveData.CurrentX = (_xMaxOverride ?? _xMinOverride ?? _saveData.CurrentX);
            }

            if (_zMaxOverride.HasValue || _zMinOverride.HasValue)
            {
                if (_saveData.CurrentZ > (_zMaxOverride ?? _zMinOverride))
                    _saveData.CurrentZ = (_zMaxOverride ?? _zMinOverride ?? _saveData.CurrentZ);
            }

            tile = null;
            lock (_processLock)
            {
                if (_saveData.Tiles.ContainsKey(_saveData.CurrentX))
                    if (_saveData.Tiles[_saveData.CurrentX].ContainsKey(_saveData.CurrentZ))
                        tile = _saveData.Tiles[_saveData.CurrentX][_saveData.CurrentZ];
            }

            if (tile != null)
            {
                StepStart(_saveData.CurrentX, _saveData.CurrentZ);
                return;
            }

            ProcessComplete();
        }

        void ProcessComplete()
        {
            if (!_forceUpdate && (DateTime.Now - _lastUpdate).TotalMinutes > 10)
            {
                Update();
                _lastUpdate = DateTime.Now;
            }

            lock (_processLock)
            {
                if (_saveData.Tiles.ContainsKey(_saveData.CurrentX))
                {
                    if (_saveData.Tiles[_saveData.CurrentX].ContainsKey(_saveData.CurrentZ))
                        _saveData.Tiles[_saveData.CurrentX].Remove(_saveData.CurrentZ);

                    if (_saveData.Tiles[_saveData.CurrentX].Count == 0)
                        _saveData.Tiles.Remove(_saveData.CurrentX);

                    if (_forceUpdate && _saveData.Tiles.Count == 0)
                        Update();
                }

                if (_saveData.CurrentZ < (_zMaxOverride ?? _zMinOverride ?? _saveData.MaxZ))
                    _saveData.CurrentZ++;
                else
                {
                    _saveData.CurrentZ = (_zMinOverride ?? _saveData.MinZ);
                    _log.Debug(string.Format("Reached the end of Z ({0}), setting to ({1}, {2})", _saveData.MaxZ, _saveData.CurrentX, _saveData.MinZ));
                    if (_saveData.CurrentX < (_xMaxOverride ?? _xMinOverride ?? _saveData.MaxX))
                        _saveData.CurrentX++;
                    else
                    {
                        _saveData.CurrentX = (_xMinOverride ?? _saveData.MinX);
                        _log.Debug(string.Format("Reached the end of X ({0}), setting to ({1},{2})", _saveData.MaxX, _saveData.MinX, _saveData.CurrentZ));
                    }
                }
            }

            try
            {
                string savePath = Path.Combine(_worldReadPath, _saveDataName);
                _saveData.LastPolledDate = DateTime.Now;
                _saveData.Save(savePath);
            }
            catch (Exception ex)
            {
                _log.Error("Error saving data", ex);
            }

            if (!_stop)
                Process();
        }

        void StepStart(int x, int z)
        {
            _log.Info(string.Format("Preparing tile {0},{1}", x, z));

            _mapFile = string.Format(_mapFilePattern, _mapName, x, z).Replace(" ", "_");

            //prepare the scene
            ChunkyScene scene = ChunkyScene.OriginTopDown;
            scene.name = _mapFile;
            scene.world.path = _worldPath;
            scene.camera.position.x = 8 + (x * 16 * 5);
            scene.camera.position.z = 8 + (z * 16 * 5);

            //add the chunks to load as a function of x,y
            scene.chunkList = GetChunks(x, z, 5);

            StepChunky(x, z, scene);
        }

        void StepChunky(int x, int z, ChunkyScene scene)
        {
            if (_processes.Count > 0)
            {
                _log.Debug(string.Format("{0} processes already running: {1} ", _processes.Count, string.Join(",", _processes.Select(p => p.Id.ToString()).ToArray())));
                //Try and sort out the process
                _stepTimer_Callback(new StepData() { X = x, Z = z, Scene = scene });
                //And just try it again - no counters have been progressed
                Process();
            }

            string[] sceneFiles = null;
            if (!_generateZoomOnly)
            {
                //clean the file structure
                if (!Directory.Exists(_chunkyScenePath))
                    Directory.CreateDirectory(_chunkyScenePath);
                sceneFiles = Directory.GetFiles(_chunkyScenePath, string.Format("{0}*.*", _mapFile));
                foreach (string sceneFile in sceneFiles)
                {
                    if (sceneFile.EndsWith("png"))
                        continue;
                    File.Delete(sceneFile);
                }

                //serialize and save the file
                string sceneFileText = JsonConvert.SerializeObject(scene, Formatting.Indented);
                File.WriteAllText(Path.Combine(_chunkyScenePath, _mapFile + ".json"), sceneFileText);

                _log.Info(string.Format("Rendering tile {0},{1}", x, z));
                ProcessStartInfo chunkyProcessInfo = new ProcessStartInfo(_javaRuntime, string.Format("-jar \"{0}\" -scene-dir \"{1}\" -threads {2} -render \"{3}\"", _chunkyRuntime, _chunkyScenePath, _cores, _mapFile));
                if (_hideWindows)
                {
                    chunkyProcessInfo.CreateNoWindow = true;
                    chunkyProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    chunkyProcessInfo.RedirectStandardOutput = true;
                    chunkyProcessInfo.UseShellExecute = false;
                }

                _log.Debug(string.Format("Running Chunky with '{0} {1}'", chunkyProcessInfo.FileName, chunkyProcessInfo.Arguments));
                Process chunkyProcess = new System.Diagnostics.Process();
                chunkyProcess.StartInfo = chunkyProcessInfo;
                chunkyProcess.OutputDataReceived += chunkyProcess_OutputDataReceived;
                chunkyProcess.Exited += chunkyProcess_Exited;
                _processes.Add(chunkyProcess);

                _stepTimer = new Timer(new System.Threading.TimerCallback(_stepTimer_Callback), new StepData() { X = x, Z = z, Scene = scene }, 60000, 60000);

                try
                {
                    chunkyProcess.Start();
                    chunkyProcess.BeginOutputReadLine();

                    _log.Debug(string.Format("Chunky started with PID {0}", chunkyProcess.Id));
                }
                catch (Exception ex)
                {
                    _log.Error("Error running chunky process", ex);
                    return;   //continue since this tile's not updated, and we can't do anything else
                }
            }
        }

        void chunkyProcess_Exited(object sender, EventArgs e)
        {
            Process jvm = sender as Process;

            if (jvm == null)
                return;

            _processes.Remove(jvm);
        }

        void chunkyProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _log.Info(e.Data);
        }

        void _stepTimer_Callback(object state)
        {
            StepData data = state as StepData;

            if (data == null)
                ProcessComplete();

            lock (_timerLock)
            {
                _log.Debug("Monitoring Chunky process");

                Process jvm;
                for (int i = 0; i < _processes.Count; i++)
                {
                    //find the chunky process
                    try
                    {
                        jvm = System.Diagnostics.Process.GetProcessById(_processes[i].Id);

                        if ((DateTime.Now - jvm.StartTime).TotalHours > 1.5)
                        {
                            TimeSpan span = jvm.TotalProcessorTime;

                            Thread.Sleep(1000);

                            TimeSpan diff = jvm.TotalProcessorTime - span;

                            if (diff.TotalMilliseconds == 0)
                            {
                                jvm.Kill();
                                _processes.RemoveAt(i);
                                i--;
                            }
                            else
                                return;
                        }

                        if (jvm != null && !jvm.HasExited)
                            return;

                        _processes.RemoveAt(i);
                        i--;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("Error Monitoring process {0}", _processes[i].Id), ex);
                        
                        if (ex is InvalidOperationException)
                            _processes[i].Kill();

                        _processes.RemoveAt(i);
                        i--;
                    }
                }

                _stepTimer.Dispose();
            }

            StepComplete(data.X, data.Z, data.Scene);
        }

        void StepComplete(int x, int z, ChunkyScene scene)
        {
            //cleanup
            string[] sceneFiles = Directory.GetFiles(_chunkyScenePath, string.Format("{0}*.*", _mapFile));
            foreach (string sceneFile in sceneFiles)
            {
                if (sceneFile.EndsWith("png"))
                    continue;
                File.Delete(sceneFile);
            }

            GenerateZoomTile(1, ZoomTileCoordinate(x), ZoomTileCoordinate(z), scene.sppTarget, _mapName, _chunkyScenePath);

            ProcessComplete();
        }

        List<int[]> GetChunks(int x, int z, int sceneRadius)
        {
            if (sceneRadius % 2 == 0)
                throw new ArgumentException(string.Format("Could not find a centre chunk on radius {0}", sceneRadius));

            int cx = x * sceneRadius;
            int cy = z * sceneRadius;

            var list = new List<int[]>();

            int chunkRadius = (int)Math.Truncate((double)sceneRadius * 0.5D);
            ChunkyScene.ChunkPosition position;
            for (int i = -chunkRadius; i <= chunkRadius; i++)
            {
                for (int j = -chunkRadius; j <= chunkRadius; j++)
                {
                    position = new ChunkyScene.ChunkPosition() { x = cx + i, y = cy + j };
                    list.Add(position.AsArray());
                }
            }

            return list;
        }

        void GenerateZoomTile(int level, int x, int z, int spp, string mapName, string tilePath)
        {
            _log.Info(string.Format("Generating zoom level {0} for ({1},{2})", level, x, z));

            if (level > 2)
                return;

            using (Bitmap zoomMap = new Bitmap(256, 256))
            {
                string filename;
                string sourcePath = (level == 1) ? tilePath : Path.Combine(tilePath, (level - 1).ToString());
                if (!Directory.Exists(sourcePath))
                    Directory.CreateDirectory(sourcePath);
                string destinationPath = Path.Combine(tilePath, level.ToString());
                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);
                for (int i = (x * 2); i <= (x * 2) + 1; i++)
                {
                    for (int j = (z * 2); j <= (z * 2) + 1; j++)
                    {
                        if (level == 1)
                            filename = string.Format("{0}_{1}_{2}-{3}.png", mapName, i, j, spp);
                        else
                            filename = string.Format("{0}_{1}_{2}_{3}-{4}.png", mapName, level - 1, i, j, spp);

                        string filePath = Path.Combine(sourcePath, filename);
                        if (File.Exists(filePath))
                        {
                            using (Bitmap bitmap = Bitmap.FromFile(filePath) as Bitmap)
                            {
                                using (Graphics gfx = Graphics.FromImage(zoomMap))
                                {
                                    int gfxX = i - (x * 2);
                                    int gfxY = j - (z * 2);
                                    float halve = 256F / 2.0F;
                                    gfx.DrawImage(bitmap, new RectangleF(gfxX * halve, gfxY * halve, halve, halve));
                                }
                            }
                        }
                    }
                }

                zoomMap.Save(Path.Combine(destinationPath, string.Format("{0}_{1}_{2}_{3}-{4}.png", mapName, level, x, z, spp)));
            }

            GenerateZoomTile(level + 1, ZoomTileCoordinate(x), ZoomTileCoordinate(z), spp, mapName, tilePath);
        }

        public int ZoomTileCoordinate(int value)
        {
            double coordinate = (double)value * 0.5D;
            double abs = Math.Abs(coordinate);
            if (value < 0)
                return (int)Math.Round(abs, MidpointRounding.AwayFromZero) * (int)Math.Round(coordinate / abs);
            return (int)Math.Round(abs) * (int)Math.Round(coordinate / abs);
        }
    }
}
