using GoogleMapLinkChecker.ViewModels;
using HtmlAgilityPack;
using System;

namespace GoogleMapLinkChecker.Services
{
    public interface ILinkChecker
    {
        bool IsUrlValidAsync(string link);
        Task<bool> UrlExistAsync(string link);
        Task<PageInfo?> GetUrlContentAsync(string link);

        bool IsGoogleMapsLink(string link);
    }
    public class LinkChecker : ILinkChecker
    {
        private readonly HttpClient _httpClient;
        public LinkChecker(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public bool IsUrlValidAsync(string link)
        {
            return Uri.TryCreate(link, UriKind.Absolute, out Uri urlResult)
                && (urlResult.Scheme == Uri.UriSchemeHttp || urlResult.Scheme == Uri.UriSchemeHttps);
        }
        public async Task<bool> UrlExistAsync(string link)
        {
            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(5);
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, link));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<PageInfo?> GetUrlContentAsync(string link)
        {
            var result = new PageInfo();

            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout for HTTP requests
                string html = await client.GetStringAsync(link);

                HtmlDocument doc = new();
                doc.LoadHtml(html);

                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                result.Title = titleNode?.InnerText.Trim() ?? string.Empty;

                var ogTitleNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                result.OgTitle = ogTitleNode?.GetAttributeValue("content", null!);

                return result;
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                Console.WriteLine($"Error fetching content from {link}: {ex.Message}");
                return null;
            }
        }

        public bool IsGoogleMapsLink(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            string host = uri.Host.ToLower();
            return host.Contains("google.com") && uri.AbsolutePath.Contains("/maps")
           || host.Contains("goo.gl") && uri.AbsolutePath.StartsWith("/maps");
        }
    }
}
