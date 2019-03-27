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
