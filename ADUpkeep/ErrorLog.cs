using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace ADUpkeep
{
  public class ErrorLog
  {
    public int AppId { get; set; } = Program.app_id;
    public string ApplicationName { get; set; } = "ADUpkeep";
    public string ErrorText { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorStacktrace { get; set; }
    public string ErrorSource { get; set; }
    public string Query { get; set; }

    public ErrorLog(string text,
          string message,
          string stacktrace,
          string source,
          string errorQuery)
    {
      ErrorText = text;
      ErrorMessage = message;
      ErrorStacktrace = stacktrace;
      ErrorSource = source;
      Query = errorQuery;
      SaveLog();
    }

    public ErrorLog(Exception ex, string errorQuery = "")
    {
      ErrorText = ex.ToString();
      ErrorMessage = ex.Message;
      ErrorStacktrace = ex.StackTrace;
      ErrorSource = ex.Source;
      Query = errorQuery;
      SaveLog();
    }

    private void SaveLog()
    {
      string sql = @"
          INSERT INTO ErrorData 
          (applicationName, errorText, errorMessage, 
          errorStacktrace, errorSource, query)  
          VALUES (@applicationName, @errorText, @errorMessage,
            @errorStacktrace, @errorSource, @query);";

      using (IDbConnection db = new SqlConnection(Program.GetCS(Program.CS_Type.Log)))
      {
        db.Execute(sql, this);
      }
    }

    public static void SaveEmail(string to, string subject, string body)
    {
      string sql = @"
          INSERT INTO EmailList 
          (EmailTo, EmailSubject, EmailBody)  
          VALUES (@To, @Subject, @Body);";

      try
      {
        var dbArgs = new Dapper.DynamicParameters();
        dbArgs.Add("@To", to);
        dbArgs.Add("@Subject", subject);
        dbArgs.Add("@Body", body);


        using (IDbConnection db = new SqlConnection(Program.GetCS(Program.CS_Type.Log)))
        {
          db.Execute(sql, dbArgs);
        }
      }
      catch (Exception ex)
      {
        new ErrorLog(ex, sql);
      }
    }
  }
}