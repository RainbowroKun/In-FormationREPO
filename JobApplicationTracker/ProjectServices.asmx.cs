using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace JobApplicationTracker
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbID = "cis440sum26team6";
		private string dbPass = "cis440sum26team6";
		private string dbName = "cis440sum26team6";
		////////////////////////////////////////////////////////////////////////
		
		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
		////////////////////////////////////////////////////////////////////////



		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from test";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}
				[WebMethod(EnableSession = true)]
		public string SubmitAccountRequest(
			string firstName,
			string lastName,
			string email,
			string username,
			string password)
		{
			if (string.IsNullOrWhiteSpace(firstName) ||
				string.IsNullOrWhiteSpace(lastName) ||
				string.IsNullOrWhiteSpace(email) ||
				string.IsNullOrWhiteSpace(username) ||
				string.IsNullOrWhiteSpace(password))
			{
				return "Please complete every field.";
			}

			try
			{
				using (MySqlConnection con = new MySqlConnection(getConString()))
				{
					con.Open();

					string duplicateQuery = @"
						SELECT COUNT(*)
						FROM (
							SELECT email, username FROM users
							UNION ALL
							SELECT email, username
							FROM account_requests
							WHERE status = 'pending'
						) AS existing_accounts
						WHERE email = @email OR username = @username;";

					using (MySqlCommand duplicateCommand =
						new MySqlCommand(duplicateQuery, con))
					{
						duplicateCommand.Parameters.AddWithValue("@email", email.Trim());
						duplicateCommand.Parameters.AddWithValue("@username", username.Trim());

						int duplicateCount =
							Convert.ToInt32(duplicateCommand.ExecuteScalar());

						if (duplicateCount > 0)
						{
							return "That email or username is already being used.";
						}
					}

					string insertQuery = @"
						INSERT INTO account_requests
						(first_name, last_name, email, username, pass, status)
						VALUES
						(@firstName, @lastName, @email, @username, @password, 'pending');";

					using (MySqlCommand insertCommand =
						new MySqlCommand(insertQuery, con))
					{
						insertCommand.Parameters.AddWithValue("@firstName", firstName.Trim());
						insertCommand.Parameters.AddWithValue("@lastName", lastName.Trim());
						insertCommand.Parameters.AddWithValue("@email", email.Trim());
						insertCommand.Parameters.AddWithValue("@username", username.Trim());
						insertCommand.Parameters.AddWithValue("@password", password);

						insertCommand.ExecuteNonQuery();
					}
				}

				return "Account request submitted.";
			}
			catch (Exception e)
			{
				return "Unable to submit the account request. Error: " + e.Message;
			}
		}


		[WebMethod(EnableSession = true)]
		public DataTable GetPendingAccountRequests()
		{
			DataTable requests = new DataTable("AccountRequests");

			using (MySqlConnection con = new MySqlConnection(getConString()))
			{
				string query = @"
					SELECT
						request_id,
						first_name,
						last_name,
						email,
						username,
						status,
						requested_at
					FROM account_requests
					WHERE status = 'pending'
					ORDER BY requested_at ASC;";

				using (MySqlCommand cmd = new MySqlCommand(query, con))
				using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
				{
					adapter.Fill(requests);
				}
			}

			return requests;
		}


		[WebMethod(EnableSession = true)]
		public string ApproveAccountRequest(int requestId)
		{
			try
			{
				using (MySqlConnection con = new MySqlConnection(getConString()))
				{
					con.Open();

					using (MySqlTransaction transaction = con.BeginTransaction())
					{
						string createUserQuery = @"
							INSERT INTO users
							(first_name, last_name, email, username, pass, role, active_status)
							SELECT
								first_name,
								last_name,
								email,
								username,
								pass,
								'user',
								TRUE
							FROM account_requests
							WHERE request_id = @requestId
							  AND status = 'pending';";

						using (MySqlCommand createUserCommand =
							new MySqlCommand(createUserQuery, con, transaction))
						{
							createUserCommand.Parameters.AddWithValue(
								"@requestId", requestId);

							int usersCreated = createUserCommand.ExecuteNonQuery();

							if (usersCreated == 0)
							{
								transaction.Rollback();
								return "The request was not found or was already reviewed.";
							}
						}

						string updateRequestQuery = @"
							UPDATE account_requests
							SET status = 'approved'
							WHERE request_id = @requestId;";

						using (MySqlCommand updateRequestCommand =
							new MySqlCommand(updateRequestQuery, con, transaction))
						{
							updateRequestCommand.Parameters.AddWithValue(
								"@requestId", requestId);

							updateRequestCommand.ExecuteNonQuery();
						}

						transaction.Commit();
					}
				}

				return "Account request approved.";
			}
			catch (Exception e)
			{
				return "Unable to approve the request. Error: " + e.Message;
			}
		}


		[WebMethod(EnableSession = true)]
		public string RejectAccountRequest(int requestId)
		{
			try
			{
				using (MySqlConnection con = new MySqlConnection(getConString()))
				{
					con.Open();

					string query = @"
						UPDATE account_requests
						SET status = 'rejected'
						WHERE request_id = @requestId
						  AND status = 'pending';";

					using (MySqlCommand cmd = new MySqlCommand(query, con))
					{
						cmd.Parameters.AddWithValue("@requestId", requestId);

						int changedRows = cmd.ExecuteNonQuery();

						if (changedRows == 0)
						{
							return "The request was not found or was already reviewed.";
						}
					}
				}

				return "Account request rejected.";
			}
			catch (Exception e)
			{
				return "Unable to reject the request. Error: " + e.Message;
			}
		}

        [WebMethod(EnableSession = true)]
        public string GetCurrentUserRole()
        {
            if (Session["username"] == null)
            {
                return "Not Logged In";
            }

            string firstName = Session["firstName"].ToString();
            string lastName = Session["lastName"].ToString();
            string role = Session["role"].ToString();

            role = char.ToUpper(role[0]) +
				role.Substring(1).ToLower();

            return firstName + " " + lastName + " | " + role;
        }
    }

}

