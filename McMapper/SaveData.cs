using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McMapper
{
    class SaveData
    {
        public Dictionary<int, Dictionary<int, Tile>> Tiles { get; set; }
        public int MinX;
        public int MinZ;
        public int MaxX;
        public int MaxZ;

        public int CurrentX;
        public int CurrentZ;

        public string WorldLocation { get; set; }
        public DateTime LastPolledDate { get; set; }

        public SaveData()
        {
            Tiles = new Dictionary<int, Dictionary<int, Tile>>();
        }

        public void Save(string filename)
        {
            string txt = JsonConvert.SerializeObject(this);
            File.WriteAllText(filename, txt);
        }
    }
}
