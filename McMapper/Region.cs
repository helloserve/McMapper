using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McMapper
{
    public class Region
    {
        ILog _log = LogManager.GetLogger(typeof(Region));

        DirectoryInfo _dirInfo;
        List<Chunk> _chunks;
        int _minX;
        int _minZ;
        int _maxX;
        int _maxZ;

        public Region(string worldPath)
        {
            if (!Directory.Exists(worldPath))
                throw new FileNotFoundException();

            _dirInfo = new DirectoryInfo(worldPath);
        }

        public Region(DirectoryInfo dirInfo)
        {
            _dirInfo = dirInfo;
        }

        public List<Chunk> Chunks
        {
            get { return _chunks; }
        }

        public string WorldPath
        {
            get { return _dirInfo.FullName; }
        }

        public int MinX
        {
            get { return _minX; }
        }

        public int MinZ
        {
            get { return _minZ; }
        }

        public int MaxX
        {
            get { return _maxX; }
        }

        public int MaxZ
        {
            get { return _maxZ; }
        }

        public void LoadChangedSince(DateTime updatedSince)
        {
            Load(updatedSince: updatedSince);
        }

        public void LoadAll()
        {
            Load();
        }

        private void Load(DateTime? updatedSince = null)
        {
            _chunks = new List<Chunk>();

            byte[] header = new byte[8 * 1024];

            byte[] chunkLocation = new byte[4];
            byte[] chunkTimestamp = new byte[4];
            int chunkPosition;
            DateTime timeStamp;

            DirectoryInfo regionDirInfo = new DirectoryInfo(Path.Combine(_dirInfo.FullName, "region"));
            FileInfo[] regionFiles = regionDirInfo.GetFiles("*.mca");

            int regionX;
            int regionZ;
            int chunkX;
            int chunkZ;
            foreach (FileInfo regionFile in regionFiles)
            {
                try
                {
                    string[] regionSplit = regionFile.Name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    regionX = int.Parse(regionSplit[1]);
                    regionZ = int.Parse(regionSplit[2]);

                    using (FileStream fs = new FileStream(regionFile.FullName, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(header, 0, header.Length);
                    }

                    for (int x = 0; x < 32; x++)
                    {
                        for (int z = 0; z < 32; z++)
                        {
                            chunkX = (regionX * 32) + x;
                            chunkZ = (regionZ * 32) + z;

                            _minX = Math.Min(_minX, chunkX);
                            _minZ = Math.Min(_minZ, chunkZ);
                            _maxX = Math.Max(_maxX, chunkX);
                            _maxZ = Math.Max(_maxZ, chunkZ);

                            chunkPosition = GetChunkPosition(x, z);

                            chunkLocation[0] = header[chunkPosition];
                            chunkLocation[1] = header[chunkPosition + 1];
                            chunkLocation[2] = header[chunkPosition + 2];
                            chunkLocation[3] = header[chunkPosition + 3];

                            if (BitConverter.ToInt32(chunkLocation, 0) != 0)
                            {
                                chunkPosition = GetChunkPosition(x, z, 4096);

                                chunkTimestamp[0] = header[chunkPosition];
                                chunkTimestamp[1] = header[chunkPosition + 1];
                                chunkTimestamp[2] = header[chunkPosition + 2];
                                chunkTimestamp[3] = header[chunkPosition + 3];

                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(chunkTimestamp);

                                timeStamp = new DateTime(1970, 1, 1).AddSeconds(BitConverter.ToInt32(chunkTimestamp, 0));

                                if (updatedSince.HasValue && timeStamp <= updatedSince)
                                    continue;

                                _chunks.Add(new Chunk() { X = chunkX, Z = chunkZ, LastUpdated = timeStamp });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("Error loading chunks for region '{0}'", regionFile.FullName), ex);
                }
            }

            _log.Info(string.Format("Found {0} chunks that changed", _chunks.Count));
        }

        private int GetChunkPosition(int x, int z, int offset = 0)
        {
            int xmod = x % 32;
            int zmod = z % 32;

            if (xmod < 0)
                xmod += 32;
            if (zmod < 0)
                zmod += 32;

            return offset + (4 * (xmod + zmod * 32));
        }
    }
}
