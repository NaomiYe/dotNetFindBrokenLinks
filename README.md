# dotNetFindBrokenLinks
Short program to find broken links in a web page, it emulates web server and utilises threads as much as possible, using an internal DB to keep results.

* In order to get this code running you will need to get the nuget package HtmlAgilityPack version 1.11.2. 

* Using internal database has it's merit (it can run everywhere without setup) but also its faults (data will not persist once the program stopps running)
