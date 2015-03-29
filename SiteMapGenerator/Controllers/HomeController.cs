using System;
using System.Web.Mvc;
using SiteMapGenerator.SiteMap;

namespace SiteMapGenerator.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Index(string homePageUrl, int depth, bool nofollow)
        {
            var parser = new SiteMapper();
            var start = DateTime.Now;
            var linkCount = parser.GetSiteMap(depth, homePageUrl, nofollow);
            var elapsed = DateTime.Now - start;
            ViewBag.FileName = String.Format("sitemap_{0}.xml", DateTime.Now.ToString("yyyyMMdd_HHmmssfff"));
            var path = Server.MapPath(String.Format("/Content/{0}", ViewBag.FileName));
            ViewBag.Site = homePageUrl;
            ViewBag.LinkCount = linkCount;
            ViewBag.ElapsedMiliseconds = elapsed.TotalMilliseconds;
            parser.MapContainer.Serialize(path);
            return View("Done");
        }

        public ActionResult DownloadFile(string fileName)
        {
            const string contentType = "application/xml";
            var filePath = Server.MapPath(String.Format("/Content/{0}", fileName));
            return File(filePath, contentType, fileName);
        }
    }
}
