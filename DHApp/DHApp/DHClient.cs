using HtmlAgilityPack;
using RestSharp;
using System;
using System.Collections.Generic;
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

        public static event Action<string> LoggedIn;
        public static event Action LoggedOut;

        private static RestClient client = new RestClient
        {
            CookieContainer = new CookieContainer(),
            BaseUrl = new Uri(forumUrl)
        };
        #endregion Fields and Events

        #region Public Methods
        public static async Task<bool> LogInAsync(string username, string password)
        {
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

                if (doc.DocumentNode.ChildNodes["html"]?.Name == "html")
                {
                    string cookie = GetLoginCookie(client.CookieContainer.GetCookieHeader(new Uri(forumUrl)));

                    Username = GetUserNameDecoded(cookie);
                    AvatarUrl = await GetAvatarUrlAsync(Username);
                    IsLoggedIn = true;

                    LoggedIn?.Invoke(Convert.ToBase64String(
                        ProtectedData.Protect(
                            Encoding.ASCII.GetBytes(cookie),
                            Encoding.ASCII.GetBytes(entropy),
                            DataProtectionScope.CurrentUser)));
                }
            }

            return IsLoggedIn;
        }

        public static async Task<bool> LogInWithCookieAsync(string encryptedCookie)
        {
            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(encryptedCookie))
                return false;

            string cookie = GetLoginCookie(Encoding.ASCII.GetString(
                    ProtectedData.Unprotect(
                        Convert.FromBase64String(encryptedCookie),
                        Encoding.ASCII.GetBytes(entropy),
                        DataProtectionScope.CurrentUser)));

            client.CookieContainer.Add(new Uri(forumUrl), new Cookie(cookieName, cookie));

            Username = GetUserNameDecoded(cookie);
            AvatarUrl = await GetAvatarUrlAsync(Username);
            IsLoggedIn = true;

            LoggedIn?.Invoke(encryptedCookie);
            return IsLoggedIn; //TODO: check if login worked
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

            LoggedOut?.Invoke();
        }

        public static async Task<IEnumerable<DHNotification>> GetNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            var response = await client.ExecuteGetTaskAsync(new RestRequest(getNotificationsPath));

            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            //client.BaseUrl = new Uri("https://mobile.donanimhaber.com");
            //var otherResponse = await client
            //    .ExecuteTaskAsync(
            //    new RestRequest("/getNotifications_ajax.asp", Method.GET)
            //    .AddQueryParameter("mode", "profile")
            //    .AddQueryParameter("top", "20")
            //    .AddQueryParameter("n", "1")
            //    .AddQueryParameter("filter", "6")
            //    );
            //string resp = otherResponse.Content;

            return htmlDocument
                .DocumentNode
                .ChildNodes["div"]
                .Descendants("a")
                .Where(a => a.Attributes["class"].Value.Contains("bildirim"))
                .Select(a =>
                {
                    var span = a.ChildNodes["span"];

                    string iconUrl = string.Empty;
                    string time = "Hata";

                    if (span != null) //todo: unexpected, might be server-side
                    {
                        iconUrl = span.ChildNodes["img"].Attributes["src"].Value;
                        time = FixText(span.InnerText);
                        span.Remove();
                    }

                    a.InnerHtml = FixText(a.InnerHtml); //undone: "collection is changed"?

                    return new DHNotification
                    {
                        Content = a.InnerText, //TODO: <strong> & <i> formatting
                        Time = time,
                        Url = forumUrl + FixText(a.Attributes["href"].Value),
                        IconUrl = iconUrl,
                        IsNew = a.Attributes["class"].Value == "bildirim yeni"
                    };
                });
        }

        public static async Task IgnoreNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            await client.ExecuteGetTaskAsync(new RestRequest(ignoreNotificationsPath));
        }
        #endregion Public Methods

        #region Private Methods
        private static async Task<string> GetAvatarUrlAsync(string username)
        {
            var response = await client.ExecuteGetTaskAsync(
                new RestRequest(getUserAvatarPath).AddQueryParameter("q", username));

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            foreach (var node in htmlDocument.DocumentNode.Descendants("a"))
            {
                if (FixText(node.InnerText, "\\n") == username)
                {
                    string url = node
                        .ChildNodes["div"]
                        .ChildNodes["img"]
                        .Attributes["src"]
                        .Value
                        .Trim('\\', '"');

                    if (url != "\"\"")
                        return url;
                }
            }

            return null;
        }

        private static string FixText(string source, string toRemove = "\n") =>
            WebUtility.HtmlDecode(source.Replace(toRemove, string.Empty).Trim());

        private static string GetUserNameDecoded(string sourceCookie) =>
            WebUtility.HtmlDecode(
                Regex.Match(sourceCookie, cookiePattern)
                .Groups[1].Value);

        private static string GetLoginCookie(string cookies) =>
            cookies.Split(';')
            .SingleOrDefault(c => c.Contains(cookieName))
            ?.Replace(cookieName + "=", string.Empty)
            .Trim() ?? cookies;
        #endregion Private Methods

        #region Constants
        private const string entropy = "[sm=dont.gif]";
        private const string cookiePattern = "^Login=(.+)&Password=.+$";
        private const string cookieName = "db4655ASPplayground%5Fforum";
        private const string forumUrl = "https://forum.donanimhaber.com";
        private const string loginPath = "/Login";
        private const string logoutPath = "/Logout";
        private const string getNotificationsPath = "/Notification/NotificationList";
        private const string ignoreNotificationsPath = "/Api/GlobalApi/IgnoreNotifications";
        private const string getUserAvatarPath = "/Api/GlobalApi/GetReplyUserByLoginName";
        #endregion Constants
    }
}