using FindBrokenLinks.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindBrokenLinks.Utilities
{
    public class GeneralUtils
    {
        public bool PrintAllDataFromDB(int _inIISQueue, Dictionary<string, List<string>> _allWebChecked)
        {
            bool printAllData = true;

            foreach (var item in _allWebChecked)
            {
                if (item.Value.Count > 0)
                {
                    printAllData = false;
                    break;
                }
            }
            if (_inIISQueue > 0)
            {
                printAllData = false;
            }

            if (printAllData)
            {
                SQLRequests SQLReq = new SQLRequests();
                List<WebPageClass> AllDBData = SQLReq.GetAllDataFromDB();

                Console.WriteLine(Environment.NewLine + "DB Contant:" + Environment.NewLine);
                foreach (WebPageClass item in AllDBData)
                {
                    Console.WriteLine(item.WebPageName + Environment.NewLine + "Number of links = " + item.AllLinks + "   Working links = " + item.WorkinkLinks + "   Broken links = " + item.BrokenLinks + "   Time out links = " + item.TimeoutLinks + "    Total check time = " + ConvertSecondsToMinutes(item.TotalCheckTime) + Environment.NewLine);
                }
            }

            return printAllData;
        }

        public string ConvertSecondsToMinutes(int _numOfSecs)
        {
            TimeSpan ts = TimeSpan.FromSeconds(_numOfSecs);
            return ts.ToString(@"mm\:ss");
        }
    }
}
