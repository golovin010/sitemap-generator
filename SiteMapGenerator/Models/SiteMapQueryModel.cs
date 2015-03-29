namespace SiteMapGenerator.Models
{
    public class SiteMapQueryModel
    {
        public string HomePageUrl { get; set; }
        public int Depth { get; set; }
        public bool Nofollow { get; set; }
    }
}