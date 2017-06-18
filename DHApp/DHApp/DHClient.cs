using HtmlAgilityPack;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DHApp
{
    public static class DHClient
    {
        public static string Username { get; private set; }
        public static bool IsLoggedIn { get; private set; }

        public static System.Net.CookieContainer Cookies => _client.CookieContainer;
        //TODO: save cookies in settings & retrieve them on app launch. (not on this class)

        private static RestClient _client;

        public static async Task<bool> LoginAsync(string username, string password)
        {
            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            _client = new RestClient
            {
                CookieContainer = new System.Net.CookieContainer(),
                BaseUrl = new Uri(loginUrl)
            };

            var response = await _client
                .ExecuteTaskAsync(new RestRequest(Method.POST)
                .AddCookie("AspxAutoDetectCookieSupport", "1")
                .AddObject(new
                {
                    LoginName = Username = username,
                    Password = password,
                    RememberMe = "true"
                }));

            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);
            var node = doc.DocumentNode.ChildNodes["html"];

            return IsLoggedIn = (node?.Name == "html");
        }

        public static async Task LogoutAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            _client.BaseUrl = new Uri(logoutUrl);
            await _client.ExecuteTaskAsync(new RestRequest(Method.GET));

            _client = new RestClient();
            Username = null;
            IsLoggedIn = false;
        }

        public static async Task<IEnumerable<DHNotification>> GetNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            _client.BaseUrl = new Uri(getNotificationUrl);
            var response = await _client.ExecuteTaskAsync(new RestRequest(Method.GET));

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            string FixText(string source) =>
                System.Net.WebUtility.HtmlDecode(source.Replace("\n", string.Empty).Trim());

            return htmlDocument
                .DocumentNode
                .ChildNodes["div"]
                .Descendants("a")
                .Where(a => a.Attributes["class"].Value == "bildirim yeni")
                .Select(a =>
                {
                    var span = a.ChildNodes["span"];

                    string nodeText = FixText(a.InnerText);
                    string spanText = FixText(span.InnerText);
                    nodeText = FixText(nodeText.Remove(nodeText.Length - spanText.Length));

                    return new DHNotification
                    {
                        Url = forumUrl + FixText(a.Attributes["href"].Value),
                        IconUrl = span.ChildNodes["img"].Attributes["src"].Value,
                        Content = nodeText,
                        Time = spanText
                    };
                });
        }

        public static async Task IgnoreNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            _client.BaseUrl = new Uri(ignoreNotificationUrl);
            await _client.ExecuteTaskAsync(new RestRequest(Method.GET));
        }

        private const string forumUrl = "https://forum.donanimhaber.com";
        private const string loginUrl = forumUrl + "/Login";
        private const string logoutUrl = forumUrl + "/Logout";
        private const string getNotificationUrl = forumUrl + "/Notification/NotificationList";
        private const string ignoreNotificationUrl = forumUrl + "/Api/GlobalApi/IgnoreNotifications";
    }
}