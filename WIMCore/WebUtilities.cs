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

        public static async Task LoginAsync(string username, string password)
        {
            Dictionary<string, string> contentDict = new Dictionary<string, string>();
            contentDict.Add("login_password", password);
            contentDict.Add("login_username", username);
            contentDict.Add("submit", "submit");
            FormUrlEncodedContent formEncContent = new FormUrlEncodedContent(contentDict);
            HttpResponseMessage response = await httpClient.PostAsync(LoginUrl, formEncContent);

            Task<HtmlDocument> responseDocTask = GetHtmlDocumentAsync(response);
            UpdateCookies(response);
            HtmlDocument responseDoc = await responseDocTask;
            HtmlNode titleNode = GetHtmlNodeByTag(responseDoc.DocumentNode, "title");
            if (titleNode.InnerText.Contains("Login Failed"))
                throw new Exception("Login error");
        }

        private static void UpdateCookies(HttpResponseMessage response)
        {
            List<string> cookieStrings = new List<string>(response.Headers.GetValues("Set-Cookie"));
            for (int i = 0; i < cookieStrings.Count; i++)
            {
                string cookieStr = cookieStrings[i];
                int eqIndex = cookieStr.IndexOf('=');
                if (eqIndex < 0)
                    throw new Exception("Invalid cookie string: " + cookieStr);
                string key = cookieStr.Substring(0, eqIndex);
                string val = cookieStr.Substring(eqIndex + 1, cookieStr.IndexOf(';') - (eqIndex + 1));

                if (cookieDict.ContainsKey(key))
                    cookieDict[key] = val;
                else
                    cookieDict.Add(key, val);
            }
        }

        private static List<string> GetCookieStrings()
        {
            List<string> cookieStrings = new List<string>();
            foreach (string key in cookieDict.Keys)
                cookieStrings.Add(key + '=' + cookieDict[key]);
            return cookieStrings;
        }

        public static async Task<string> GetHtmlStringAsync(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", GetCookieStrings());
            HttpResponseMessage response = await httpClient.SendAsync(request);
            Task<string> htmlTask = response.Content.ReadAsStringAsync();
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


        public static HtmlNode GetHtmlNodeByTag(HtmlNode rootNode, string tag)
        {
            List<HtmlNode> searchStack = new List<HtmlNode>() { rootNode };
            while (searchStack.Count > 0)
            {
                HtmlNode currentNode = searchStack[searchStack.Count - 1];
                if (currentNode.Name.Equals(tag, StringComparison.OrdinalIgnoreCase))
                    return currentNode;
                searchStack.RemoveAt(searchStack.Count - 1);
                searchStack.AddRange(currentNode.ChildNodes);
            }
            return null;
        }

        public static HtmlNode GetHtmlNodeByAttribute(HtmlNode rootNode, string attribName, string attribValue)
        {
            List<HtmlNode> searchStack = new List<HtmlNode> { rootNode };
            while (searchStack.Count > 0)
            {
                HtmlNode currentNode = searchStack[searchStack.Count - 1];
                if (currentNode.GetAttributeValue(attribName, "").Equals(attribValue, StringComparison.OrdinalIgnoreCase))
                    return currentNode;
                searchStack.RemoveAt(searchStack.Count - 1);
                searchStack.AddRange(currentNode.ChildNodes);
            }
            return null;
        }

        public static HtmlNode GetHtmlNodeByClass(HtmlNode rootNode, string className)
        {
            return GetHtmlNodeByAttribute(rootNode, "class", className);
        }

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
    }
}
