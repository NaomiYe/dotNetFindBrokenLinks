using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindBrokenLinks
{
    //This class is for your use only.
    //All Items in this class are configurable and affect the way the app runs.
    //In a real web service there would be no use of this class, since some of them are IIS settings and would
    //be determined in IIS, some of them are internal settings and woulb be determined in server or service
    //settings and some of them are not necessary, only for the execution of this app in its current state.
    public class Settings
    {
        //I limited it here to a hard coded number, but IIS has settings for that, to avoid DDos attacks
        public int NumberOfIISSockets = 5;

        //Calculat the number of sockets to check the links.
        //By default, number of sockets can be between 1 to 20 and being calculate with log base 2. In a real
        //web service, that will be an internal setting.
        public int DeterminNumberOfSockets(int NumberOfLinksToCheck)
        {
            int ReturnNumberOfSockets = (int)Math.Ceiling(Math.Log(NumberOfLinksToCheck, 2));

            if (ReturnNumberOfSockets < 1)
            {
                return 1;
            }
            if (ReturnNumberOfSockets > 20)
            {
                return 20;
            }

            return ReturnNumberOfSockets;
        }

        //I limited those values here to a hard coded numbers, but in a real web service, those will be an internal setting.
        //Each link timeout value in milliseconds = minutes mul 60 seconds + seconds that are a part of a minute, all mul by 1000
        //Each web page links check timeout value in milliseconds = minutes mul 60 seconds + seconds that are a part of a minute, all mul by 1000
        public int LinksTimeoutInMilliseconds = ((3 * 60) + 0) * 1000;
        public int WebPageCheckTimeoutInMilliseconds = ((10 * 60) + 0) * 1000;

        //After this value of minutes, the results in the DB are not valid and a new links check is required
        public int TimePeriodForValidCheckResultsInMinutes = 10;
    }
}
