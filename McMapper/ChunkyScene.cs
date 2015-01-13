using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McMapper
{
    public class Position
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public static Position Origin
        {
            get
            {
                return new Position()
                {
                    x = 8.0D,
                    y = 67.0D,
                    z = 8.0D
                };
            }
        }
    }

    public class ChunkyScene
    {
        public class GradientPoint
        {
            public string rgb { get; set; }
            public double pos { get; set; }
        }

        public class ChunkPosition
        {
            public int x { get; set; }
            public int y { get; set; }

            public int[] AsArray()
            {
                return new int[] { x, y };
            }
        }

        public class Orientation
        {
            public double roll { get; set; }
            public double pitch { get; set; }
            public double yaw { get; set; }

            public static Orientation TowardsDown
            {
                get
                {
                    return new Orientation()
                    {
                        roll = 0.0D,
                        pitch = 0.0D,
                        yaw = -1.0D * Math.PI * 0.5D
                    };
                }
            }
        }

        public class Color
        {
            public double red;
            public double green;
            public double blue;

            public static Color White
            {
                get
                {
                    return new Color()
                    {
                        red = 1.0D,
                        green = 1.0D,
                        blue = 1.0D
                    };
                }
            }
        }

        public class World
        {
            public string path { get; set; }
            public int dimension { get; set; }
        }

        public class Camera
        {
            public Position position { get; set; }
            public Orientation orientation { get; set; }
            public string projectionMode { get; set; }
            public double fov { get; set; }
            public string dof { get; set; }
            public double focalOffset { get; set; }
        }

        public class CameraPreset
        {

        }

        public class Sun
        {
            public double altitude { get; set; }
            public double azimuth { get; set; }
            public double intensity { get; set; }
            public Color color { get; set; }
        }

        public class Sky
        {
            public double skyYaw { get; set; }
            public bool skyMirrored { get; set; }
            public double skyLight { get; set; }
            public string mode { get; set; }
            public double horizonOffset { get; set; }
            public bool cloudsEnabled { get; set; }
            public double cloudSize { get; set; }
            public Position cloudOffset { get; set; }
            public List<GradientPoint> gradient { get; set; }
        }

        public int sdfVersion { get; set; }
        public string name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public double exposure { get; set; }
        public int postprocess { get; set; }
        public int renderTime { get; set; }
        public int spp { get; set; }
        public int sppTarget { get; set; }
        public int rayDepth { get; set; }
        public bool pathTrace { get; set; }
        public int dumpFrequency { get; set; }
        public bool saveSnapshots { get; set; }
        public bool emittersEnabled { get; set; }
        public double emittersIntensity { get; set; }
        public bool sunEnabled { get; set; }
        public bool stillWater { get; set; }
        public bool clearWater { get; set; }
        public bool biomeColorsEnabled { get; set; }
        public bool atmosphereEnabled { get; set; }
        public bool volumetricFogEnabled { get; set; }
        public int waterHeight { get; set; }
        public World world { get; set; }
        public Camera camera { get; set; }
        public Sun sun { get; set; }
        public Sky sky { get; set; }
        public CameraPreset cameraPresets { get; set; }
        public List<int[]> chunkList { get; set; }

        public static ChunkyScene OriginTopDown
        {
            get
            {
                return new ChunkyScene()
                {
                    sdfVersion = 3,
                    name = "world",
                    width = 256,
                    height = 256,
                    exposure = 1.0D,
                    postprocess = 1,
                    spp = 0,
                    sppTarget = 500,
                    rayDepth = 5,
                    pathTrace = true,
                    dumpFrequency = 501,
                    saveSnapshots = false,
                    emittersEnabled = false,
                    emittersIntensity = 13.0D,
                    sunEnabled = false,
                    stillWater = true,
                    clearWater = true,
                    biomeColorsEnabled = true,
                    atmosphereEnabled = false,
                    volumetricFogEnabled = false,
                    waterHeight = 0,
                    world = new World()
                    {
                        path = Path.Combine("C:\\User", Environment.UserName, "AppData\\Roaming\\.minecraft\\saves\\world"),
                        dimension = 0
                    },
                    camera = new Camera()
                    {
                        position = Position.Origin,
                        orientation = Orientation.TowardsDown,
                        projectionMode = "PARALLEL",
                        fov = 80,
                        dof = "Infinity",
                        focalOffset = 2.0D
                    },
                    sun = new Sun()
                    {
                        altitude = 1.0471975511965976D,
                        azimuth = 1.2566370614359172D,
                        intensity = 1.25D,
                        color = Color.White
                    },
                    sky = new Sky()
                    {
                        skyYaw = 0.0D,
                        skyMirrored = true,
                        skyLight = 5.0D,
                        mode = "SIMULATED",
                        horizonOffset = 0.1D,
                        cloudsEnabled = false,
                        cloudSize = 64.0D,
                        cloudOffset = new Position()
                        {
                            x = 0.0D,
                            y = 128.0D,
                            z = 0.0D
                        },
                        gradient = new List<GradientPoint>()
                        {
                            new GradientPoint() { rgb = "FFFFFF", pos = 0.0D },
                            new GradientPoint() { rgb = "FFFFFF", pos = 1.0D }
                        }
                    },
                    cameraPresets = new CameraPreset(),
                    chunkList = new List<int[]>()
                };
            }
        }
    }
}
