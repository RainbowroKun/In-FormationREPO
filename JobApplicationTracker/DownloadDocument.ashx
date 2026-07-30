<%@ WebHandler Language="C#" Class="DownloadDocument" %>

using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Web;
using System.Web.SessionState;

public class DownloadDocument : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        if (context.Session["userId"] == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Write("Please log in first.");
            return;
        }

        int documentId;

        if (!int.TryParse(context.Request.QueryString["documentId"], out documentId))
        {
            context.Response.StatusCode = 400;
            context.Response.Write("Invalid document.");
            return;
        }

        int userId = Convert.ToInt32(context.Session["userId"]);

        string connectionString =
            "SERVER=107.180.1.16;" +
            "PORT=3306;" +
            "DATABASE=cis440sum26team6;" +
            "UID=cis440sum26team6;" +
            "PASSWORD=cis440sum26team6;";

        using (MySqlConnection con = new MySqlConnection(connectionString))
        {
            con.Open();

            string query = @"
                SELECT
                    file_name,
                    content_type,
                    file_data
                FROM documents
                WHERE document_id = @documentId
                  AND user_id = @userId;";

            using (MySqlCommand command = new MySqlCommand(query,con))
            {
                command.Parameters.AddWithValue("@documentId", documentId);
                command.Parameters.AddWithValue("@userId", userId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Write(
                            "Document was not found."
                        );

                        return;
                    }

                    string fileName = Convert.ToString(reader["file_name"]);

                    string contentType = Convert.ToString(reader["content_type"]);

                    byte[] fileData = (byte[])reader["file_data"];

                    context.Response.Clear();
                    context.Response.ContentType =
                        string.IsNullOrWhiteSpace(
                            contentType
                        )
                            ? "application/octet-stream"
                            : contentType;

                    context.Response.AddHeader(
                        "Content-Disposition",
                        "attachment; filename=\"" +
                        fileName.Replace("\"", "") +
                        "\""
                    );

                    context.Response.BinaryWrite(
                        fileData
                    );

                    context.Response.End();
                }
            }
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}