using FindBrokenLinks.DataAccessLayer;
using FindBrokenLinks.Utilities;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FindBrokenLinks
{
    class Program
    {
        static readonly object _objectLockWebPagesList = new object();
        static readonly object _objectLockWebPagesDictionary = new object();
        static readonly object _objectLockWebPageInternalLinksList = new object();
        static readonly object _objectLockWebPageClass = new object();

        //This list virtualizing IIS queue, a thread is inserting here web pages to check
        static List<string> WebPagesToCheck = new List<string>();

        //Threads are adding to this dictionary web pages to check. Each web page is a web request to the web service.
        static Dictionary<string, List<string>> WebPagesLinksToCheck = new Dictionary<string, List<string>>();

        static void Main(string[] args)
        {
            Settings settingsValues = new Settings();

            //Make all IIS sockets start working virtualy, since it is not a real web service project
            Task[] AllIISSockets = new Task[settingsValues.NumberOfIISSockets];

            for (int i = 0; i < settingsValues.NumberOfIISSockets; i++)
            {
                AllIISSockets[i] = Task.Factory.StartNew(() => CheckWebPageLinks());
            }

            //Virtualizing sending requests to the IIS
            Task AddRequestsTask = Task.Factory.StartNew(() => AddRequests());
            Task.WaitAll(AddRequestsTask);

            Console.Read();
        }

        //Add to this method commands like adding more web pages and sleap, in order to virtualize a real web service activity
        static void AddRequests()
        {
            WebPagesToCheck.Add("http://www.google.com");
            WebPagesToCheck.Add("http://www.ynet.co.il");
            WebPagesToCheck.Add("https://www.msn.com/he-il");

            Thread.Sleep(10000);
            WebPagesToCheck.Add("http://www.google.com");
            WebPagesToCheck.Add("http://www.wikipedia.co.il");
            WebPagesToCheck.Add("http://www.ynet.co.il");

            Thread.Sleep(10000);
            WebPagesToCheck.Add("http://www.google.com");
            WebPagesToCheck.Add("http://www.walla.co.il");
            WebPagesToCheck.Add("http://www.yahoo.co.il");
            WebPagesToCheck.Add("http://www.ynet.co.il");
            WebPagesToCheck.Add("http://www.mako.co.il");

            //Flag for printing all DB data once
            bool dataPrinted = false;

            while(!dataPrinted)
            {
                Thread.Sleep(15000);
                lock(_objectLockWebPagesDictionary)
                {
                    GeneralUtils generalUtils = new GeneralUtils();
                    dataPrinted = generalUtils.PrintAllDataFromDB(WebPagesToCheck.Count, WebPagesLinksToCheck);
                }
            }
        }

        static void CheckWebPageLinks()
        {
            string _currentWebPage = String.Empty;

            while (true) //Never stop (for virtualizing IIS socket)
            {
                GeneralUtils generalUtils = new GeneralUtils();

                if (String.IsNullOrEmpty(_currentWebPage))
                {
                    lock (_objectLockWebPagesList)
                    {
                        _currentWebPage = WebPagesToCheck.FirstOrDefault(); //Get web page from IIS queue
                        WebPagesToCheck.Remove(_currentWebPage);
                    }
                }

                if ((_currentWebPage != null) && (_currentWebPage != ""))
                {
                    WebPageClass CurrentWebPage = new WebPageClass(_currentWebPage);

                    Settings settingsValues = new Settings();
                    SQLRequests SQLReq = new SQLRequests();
                    CurrentWebPage = SQLReq.GetWebPageCheckResults(CurrentWebPage.WebPageName, settingsValues.TimePeriodForValidCheckResultsInMinutes);
                    if (CurrentWebPage.AllLinks > 0) //Results from last x minutes exist in the DB and will return to the request
                    {
                        Console.WriteLine("Results from DB - " + CurrentWebPage.WebPageName + Environment.NewLine + "Number of links = " + CurrentWebPage.AllLinks + "   Working links = " + CurrentWebPage.WorkinkLinks + "   Broken links = " + CurrentWebPage.BrokenLinks + "   Time out links = " + CurrentWebPage.TimeoutLinks + "    Total check time = " + generalUtils.ConvertSecondsToMinutes(CurrentWebPage.TotalCheckTime) + Environment.NewLine);
                        _currentWebPage = "";
                    }
                    else //No results exist in the DB
                    {
                        bool addToCheckWebList = false;

                        lock (_objectLockWebPagesDictionary)
                        {
                            if ((WebPagesLinksToCheck.ContainsKey(CurrentWebPage.WebPageName)) && (WebPagesLinksToCheck[CurrentWebPage.WebPageName].Count > 0)) //Web page links currently being checked
                            {
                                addToCheckWebList = false;
                                Thread.Sleep(60000); //Sleep 1 minutes to wait for the check results
                            }
                            else //Add a check web page to dictionary
                            {
                                addToCheckWebList = true;
                                if (!WebPagesLinksToCheck.ContainsKey(CurrentWebPage.WebPageName))
                                {
                                    WebPagesLinksToCheck.Add(CurrentWebPage.WebPageName, new List<string>());
                                }
                            }
                        }

                        if (addToCheckWebList) //Check web page links
                        {
                            DateTime StartTime = new DateTime();
                            DateTime EndTime = new DateTime();

                            StartTime = DateTime.Now;

                            //Get all links from http
                            WebUtils webUtils = new WebUtils();
                            List<string> LinksFromWebPage = webUtils.GetAllLinksFromWebPage(CurrentWebPage.WebPageName);
                            lock (_objectLockWebPagesDictionary)
                            {
                                foreach (string item in LinksFromWebPage)
                                {
                                    WebPagesLinksToCheck[CurrentWebPage.WebPageName].Add(item);
                                }
                                CurrentWebPage.AllLinks = WebPagesLinksToCheck[CurrentWebPage.WebPageName].Count;
                            }

                            if (CurrentWebPage.AllLinks > 0) //Address is valid and internet connection works
                            {
                                int numberOfLinksCheckTasks = settingsValues.DeterminNumberOfSockets(CurrentWebPage.AllLinks);
                                Task[] AllTasks = new Task[numberOfLinksCheckTasks];

                                for (int i = 0; i < numberOfLinksCheckTasks; i++)
                                {
                                    AllTasks[i] = Task.Factory.StartNew(() => CheckNextLink(CurrentWebPage));
                                }

                                //All unchecked links after timeout time will considered as timeout links
                                if (!Task.WaitAll(AllTasks, settingsValues.WebPageCheckTimeoutInMilliseconds))
                                {
                                    int allUncheckLinks = CurrentWebPage.AllLinks - CurrentWebPage.WorkinkLinks - CurrentWebPage.BrokenLinks - CurrentWebPage.TimeoutLinks;
                                    lock (_objectLockWebPageClass)
                                    {
                                        CurrentWebPage.TimeoutLinks = CurrentWebPage.TimeoutLinks + allUncheckLinks;
                                    }
                                }

                                EndTime = DateTime.Now;
                                TimeSpan ts = EndTime - StartTime;
                                CurrentWebPage.TotalCheckTime = (int)ts.TotalSeconds;

                                //Add result to the DB and print a message
                                if (SQLReq.AddWebCheckResult(CurrentWebPage.WebPageName, CurrentWebPage.AllLinks, CurrentWebPage.WorkinkLinks, CurrentWebPage.BrokenLinks, CurrentWebPage.TimeoutLinks, CurrentWebPage.TotalCheckTime))
                                {
                                    Console.WriteLine("INSERTED - " + CurrentWebPage.WebPageName + Environment.NewLine + "Number of links = " + CurrentWebPage.AllLinks + "   Working links = " + CurrentWebPage.WorkinkLinks + "   Broken links = " + CurrentWebPage.BrokenLinks + "   Time out links = " + CurrentWebPage.TimeoutLinks + "    Total check time = " + generalUtils.ConvertSecondsToMinutes(CurrentWebPage.TotalCheckTime) + Environment.NewLine);
                                }
                                else
                                {
                                    Console.WriteLine("NOT INSERTED - " + CurrentWebPage.WebPageName + Environment.NewLine);
                                }
                                _currentWebPage = String.Empty;
                            }
                            else //Address is not valid or no internet connection - set web page as timeout page
                            {
                                EndTime = DateTime.Now;
                                TimeSpan ts = EndTime - StartTime;
                                CurrentWebPage.TotalCheckTime = (int)ts.TotalSeconds;

                                //Add result to the DB (name and all counts are 0) and print a message
                                if (SQLReq.AddWebCheckResult(CurrentWebPage.WebPageName, 0, 0, 0, 0, CurrentWebPage.TotalCheckTime))
                                {
                                    Console.WriteLine("ERROR - Error reading web page links " + Environment.NewLine + "INSERTED - " + CurrentWebPage.WebPageName + Environment.NewLine + "Number of links = " + CurrentWebPage.AllLinks + "   Working links = " + CurrentWebPage.WorkinkLinks + "   Broken links = " + CurrentWebPage.BrokenLinks + "   Time out links = " + CurrentWebPage.TimeoutLinks + "    Total check time = " + generalUtils.ConvertSecondsToMinutes(CurrentWebPage.TotalCheckTime) + Environment.NewLine);
                                }
                                else
                                {
                                    Console.WriteLine("ERROR - Error reading web page links and DB insertion failed " + Environment.NewLine + "NOT INSERTED - " + CurrentWebPage.WebPageName + Environment.NewLine);
                                }
                                _currentWebPage = String.Empty;
                            }
                        }
                    }
                }

                Thread.Sleep(3000); //Sleep 3 seconds for reducing actions while queue is empty
            }
        }

        static void CheckNextLink(WebPageClass _currentWebPage)
        {
            bool ContinueRunning = true;
            string _currentLink = String.Empty;

            lock (_objectLockWebPageInternalLinksList)
            {
                _currentLink = WebPagesLinksToCheck[_currentWebPage.WebPageName].FirstOrDefault(); //Get link to check fromweb page
                WebPagesLinksToCheck[_currentWebPage.WebPageName].Remove(_currentLink);
            }

            if (String.IsNullOrEmpty(_currentLink))
            {
                ContinueRunning = false;
            }

            while (ContinueRunning) //While web page has unchecked links
            {
                //Update web page counters after getting result of links checks
                WebUtils webUtils = new WebUtils();
                switch (webUtils.GetLinkRequestCode(_currentLink))
                {
                    case 0:
                        lock (_objectLockWebPageClass)
                        {
                            _currentWebPage.WorkinkLinks++;
                        }
                        break;

                    case 1:
                        lock (_objectLockWebPageClass)
                        {
                            _currentWebPage.BrokenLinks++;
                        }
                        break;

                    default:
                        lock (_objectLockWebPageClass)
                        {
                            _currentWebPage.TimeoutLinks++;
                        }
                        break;
                }
                
                lock (_objectLockWebPagesDictionary)
                {
                    _currentLink = WebPagesLinksToCheck[_currentWebPage.WebPageName].FirstOrDefault(); //Get link to check fromweb page
                    WebPagesLinksToCheck[_currentWebPage.WebPageName].Remove(_currentLink);
                }

                //Stop when web page has its all links checked
                if (String.IsNullOrEmpty(_currentLink))
                {
                    ContinueRunning = false;
                }
            }
        }
    }
}
