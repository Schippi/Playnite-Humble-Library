using Playnite;
using Playnite.SDK;
using System;
using System.Net;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using CefSharp;

namespace humble
{

    public class HumbleAccountClient
    {

        private static ILogger logger = LogManager.GetLogger();
        private const string loginUrl = @"https://www.humblebundle.com/login?goto=/api/v1/user/order";
        private const string tokenUrl = @"https://www.humblebundle.com/api/v1/user/order";
        private const string logoutUrl = @"https://www.humblebundle.com/logout?goto=/resender";
        private IWebView webView;

        public HumbleAccountClient(IWebView web)
        {
            this.webView = web;
        }

        public void Login()
        {
            webView.NavigationChanged += (s, e) =>
            {
                if (webView.GetCurrentAddress().StartsWith(@"https://www.humblebundle.com/api"))
                {
                    //webView.GetC
                    webView.Close();
                }

                if (webView.GetCurrentAddress().StartsWith(@"https://www.humblebundle.com/resender"))
                {
                    webView.Navigate(loginUrl);
                }
            };
            webView.DeleteCookies("www.humblebundle.com", "_simpleauth_sess");
            webView.DeleteCookies("www.humblebundle.com", "csrf_cookie");
            webView.Navigate(loginUrl);
            webView.OpenDialog();
        }

     //  public class MyCookieVisitor : ICookieVisitor
     //  {

     //      private static ILogger logger = LogManager.GetLogger();
     //      public bool Visit(CefSharp.Cookie cookie, int count, int total, ref bool deleteCookie)
     //      {
     //          logger.Info(cookie.Domain);
     //          logger.Info(cookie.Name);
     //          logger.Info(cookie.Value);
     //          logger.Info(cookie.Path);
     //          return true;
     //      }
     //      public void Dispose()
     //      {

     //      }
     //  }

        public string GetAccessToken()
        {
            webView.NavigateAndWait(tokenUrl);

            string myjson = webView.GetPageText();
            JArray objects = null;

            try
            {
                
                objects = JArray.Parse(webView.GetPageText());
                //MyCookieVisitor v = new MyCookieVisitor();
              //  Cef.GetGlobalCookieManager().VisitAllCookies(v);


                return "x";
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public bool GetIsUserLoggedIn()
        {
            var token = GetAccessToken()[0].ToString();
            return string.IsNullOrEmpty(token);
        }

    }
}