using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace McMapper.Web.Controllers
{
    public class TileController : Base.BaseController
    {
        // GET: Tile
        public ActionResult Index(int zoom, int x, int y)
        {
            zoom = 3 - zoom;

            int tileX = x; //(x / 256) - 20;
            int tileY = y; //(y / 256) - 20;

            string type = ConfigurationManager.AppSettings["tileImageType"];
            string tileName = ConfigurationManager.AppSettings["tileNameFormat"].Replace("{tile}", ConfigurationManager.AppSettings["worldName"]).Replace("{x}", tileX.ToString()).Replace("{y}", tileY.ToString());

            if (zoom != 0)
                tileName = tileName.Replace("{zoom}", string.Format("_{0}", zoom));
            else
                tileName = tileName.Replace("{zoom}", string.Empty);

            tileName = string.Format("{0}.{1}", tileName, type);
            string tilesPath = ConfigurationManager.AppSettings["tileFolder"];
            string tilePath;
            if (zoom == 0)
                tilePath = System.IO.Path.Combine(tilesPath, tileName);
            else
                tilePath = System.IO.Path.Combine(tilesPath, zoom.ToString(), tileName);

            if (!System.IO.File.Exists(tilePath))
            {
                tilePath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "Content", "images", "notile.png");
                type = "png";
            }

            System.IO.FileStream fs = new System.IO.FileStream(tilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            return new FileStreamResult(fs, string.Format("image/{0}", type));
        }
    }
}