using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FindBrokenLinks.DataAccessLayer
{
    public class SQLRequests
    {
        public bool AddWebCheckResult(string webPageName, int numOfLinks, int numOfWorkingLinks, int numOfBrokenLinks, int numOfTimeoutLinks, int totalCheckTimeInSeconds)
        {
            bool returnValue = false;

            string connectionString = ConfigurationManager.ConnectionStrings["FindBrokenLinks.Properties.Settings.LocalDBConnectionString"].ConnectionString;
            using (SqlConnection con = new SqlConnection((connectionString)))
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO WebCheckResults (CheckTime, WebPage, NumberOfLinks, WorkingLinks, BrokenLinks, TimeoutLinks, TotalCheckTimeInSeconds)" +
                        "VALUES(@CheckTimeVal, @WebPageVal, @NumberOfLinksVal, @WorkingLinksVal, @BrokenLinksVal, @TimeoutLinksVal, @TotalCheckTimeInSecondsVal)", con))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@CheckTimeVal", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WebPageVal", webPageName);
                        cmd.Parameters.AddWithValue("@NumberOfLinksVal", numOfLinks);
                        cmd.Parameters.AddWithValue("@WorkingLinksVal", numOfWorkingLinks);
                        cmd.Parameters.AddWithValue("@BrokenLinksVal", numOfBrokenLinks);
                        cmd.Parameters.AddWithValue("@TimeoutLinksVal", numOfTimeoutLinks);
                        cmd.Parameters.AddWithValue("@TotalCheckTimeInSecondsVal", totalCheckTimeInSeconds);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        returnValue = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + ex.Message);
                        returnValue = false;
                    }
                    finally
                    {
                        if ((con != null) && (con.State == ConnectionState.Open))
                        {
                            con.Close();
                        }
                    }
                }
            }

            return returnValue;
        }

        public WebPageClass GetWebPageCheckResults(string _webPageName, int _validCheckResultsMinutesPeriod)
        {
            WebPageClass CurrentWebPage = new WebPageClass(_webPageName);

            string connectionString = ConfigurationManager.ConnectionStrings["FindBrokenLinks.Properties.Settings.LocalDBConnectionString"].ConnectionString;
            using (SqlConnection con = new SqlConnection((connectionString)))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM WebCheckResults where WebPage = @WebPageVal and CheckTime > @CheckTimeVal", con))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@WebPageVal", _webPageName);
                        cmd.Parameters.AddWithValue("@CheckTimeVal", DateTime.Now.AddMinutes(-_validCheckResultsMinutesPeriod));

                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CurrentWebPage.WebPageName = reader["WebPage"].ToString();
                                CurrentWebPage.AllLinks = int.Parse(reader["NumberOfLinks"].ToString());
                                CurrentWebPage.WorkinkLinks = int.Parse(reader["WorkingLinks"].ToString());
                                CurrentWebPage.BrokenLinks = int.Parse(reader["BrokenLinks"].ToString());
                                CurrentWebPage.TimeoutLinks = int.Parse(reader["TimeoutLinks"].ToString());
                                CurrentWebPage.TotalCheckTime = int.Parse(reader["TotalCheckTimeInSeconds"].ToString());
                            }
                            reader.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + ex.Message);
                        CurrentWebPage = new WebPageClass(_webPageName);
                    }
                    finally
                    {
                        if ((con != null) && (con.State == ConnectionState.Open))
                        {
                            con.Close();
                        }
                    }
                }
            }
            return CurrentWebPage;
        }

        public List<WebPageClass> GetAllDataFromDB()
        {
            List<WebPageClass> ReturnList = new List<WebPageClass>();

            string connectionString = ConfigurationManager.ConnectionStrings["FindBrokenLinks.Properties.Settings.LocalDBConnectionString"].ConnectionString;
            using (SqlConnection con = new SqlConnection((connectionString)))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM WebCheckResults", con))
                {
                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                WebPageClass newItem = new WebPageClass("");

                                newItem.WebPageName = reader["WebPage"].ToString();
                                newItem.AllLinks = int.Parse(reader["NumberOfLinks"].ToString());
                                newItem.WorkinkLinks = int.Parse(reader["WorkingLinks"].ToString());
                                newItem.BrokenLinks = int.Parse(reader["BrokenLinks"].ToString());
                                newItem.TimeoutLinks = int.Parse(reader["TimeoutLinks"].ToString());
                                newItem.TotalCheckTime = int.Parse(reader["TotalCheckTimeInSeconds"].ToString());

                                ReturnList.Add(newItem);
                            }
                            reader.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + ex.Message);
                    }
                    finally
                    {
                        if ((con != null) && (con.State == ConnectionState.Open))
                        {
                            con.Close();
                        }
                    }
                }
            }
            return ReturnList;
        }
    }
}
