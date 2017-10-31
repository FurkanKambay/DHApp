using HtmlAgilityPack;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DHApp
{
    public static class DHClient
    {
        #region Fields and Events
        public static string Username { get; private set; }
        public static string AvatarUrl { get; private set; }
        public static bool IsLoggedIn { get; private set; }

        public static event Action<string> Login;
        public static event Action Logout;

        private static RestClient client = new RestClient
        {
            CookieContainer = new CookieContainer(),
            BaseUrl = new Uri(forumUrl)
        };
        #endregion Fields and Events

        #region Public Methods
        public static async Task<bool> LogInAsync(string username, string password)
        {
            Contract.Requires(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password));
            Contract.Requires(IsLoggedIn, "Already logged in");

            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var response = await client.ExecutePostTaskAsync(
                new RestRequest(loginPath)
                .AddCookie("AspxAutoDetectCookieSupport", "1")
                .AddObject(new
                {
                    LoginName = username,
                    Password = password,
                    RememberMe = true
                }));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(response.Content);

                if (doc.DocumentNode.ChildNodes["html"] != null)
                {
                    var encryptedCookie = GetLoginCookie(client.CookieContainer.GetCookieHeader(new Uri(forumUrl)));

                    Username = GetUserNameDecoded(encryptedCookie);
                    IsLoggedIn = true;

                    Login?.Invoke(Convert.ToBase64String(
                        ProtectedData.Protect(
                            Encoding.ASCII.GetBytes(encryptedCookie),
                            Encoding.ASCII.GetBytes(entropy),
                            DataProtectionScope.CurrentUser)));
                }
            }

            return IsLoggedIn;
        }

        public static bool LogInWithCookie(string encryptedCookie)
        {
            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(encryptedCookie))
                return false;

            var cookie = GetLoginCookie(Encoding.ASCII.GetString(
                ProtectedData.Unprotect(
                        Convert.FromBase64String(encryptedCookie),
                        Encoding.ASCII.GetBytes(entropy),
                        DataProtectionScope.CurrentUser)));

            if (string.IsNullOrEmpty(cookie))
                throw new InvalidOperationException("Cookies are corrupt");

            //TODO: Check if login worked

            //var response = await client.ExecuteGetTaskAsync(
            //    new RestRequest(getNotificationsPath).AddCookie(cookieName, cookie));

            //var htmlDoc = new HtmlDocument();
            //htmlDoc.LoadHtml(response.Content);

            //if (htmlDoc.DocumentNode.ChildNodes.Count < 1)
            //    return false;

            client.CookieContainer.Add(new Uri(forumUrl), new Cookie(cookieName, cookie));

            Username = GetUserNameDecoded(cookie);
            IsLoggedIn = true;

            Login?.Invoke(encryptedCookie);
            return IsLoggedIn;
        }

        public static async Task LogOutAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            await client.ExecuteGetTaskAsync(new RestRequest(logoutPath));

            client = new RestClient
            {
                CookieContainer = new CookieContainer(),
                BaseUrl = new Uri(forumUrl)
            };

            IsLoggedIn = false;
            Username = null;
            AvatarUrl = null;

            Logout?.Invoke();
        }

        public static async Task<DHNotification[]> GetNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            var response = await client.ExecuteGetTaskAsync(new RestRequest(getNotificationsPath));

            //var altResponse = await client.ExecuteGetTaskAsync(
            //    new RestRequest("/getNotifications_ajax.asp")
            //    .AddQueryParameter("mode", "profile")
            //    .AddQueryParameter("top", "20")
            //    .AddQueryParameter("n", "1")
            //    .AddQueryParameter("filter", "6"));
            //string altContent = altResponse.Content;

            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            return htmlDocument
                .DocumentNode
                .ChildNodes["div"]
                .Descendants("a")
                .Where(a => a.Attributes["class"].Value == "bildirim yeni")
                .Select(HtmlNodeToNotification)
                .ToArray();
        }

        public static async Task IgnoreNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            await client.ExecuteGetTaskAsync(new RestRequest(ignoreNotificationsPath));
        }

        public static async Task<string> GetAvatarUrlAsync(string username = null)
        {
            if (username == null)
                username = Username;

            var response = await client.ExecuteGetTaskAsync(
                new RestRequest(getUserAvatarPath).AddQueryParameter("q", username));

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            string avatarUrl = null;
            foreach (var node in htmlDocument.DocumentNode.Descendants("a"))
            {
                if (FixText(node.InnerText, "\\n") == username)
                {
                    var url = node
                        .ChildNodes["div"]
                        .ChildNodes["img"]
                        .Attributes["src"]
                        .Value
                        .Trim('\\', '"');

                    if (!string.IsNullOrWhiteSpace(url) && url != "\"\"")
                    {
                        avatarUrl = url;
                        break;
                    }
                }
            }

            return (username == Username)
                ? AvatarUrl = avatarUrl
                : avatarUrl;
        }
        #endregion Public Methods

        #region Private Methods
        private static DHNotification HtmlNodeToNotification(HtmlNode node)
        {
            string iconUrl = null;
            string time = null;

            var span = node.ChildNodes.FindFirst("span");

            iconUrl = span.ChildNodes["img"].Attributes["src"].Value;
            time = FixText(span.InnerText);

            span.Remove();

            return new DHNotification
            (
                content: FixText(node.InnerHtml),
                time: time,
                url: forumUrl + FixText(node.Attributes["href"].Value),
                iconUrl: iconUrl
            );
        }

        private static string FixText(string source, string toRemove = "\n") =>
            WebUtility.HtmlDecode(source.Replace(toRemove, string.Empty).Trim());

        private static string GetUserNameDecoded(string sourceCookie) =>
            HttpUtility.UrlDecode(Regex.Match(sourceCookie, cookiePattern).Groups[1].Value);

        private static string GetLoginCookie(string cookies) =>
            Regex.Match(cookies, cookiePattern).Value;
        #endregion Private Methods

        #region Constants
        private const string entropy = "[sm=dont.gif]";
        private const string cookieName = "db4655ASPplayground%5Fforum";
        private const string cookiePattern = "Login=(.+)&Password=.+";
        private const string forumUrl = "https://forum.donanimhaber.com";
        private const string loginPath = "/Login";
        private const string logoutPath = "/Logout";
        private const string getNotificationsPath = "/Notification/NotificationList";
        private const string ignoreNotificationsPath = "/Api/GlobalApi/IgnoreNotifications";
        private const string getUserAvatarPath = "/Api/GlobalApi/GetReplyUserByLoginName";
        #endregion Constants
    }
}