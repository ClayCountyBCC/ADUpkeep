using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADUpkeep
{
  public class telestaff_employee
  {
    public int id { get; set; }
    public string name { get; set; }
    public string rank { get; set; }


    public static Dictionary<int, telestaff_employee> Get()
    {
      string sql = @"
        SELECT 
          J.Job_Name_Ch rank
          ,RM.RscMaster_Name_Ch name
          ,RM.RscMaster_EmployeeID_Ch id
          --,R.Rsc_From_Da
          --,R.Rsc_Thru_Da  
        FROM Job_Title_Tbl J
        INNER JOIN Resource_Tbl R ON J.Job_No_In = R.job_no_in
        INNER JOIN Resource_Master_Tbl RM ON R.RscMaster_No_In = RM.RscMaster_No_In
        WHERE 
          J.job_disable_si='N'
          AND J.Job_Abrv_Ch IN ('LT', 'BC', 'C')
          AND (R.Rsc_Thru_Da IS NULL OR R.Rsc_Thru_Da >= CAST(GETDATE() AS DATE))
          AND R.Rsc_From_Da IS NOT NULL
          AND RM.RscMaster_Disable_Si='N'
        ORDER BY J.Job_Name_Ch, RM.RscMaster_Name_Ch";
      var employee_list = Program.Get_Data<telestaff_employee>(sql, Program.CS_Type.Telestaff);
      var employee_dict = new Dictionary<int, telestaff_employee>();
      foreach(telestaff_employee e in employee_list)
      {
        employee_dict[e.id] = e;
      }
      return employee_dict;

    }

  }
}
