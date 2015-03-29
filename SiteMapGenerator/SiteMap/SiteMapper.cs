using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SiteMapGenerator.SiteMap
{
    public class SiteMapper
    {
        private int MaxDepth { get; set; }
        private bool Nofollow { get; set; }
        private int CurrentDepth { get; set; }
        private Uri _baseUri;
        private Uri _currentUri;
        public SiteMapContainer MapContainer = new SiteMapContainer();

        public int GetSiteMap(int depth, string baseUrl, bool nofollow)
        {
            MaxDepth = depth;
            Nofollow = nofollow;
            try
            {
                _baseUri = new Uri(baseUrl.StartsWith("http")?baseUrl:"http://"+baseUrl);
                ProcessLinks(_baseUri.AbsoluteUri);
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
            return MapContainer.Count();
        }
        private string GetAbsoluteUrl(string url)
        {
            if (url.StartsWith("//"))
            {
                return url.Insert(0, "http:");  //add protocol
            }
            if (url.StartsWith("/"))
            {
                return url.Insert(0, "http://" + _baseUri.Host);  //add protocol and base hostname
            }
            if (url.StartsWith("./"))
            {
                return url.Remove(0, 1).Insert(0, "http://" + _baseUri.Host);   //remove dot and add protocol and base hostname
            }
            if (url.StartsWith("../"))
            {
                var currentUrl = _currentUri.AbsoluteUri;
                var index = currentUrl.LastIndexOf("/", StringComparison.Ordinal);
                if (index > 0)
                {
                    return url.Remove(index + 1, url.Length - index).Insert(0, "http://" + _baseUri.Host);
                }
                return url.Remove(0, 1).Insert(0, "http://" + _baseUri.Host);   //remove dot and add protocol and base hostname
            }
            if (url.StartsWith("http"))
            {
                return url;
            }
            if (url.Contains("javascript:") || url.StartsWith("#"))
            {
                return _currentUri.AbsoluteUri;
            }

            return _currentUri.AbsoluteUri + "/" + url;
        }

        private void ProcessLinks(string url)
        {
            if (CurrentDepth >= MaxDepth) return;
            CurrentDepth++;
            _currentUri = new Uri(url);
            var pageContext = GetPageContext(_currentUri.AbsoluteUri);
            if (pageContext.Content.Length > 0)
            {
                var links = GetAllLinksFromHtmlContent(pageContext.Content);
                foreach (var link in links)
                {
                    var nextUrl = GetAbsoluteUrl(link);
                    try
                    {
                        var uri = new Uri(nextUrl);
                        if (uri.Host == _baseUri.Host)
                        {
                            if (!MapContainer.Contains(uri.AbsoluteUri))
                            {
                                MapContainer.Add(uri.AbsoluteUri, pageContext.LastModified);
                                ProcessLinks(uri.AbsoluteUri);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // cant't parse url for some reason. Silently go to the next one
                    }
                }
            }
        }

        private async Task ProcessLinksAsync(string url)
        {
            if (url.Length <= 0 || CurrentDepth > MaxDepth || !url.StartsWith("http")) return;
            CurrentDepth++;
            var pageContext = await GetPageContextAsync(url);
            if (pageContext.Content.Length > 0)
            {
                foreach (var link in GetAllLinksFromHtmlContent(pageContext.Content))
                {
                    try
                    {
                        var nextUri = new Uri(link);
                        var scheme = nextUri.Scheme == "file" ? "http" : nextUri.Scheme;
                        var nextUrl = scheme + "://" + nextUri.Host + nextUri.AbsolutePath;
                        if (nextUri.Host.Equals(_baseUri.Host))
                        {
                            if (!MapContainer.Contains(nextUrl))
                            {
                                MapContainer.Add(nextUrl, pageContext.LastModified);
                                await ProcessLinksAsync(nextUrl);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Can't parse URI. Silently go to next one
                    }
                }
            }
        }

        private PageContext GetPageContext(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            CookieContainer cc = new CookieContainer();
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.Expect100Continue = false;
            request.Proxy = null;
            request.CookieContainer = cc;
            var pageContext = new PageContext();
            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    pageContext.LastModified = response.LastModified;
                    var dataStream = response.GetResponseStream();
                    if (dataStream != null)
                    {
                        var reader = new StreamReader(dataStream);
                        pageContext.Content = reader.ReadToEnd();
                        reader.Close();
                    }
                }
                response.Close();
                return pageContext;
            }
            catch (SocketException)
            {
                return pageContext;
            }
        }

        private async Task<PageContext> GetPageContextAsync(string url)
        {
            var pageContext = new PageContext();
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                if (response.IsSuccessStatusCode)
                {
                    pageContext.Content = await content.ReadAsStringAsync();
                    pageContext.NullLastModified = content.Headers.LastModified;
                }
                return pageContext;
            }
        }

        private IEnumerable<string> GetAllLinksFromHtmlContent(string htmlContent)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlContent);
            var linksList = new List<String>();
            foreach (var link in html.DocumentNode.SelectNodes("//a[@href]"))
            {
                if (Nofollow)
                {
                    var nofollowAttr = link.Attributes["rel"];
                    if (nofollowAttr != null)
                    {
                        if (nofollowAttr.Value != "nofollow")
                        {
                            linksList.Add(link.Attributes["href"].Value);
                        }
                    }
                    else
                    {
                        linksList.Add(link.Attributes["href"].Value);
                    }
                }
                else
                {
                    linksList.Add(link.Attributes["href"].Value);
                }
            }
            return linksList;
        }

        private struct PageContext
        {
            public string Content { get; set; }
            public DateTimeOffset? NullLastModified { get; set; }
            public DateTime LastModified { get; set; }
        }
    }
}