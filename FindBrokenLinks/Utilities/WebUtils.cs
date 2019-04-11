using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FindBrokenLinks.Utilities
{
    public class WebUtils
    {
        public List<string> GetAllLinksFromWebPage(string _webPage)
        {
            List<string> ReturnList = new List<string>();

            try
            {
                Settings settingsValues = new Settings();

                HtmlWeb hw = new HtmlWeb();
                hw.PreRequest = delegate (HttpWebRequest webRequest)
                {
                    webRequest.Timeout = settingsValues.LinksTimeoutInMilliseconds;
                    webRequest.ReadWriteTimeout = settingsValues.LinksTimeoutInMilliseconds;
                    return true;
                };

                HtmlDocument doc = hw.Load(_webPage);

                var linkTags = doc.DocumentNode.Descendants("link");
                var linkedPages = doc.DocumentNode.Descendants("a")
                                                  .Select(a => a.GetAttributeValue("href", null))
                                                  .Where(u => !String.IsNullOrEmpty(u));

                foreach (var item in linkTags)
                {
                    string CurrentLink = ReturnValidWebAddress(item.ToString(), _webPage);

                    if (CurrentLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        ReturnList.Add(CurrentLink);
                    }
                }

                foreach (var item in linkedPages)
                {
                    string CurrentLink = ReturnValidWebAddress(item.ToString(), _webPage);

                    if (CurrentLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        ReturnList.Add(CurrentLink);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR - Error in getting all links from web page - " + ex.Message);
            }

            return ReturnList;
        }

        public int GetLinkRequestCode(string _currentLink)
        {
            //return codes: 0 - OK, 1 - Broken link, 2 - Timeout link
            int returnCode = -1;

            Settings settingsValues = new Settings();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_currentLink);
            request.AllowAutoRedirect = true;
            request.Accept = "*/*";
            request.UserAgent = AppDomain.CurrentDomain.DomainManager.EntryAssembly.GetName().Name;
            request.CookieContainer = new CookieContainer();
            request.Timeout = settingsValues.LinksTimeoutInMilliseconds;
            request.ReadWriteTimeout = settingsValues.LinksTimeoutInMilliseconds;
            request.ServicePoint.Expect100Continue = false;
            request.KeepAlive = false;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            returnCode = 0;
                            break;

                        case HttpStatusCode.NotFound:
                            returnCode = 1;
                            break;

                        default:
                            returnCode = 2;
                            break;
                    }
                }
            }
            catch (WebException we)
            {
                if (we.Response == null)
                {
                    returnCode = 2;
                }
                else
                {
                    var resp = (HttpWebResponse)we.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        returnCode = 1;
                    }
                    else
                    {
                        returnCode = 2;
                    }
                }
            }

            if (request != null)
                request.Abort();

            return returnCode;
        }

        string ReturnValidWebAddress(string _currentAddress, string _currentWebPageChecked)
        {
            string ReturnAddress = _currentAddress.TrimStart();
            if (ReturnAddress.StartsWith(@"www"))
            {
                ReturnAddress = "http://" + _currentAddress.TrimStart();
            }
            if (ReturnAddress.StartsWith(@"//www"))
            {
                ReturnAddress = "http:" + _currentAddress.TrimStart();
            }
            if (ReturnAddress.StartsWith(@"/"))
            {
                ReturnAddress = _currentWebPageChecked + _currentAddress.TrimStart();
            }

            return ReturnAddress;
        }
    }
}
