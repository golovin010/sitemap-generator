using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SiteMapGenerator.SiteMap
{
    public class SiteMapContainer
    {
        [XmlArray("urlset")]
        public List<SiteMapModel> SiteUrls { get; set; }

        public SiteMapContainer()
        {
            SiteUrls = new List<SiteMapModel>();
        }

        public bool Contains(string urlToCheck)
        {
            var result = SiteUrls.Find(t => t.Location == urlToCheck);
            return result != null;
        }

        public void Add(string location, DateTime lastModified)
        {
            SiteUrls.Add(new SiteMapModel(location, lastModified));
        }

        public int Count()
        {
            return SiteUrls.Count;
        }

        public void Serialize(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (List<SiteMapModel>),
                new XmlRootAttribute("urlset") {Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9"});
            using (TextWriter writer = new StreamWriter(filename))
            {
                serializer.Serialize(writer, SiteUrls);
            }
        }
    }
}