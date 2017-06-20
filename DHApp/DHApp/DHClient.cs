using HtmlAgilityPack;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace DHApp
{
    public static class DHClient
    {
        public static string Username { get; private set; }
        public static bool IsLoggedIn { get; private set; }

        public static event Action<string> Login;
        public static event Action Logout;

        private static RestClient client = new RestClient
        {
            CookieContainer = new CookieContainer(),
            BaseUrl = new Uri(forumUrl)
        };

        public static async Task<bool> LoginAsync(string username, string password)
        {
            Contract.Requires(string.IsNullOrEmpty(password) == false);
            Contract.Requires(string.IsNullOrEmpty(username) == false);

            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var response = await client.ExecuteTaskAsync(
                new RestRequest(loginPath, Method.POST)
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

                if (IsLoggedIn = doc.DocumentNode.ChildNodes["html"]?.Name == "html")
                {
                    string cookie = FindLoginCookie(client.CookieContainer
                        .GetCookieHeader(new Uri(forumUrl)));

                    string encryptedCookie = Convert.ToBase64String(
                        ProtectedData.Protect(
                            Encoding.ASCII.GetBytes(cookie),
                            Encoding.ASCII.GetBytes(entropy),
                            DataProtectionScope.CurrentUser));

                    Username = FindUsernameInCookie(cookie);
                    Login?.Invoke(encryptedCookie);
                }
            }

            return IsLoggedIn;
        }

        public static bool LoginWithCookie(string encryptedCookie)
        {
            if (IsLoggedIn)
                throw new InvalidOperationException("Already logged in");

            if (string.IsNullOrWhiteSpace(encryptedCookie))
                return false;

            string cookie = FindLoginCookie(
                Encoding.ASCII.GetString(
                    ProtectedData.Unprotect(
                        Convert.FromBase64String(encryptedCookie),
                        Encoding.ASCII.GetBytes(entropy),
                        DataProtectionScope.CurrentUser)));

            Username = FindUsernameInCookie(cookie);

            client.CookieContainer.Add(new Uri(forumUrl), new Cookie(cookieName, cookie));

            IsLoggedIn = true;
            Login?.Invoke(encryptedCookie);

            return IsLoggedIn; //TODO: check if login worked
        }

        public static async Task LogoutAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            await client.ExecuteTaskAsync(new RestRequest(logoutPath, Method.GET));
            client = new RestClient
            {
                CookieContainer = new CookieContainer(),
                BaseUrl = new Uri(forumUrl)
            };

            Username = null;
            IsLoggedIn = false;

            Logout?.Invoke();
        }

        public static async Task<IEnumerable<DHNotification>> GetNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            var response = await client.ExecuteTaskAsync(
                new RestRequest(getNotificationsPath, Method.GET));

            if (response.StatusCode != HttpStatusCode.OK)
                return null; //todo: throw?

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response.Content);

            string fixText(string source) =>
                WebUtility.HtmlDecode(source.Replace("\n", string.Empty).Trim());

            return htmlDocument
                .DocumentNode
                .ChildNodes["div"]
                .Descendants("a")
                .Where(a => a.Attributes["class"].Value == "bildirim yeni")
                .Select(a =>
                {
                    var span = a.ChildNodes["span"];

                    string nodeText = fixText(a.InnerText);
                    string spanText = fixText(span.InnerText);
                    nodeText = fixText(nodeText.Remove(nodeText.Length - spanText.Length));

                    return new DHNotification(nodeText,
                        spanText,
                        forumUrl + fixText(a.Attributes["href"].Value),
                        span.ChildNodes["img"].Attributes["src"].Value
                        );
                });
        }

        public static async Task IgnoreNotificationsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            var response = await client.ExecuteTaskAsync(new RestRequest(ignoreNotificationsPath, Method.GET));
        }

        private static string FindUsernameInCookie(string sourceCookie) =>
            HttpUtility.UrlDecode(
                Regex.Match(sourceCookie, cookiePattern)
                .Groups[1].Value);

        private static string FindLoginCookie(string cookies) =>
            cookies.Split(';')
            .SingleOrDefault(c => c.Contains(cookieName))
            ?.Replace(cookieName + "=", string.Empty)
            .Trim() ?? cookies;

        private const string entropy = "[sm=dont.gif]";
        private const string cookiePattern = "^Login=(.+)&Password=.+$";
        private const string cookieName = "db4655ASPplayground%5Fforum";
        private const string forumUrl = "https://forum.donanimhaber.com";
        private const string loginPath = "/Login";
        private const string logoutPath = "/Logout";
        private const string getNotificationsPath = "/Notification/NotificationList";
        private const string ignoreNotificationsPath = "/Api/GlobalApi/IgnoreNotifications";
    }
}