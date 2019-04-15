using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.IO;

namespace ADUpkeep
{
  class Program
  {
    // To Do: Check Chameleon, Pub Works, CAD, and Finplus for accounts that should be deactivated.
    // Considerations:  
    //    Probably can't check Finplus or CAD because of a lack of SQL access to being able to look at Login information.
    //    Not sure where user data is kept in Chameleon, if it is.
    //    


    public const int app_id = 20008;
    public const string error_email_to = "helpdesk@claycountygov.com";    

    static void Main(string[] args)
    {
      if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday) return;

      var ad_issue = new StringBuilder();
      
      var telestaff_employees = telestaff_employee.Get();
      var ad_accounts = ad_account.GetAllEmployees(telestaff_employees);
      var finplus_employees = finplus_employee.GetEmployees();
      var termed_pubworks_employees = finplus_employee.GetActivePubWorksEmployeesThatAreTermed();

      ad_issue.AppendLine(LookForNewEmployees(ad_accounts, finplus_employees));

      if(DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
      {
        EmailPubWorksCheck(PrintTermedPubworksEmployees(termed_pubworks_employees));
        ad_issue.AppendLine(LookForTerminatedEmployeesWithActiveADAccounts(ad_accounts, finplus_employees));
        ad_issue.AppendLine(CheckTelestaffInfo(ad_accounts, telestaff_employees));
      }
      EmailADCheck(ad_issue.ToString());

      ////// save it to a temp file
      //if(ad_issue.Length > 0)
      //{
      //  File.WriteAllText("C:\\Temp\\AD-Test 2019-04-15.txt", ad_issue.ToString());
      //}
    }

    static string LookForNewEmployees(Dictionary<int, ad_account> ad_accounts, Dictionary<int, finplus_employee> finplus_employees)
    {
      var ad_issue = new StringBuilder();

      foreach (int id in finplus_employees.Keys)
      {
        // let's check some stuff
        if (!finplus_employees[id].is_terminated)
        {
          // if the user isn't terminated in finplus, let's look to make sure we were able to find
          // an AD account for them.
          if (!ad_accounts.ContainsKey(id) || (ad_accounts.ContainsKey(id) && !ad_accounts[id].active))
          {
            ad_issue.Append(finplus_employees[id].id.ToString())
              .Append(" - ")
              .Append(finplus_employees[id].name)
              .Append(" - ")
              .AppendLine("No active AD account found for that employee.");
          }
        }
      }
      return ad_issue.ToString();
    }

    static string LookForTerminatedEmployeesWithActiveADAccounts(Dictionary<int, ad_account> ad_accounts, Dictionary<int, finplus_employee> finplus_employees)
    {
      var ad_issue = new StringBuilder();

      foreach (int id in finplus_employees.Keys)
      {
        // if the user is terminated in finplus, their AD account should be inactive too.
        if (finplus_employees[id].is_terminated)
        {
          if (ad_accounts.ContainsKey(id) && ad_accounts[id].active)
          {
            ad_issue.Append(finplus_employees[id].id.ToString())
              .Append(" - ")
              .Append(finplus_employees[id].name)
              .Append(" - Username: ")
              .Append(ad_accounts[id].user_name)
              .Append(" - ")
              .AppendLine("AD account still active for terminated employee.");
          }
        }
      }
      return ad_issue.ToString();
    }

    static string CheckTelestaffInfo(Dictionary<int, ad_account> ad_accounts, Dictionary<int, telestaff_employee> telestaff_employees)
    {
      var ad_issue = new StringBuilder();
      var lieutenant_groups = new List<string>() { "gPSLieutenants" };
      var captain_groups = new List<string>() { "gPSCaptains" };
      var bc_groups = new List<string>() { "gPSBattalionChiefs" };
      // now let's loop through all of the employees 
      // and compare titles to access
      foreach(int id in telestaff_employees.Keys)
      {
        try
        {
          if (!ad_accounts.ContainsKey(id) || !ad_accounts[id].found)
          {
            // houston we have a problem
            new ErrorLog("Missing AD account for telestaff employee: " + id.ToString(), telestaff_employees[id].name, "", "", "");            
          }
          else
          {
            if (ad_accounts[id].title.ToLower() != ad_accounts[id].title.ToLower())
            {
              ad_issue.Append(id.ToString())
                .Append(" - ")
                .Append(telestaff_employees[id].name)
                .Append(" - Title does not match.  AD Title: ")
                .Append(ad_accounts[id].title)
                .Append(" - Telestaff Title: ")
                .AppendLine(telestaff_employees[id].rank);
            }
            switch (telestaff_employees[id].rank.ToLower())
            {
              case "lieutenant":
                foreach(string g in lieutenant_groups)
                {
                  if (!ad_accounts[id].groups.Contains(g))
                  {
                    ad_issue.Append(id.ToString())
                      .Append(" - ")
                      .Append(telestaff_employees[id].name)
                      .Append(" - Missing AD Group for rank: ")
                      .AppendLine(g);
                  }
                }
                break;

              case "captain":
                foreach (string g in captain_groups)
                {
                  if (!ad_accounts[id].groups.Contains(g))
                  {
                    ad_issue.Append(id.ToString())
                      .Append(" - ")
                      .Append(telestaff_employees[id].name)
                      .Append(" - Missing AD Group for rank: ")
                      .AppendLine(g);
                  }
                }
                break;

              case "battalion chief":
                foreach (string g in bc_groups)
                {
                  if (!ad_accounts[id].groups.Contains(g))
                  {
                    ad_issue.Append(id.ToString())
                      .Append(" - ")
                      .Append(telestaff_employees[id].name)
                      .Append(" - Missing AD Group for rank: ")
                      .AppendLine(g);
                  }
                }
                break;

              default:
                new ErrorLog("Unknown Rank for telestaff employee: " + id.ToString(), telestaff_employees[id].name, "", "", "");
                break;
            }
          }
        }
        catch(Exception ex)
        {
          new ErrorLog(ex);
        }
      }
      return ad_issue.ToString();
    }

    static string PrintTermedPubworksEmployees(Dictionary<int, finplus_employee> employees)
    {
      StringBuilder sb = new StringBuilder();

      foreach(int id in employees.Keys)
      {
        sb.Append(employees[id].id.ToString())
          .Append(" - ")
          .Append(employees[id].name)
          .Append(" - ")
          .Append(employees[id].department)
          .Append(" - ")
          .Append(employees[id].termination_date.ToShortDateString())
          .AppendLine(" - Account still active in Pub works.");          
      }
      return sb.ToString();
    }

    static void EmailPubWorksCheck(string email_message)
    {
      if (email_message.Length == 0) return;
      ErrorLog.SaveEmail(error_email_to,
                         "Active Pub Works Accounts for terminated Employees",
                         email_message);
    }

    static void EmailADCheck(string email_message)
    {
      if (email_message.Length == 0) return;
      ErrorLog.SaveEmail(error_email_to,
                         "AD Account issues found",
                         email_message);
    }
    
    #region  Data Code 

    public static List<T> Get_Data<T>(string query, CS_Type cs)
    {
      try
      {
        using (IDbConnection db = new SqlConnection(GetCS(cs)))
        {
          return (List<T>)db.Query<T>(query);
        }
      }
      catch (Exception ex)
      {
        new ErrorLog(ex, query);
        return null;
      }
    }

    public enum CS_Type
    {
      Log,
      Finplus,
      Telestaff
    }

    public static string GetCS(CS_Type cs)
    {
      return ConfigurationManager.ConnectionStrings[cs.ToString()].ConnectionString;
    }

    #endregion
  }
}
