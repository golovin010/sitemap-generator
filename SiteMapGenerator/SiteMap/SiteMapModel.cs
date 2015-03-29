using System;
using System.Linq;
using System.Xml.Serialization;

namespace SiteMapGenerator.SiteMap
{
    [XmlType("url")]
    public class SiteMapModel
    {
        [XmlElement("loc")]
        public string Location { get; set; }

        [XmlElement("lastmod")]
        public string LastModified {
            get { return _lastModified.ToString("yyyy-MM-ddThh:mm:sszzz"); }
            set{}
        }

        [XmlElement("changefreq")]
        public string ChangeFreq
        {
            get
            {
                switch (_slashCount)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return "hourly";
                    default:
                        return "daily";
                }
            }
            set { }
        }

        [XmlElement("priority")]
        public float Priority
        {
            get
            {
                switch (_slashCount)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return 0.9f;
                    case 5:
                        return (float)0.8f;
                    case 6:
                        return 0.6f;
                    case 7:
                        return 0.4f;
                    default:
                        return 0.5f;
                }
            }
            set { }
        }

        private readonly DateTimeOffset _lastModified;
        private readonly int _slashCount;
        public SiteMapModel()   // default constructor foer serialization
        {
        }

        public SiteMapModel(string location, DateTime lastModified)
        {
            if (location.Length > 0)
            {
                Location = location;
                _lastModified = lastModified;
                _slashCount = Location.Split('/').Count();
            }
        }
    }
}