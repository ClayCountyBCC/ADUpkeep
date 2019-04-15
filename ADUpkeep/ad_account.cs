using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Reflection;

namespace ADUpkeep
{
  public class ad_account
  {

    public int id { get; set; } = -1;
    public string name { get; set; } = "";
    public string title { get; set; } = "";
    public bool active { get; set; } = false;
    public string user_name { get; set; } = "";
    public bool found { get; set; } = false;
    public List<string> groups { get; set; } = new List<string>();

    public ad_account() { }

    public static Dictionary<int, ad_account> GetAllEmployees(Dictionary<int, telestaff_employee> telestaff_employees)
    {
      var ad_accounts = new Dictionary<int, ad_account>();

      using (PrincipalContext pc = new PrincipalContext(ContextType.Domain))
      {
        try
        {
          UserPrincipal search = new UserPrincipal(pc)
          {
            Enabled = true
          };
          using (var searcher = new PrincipalSearcher(search))
          {
            var searchers = searcher.FindAll().ToList();
            var filtered = (from s in searchers
                            select s as UserPrincipal).ToList<UserPrincipal>();
            var users = (from f in filtered
                         where f.EmployeeId != null
                         select f).ToList();

            Console.WriteLine("total users found: " + users.Count().ToString());
            foreach (UserPrincipal up in users)
            {
              // We don't want ot look at any AD accounts that 
              // don't have a valid employeeId field.
              // any employeeIds less than 1001 are invalid
              if (up.EmployeeId == null || up.EmployeeId.Length == 0) continue;
              if (!int.TryParse(up.EmployeeId, out int employeeId)) continue;
              if (employeeId < 1000) continue;

              if (ad_accounts.ContainsKey(employeeId)) // duplicate employeeId found
              {
                new ErrorLog("Found duplicate employeeId " + employeeId.ToString(), up.SamAccountName, "", "", "");
                continue;
              }
              var ad = new ad_account();
              ad.found = true;
              ad.user_name = up.SamAccountName.ToLower();
              ad.name = up.DisplayName;
              ad.title = AccountManagmentExtensions.ExtensionGet(up, "title");
              if (ad.title == null) ad.title = "";
              ad.title = ad.title.Trim();
              ad.active = up.Enabled ?? false;
              if (telestaff_employees.ContainsKey(employeeId))
              {
                ad.groups = (from g in up.GetAuthorizationGroups() select g.Name).ToList();
              }              
              ad_accounts[employeeId] = ad;              
            }
          }
        }
        catch (Exception ex)
        {
          new ErrorLog(ex, "");          
        }
      }
      return ad_accounts;
    }

    public static ad_account FindByEmployeeId(int id)
    {
      
      using (PrincipalContext pc = new PrincipalContext(ContextType.Domain))
      {
        try
        {
          UserPrincipal search = new UserPrincipal(pc);
          search.EmployeeId = id.ToString();
          PrincipalSearcher searcher = new PrincipalSearcher(search);
          return ParseUser((UserPrincipal)searcher.FindOne(), id);
        }
        catch (Exception ex)
        {
          new ErrorLog(ex, "");
          return new ad_account();
        }
      }
    }

    private static ad_account ParseUser(UserPrincipal up, int id)
    {
      var ad = new ad_account
      {
        id = id
      };
      try
      {
        if (up != null)
        {
          ad.found = true;
          ad.user_name = up.SamAccountName.ToLower();          
          ad.name = up.DisplayName;
          ad.title = AccountManagmentExtensions.ExtensionGet(up, "title");
          if (ad.title == null) ad.title = "";
          ad.title = ad.title.Trim();
          ad.active = up.Enabled ?? false;
          ad.groups = (from g in up.GetAuthorizationGroups() select g.Name).ToList();
        }
      }
      catch (Exception ex)
      {
        new ErrorLog(ex);
      }
      return ad;
    }

  }

  public static class AccountManagmentExtensions
  {
    public static string ExtensionGet(this UserPrincipal up, string key)
    {
      string value = null;
      MethodInfo mi = up.GetType()
          .GetMethod("ExtensionGet", BindingFlags.NonPublic | BindingFlags.Instance);

      Func<UserPrincipal, string, object[]> extensionGet = (k, v) =>
          ((object[])mi.Invoke(k, new object[] { v }));

      if (extensionGet(up, key).Length > 0)
      {
        value = (string)extensionGet(up, key)[0];
      }

      return value;
    }
  }
}
