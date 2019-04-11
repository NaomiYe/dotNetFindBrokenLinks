using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindBrokenLinks
{
    public class WebPageClass
    {
        public string WebPageName { get; set; }
        public int AllLinks { get; set; }
        public int WorkinkLinks { get; set; }
        public int BrokenLinks { get; set; }
        public int TimeoutLinks { get; set; }
        public int TotalCheckTime { get; set; }

        public WebPageClass(string _webPageName)
        {
            WebPageName = _webPageName;
            AllLinks = 0;
            WorkinkLinks = 0;
            BrokenLinks = 0;
            TimeoutLinks = 0;
            TotalCheckTime = 0;
        }
    }
}
