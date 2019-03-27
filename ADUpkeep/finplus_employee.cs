using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADUpkeep
{
  public class finplus_employee
  {
    public int id { get; set; }
    public string name { get; set; }
    public string department { get; set; }
    public DateTime termination_date { get; set; } = DateTime.MinValue;
    public bool is_terminated
    {
      get
      {
        return termination_date != DateTime.MinValue;
      }
    }

    public finplus_employee() { }

    public static Dictionary<int, finplus_employee> GetEmployees()
    {
      string sql = @"
        USE finplus50;

        SELECT 
          CAST(E.empl_no AS INT) id, 
          LTRIM(RTRIM(E.l_name)) + ' ' + LTRIM(RTRIM(E.f_name)) AS name,   
          D.desc_x AS department, 
          P.term_date termination_date
        FROM [SQLCLUSFINANCE\FINANCE].finplus50.dbo.employee E
        INNER JOIN [SQLCLUSFINANCE\FINANCE].finplus50.dbo.person P ON E.empl_no = P.empl_no
        INNER JOIN [SQLCLUSFINANCE\FINANCE].finplus50.dbo.dept D ON E.home_orgn = D.code";

      var employees = Program.Get_Data<finplus_employee>(sql, Program.CS_Type.Finplus);
      var all_employees = new Dictionary<int, finplus_employee>();
      foreach(finplus_employee e in employees)
      {
        all_employees[e.id] = e;
      }
      return all_employees;
    }

    public static Dictionary<int, finplus_employee> GetTermedEmployees(Dictionary<int, finplus_employee> all_employees)
    {
      Dictionary<int, finplus_employee> termed = new Dictionary<int, finplus_employee>();
      
      foreach(int id in all_employees.Keys)
      {
        if (all_employees[id].is_terminated) termed[id] = all_employees[id];
      }

      return termed;
    }

    public static Dictionary<int, finplus_employee> GetActivePubWorksEmployeesThatAreTermed()
    {
      string sql = @"
        USE PubWorks;

        SELECT
          P.empl_no id
          ,P.term_date termination_date
          ,'Employee Name: ' + LTRIM(RTRIM(E.l_name)) + ' ' + LTRIM(RTRIM(E.f_name)) name 
          ,'Pub Works Name: ' + LTRIM(RTRIM(EPUB.FirstName)) + ' ' +  LTRIM(RTRIM(EPUB.Name)) + ' Pub Works Id: ' + CAST(EPUB.ID AS VARCHAR(5)) department
        FROM [SQLCLUSFINANCE\Finance].finplus50.dbo.person P
        INNER JOIN [SQLCLUSFINANCE\FINANCE].finplus50.dbo.employee E ON P.empl_no = E.empl_no
        INNER JOIN [SQLCLUSFINANCE\FINANCE].finplus50.dbo.dept D ON E.home_orgn = D.code
        LEFT OUTER JOIN Employees EPUB ON EPUB.code = CAST(P.empl_no AS VARCHAR(4)) 
        WHERE EPUB.Active = 1
        AND P.term_date IS NOT NULL";
      var termed_pubworks_employees = Program.Get_Data<finplus_employee>(sql, Program.CS_Type.Telestaff);
      var employees = new Dictionary<int, finplus_employee>();
      foreach (finplus_employee e in termed_pubworks_employees)
      {
        employees[e.id] = e;
      }
      return employees;

    }

  }
}
