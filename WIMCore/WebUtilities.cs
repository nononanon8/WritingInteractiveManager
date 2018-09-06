using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;

namespace WIMCore
{
    public static class WebUtilities
    {
        private const string LoginUrl = "https://www.writing.com/main/login.php";

        private static HttpClient httpClient = new HttpClient();
        private static Dictionary<string, string> cookieDict = new Dictionary<string, string>();

        // Login to Writing.com.
        public static async Task LoginAsync(string username, string password)
        {
            // Endcode login info.
            Dictionary<string, string> contentDict = new Dictionary<string, string>();
            contentDict.Add("login_password", password);
            contentDict.Add("login_username", username);
            // Not sure if this is needed, but it's sent by Edge browser.
            contentDict.Add("submit", "submit");
            FormUrlEncodedContent formEncContent = new FormUrlEncodedContent(contentDict);
            // Send post message with login info, get response.
            HttpResponseMessage response = await httpClient.PostAsync(LoginUrl, formEncContent);
            // Start decoding HTML document from response.
            Task<HtmlDocument> responseDocTask = GetHtmlDocumentAsync(response);
            // Response contains new cookie values, including user token from login.
            UpdateCookies(response);
            // Finish decoding HTML document.
            HtmlDocument responseDoc = await responseDocTask;
            // Check page title text to see if login was successful.
            HtmlNode titleNode = responseDoc.DocumentNode.SelectSingleNode(ParseParams.PageTitleXPath);
            if (titleNode.InnerText.Contains("Login Failed"))
                throw new Exception("Login failed");
        }

        // Update our dictionary of cookie values with those contained in response.
        private static void UpdateCookies(HttpResponseMessage response)
        {
            // Values to upate are contained in "Set-Cookie" headers.
            List<string> cookieStrings = new List<string>(response.Headers.GetValues("Set-Cookie"));
            // For each cookie...
            for (int i = 0; i < cookieStrings.Count; i++)
            {
                // Format is "cookieName=cookieValue; cookieAttribute1=attribute1Value; ..."
                // Attributes are ignored for now.
                // Parse out cookie name (dictionary key) and value.
                string cookieStr = cookieStrings[i];
                int eqIndex = cookieStr.IndexOf('=');
                // Ignore if '=' not found.
                if (eqIndex >= 0)
                {
                    string key = cookieStr.Substring(0, eqIndex);
                    string val = cookieStr.Substring(eqIndex + 1, cookieStr.IndexOf(';') - (eqIndex + 1));
                    // Update existing cookie value or add new cookie to dictionary.
                    if (cookieDict.ContainsKey(key))
                        cookieDict[key] = val;
                    else
                        cookieDict.Add(key, val);
                }
                
            }
        }

        // Convert cookie dictionary back into a set of formated strings.
        private static List<string> GetCookieStrings()
        {
            List<string> cookieStrings = new List<string>();
            foreach (string key in cookieDict.Keys)
                cookieStrings.Add(key + '=' + cookieDict[key]);
            return cookieStrings;
        }

        // Get page HTML (as a string) from a url.
        public static async Task<string> GetHtmlStringAsync(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            // Add cookies so we can access member-only pages with login info.
            request.Headers.Add("Cookie", GetCookieStrings());
            //if (cookieDict.ContainsKey("user_token"))
            //    request.Headers.Add("Cookie", "user_token=" + cookieDict["user_token"]);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            Task<string> htmlTask = response.Content.ReadAsStringAsync();
            // All responses contain "Set-Cookie" headers, not sure how important they are.
            UpdateCookies(response);
            return await htmlTask;
        }

        public static async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            string html = await GetHtmlStringAsync(url);
            return HtmlString2Doc(html);
        }

        private static async Task<HtmlDocument> GetHtmlDocumentAsync(HttpResponseMessage response)
        {
            string html = await response.Content.ReadAsStringAsync();
            return HtmlString2Doc(html);
        }

        public static HtmlDocument HtmlString2Doc(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc;
        }

        // Certain text symbols have an HTML escape encoding (there's probably a proper name, idk).
        // This replaces those with the correct characters.
        public static string CleanHtmlSymbols(string rawText)
        {
            int htmlSymbolIndex = rawText.IndexOf("&#");
            while (htmlSymbolIndex >= 0)
            {
                string htmlNumStr = rawText.Substring(htmlSymbolIndex + 2, 2);
                string symbol = "" + (char)int.Parse(htmlNumStr);
                rawText = rawText.Remove(htmlSymbolIndex, 5);
                rawText = rawText.Insert(htmlSymbolIndex, symbol);
                htmlSymbolIndex = rawText.IndexOf("&#");
            }
            return rawText;
        }

        public static string GetHtmlNodeText(HtmlDocument doc, string xPath)
        {
            HtmlNode node = doc.DocumentNode.SelectSingleNode(xPath);
            if (node == null)
                return "data not found";
            return CleanHtmlSymbols(node.InnerText);
        }


    }
}
