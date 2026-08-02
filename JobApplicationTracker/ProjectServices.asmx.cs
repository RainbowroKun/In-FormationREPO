using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Services;

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
        private string getConString()
        {
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
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
                return "Something went wrong, please check your credentials and db name and try again.  Error: " + e.Message;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Account Creation and Requests
        ////////////////////////////////////////////////////////////////////////
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

                    using (MySqlCommand duplicateCommand = new MySqlCommand(duplicateQuery, con))
                    {
                        duplicateCommand.Parameters.AddWithValue("@email", email.Trim());
                        duplicateCommand.Parameters.AddWithValue("@username", username.Trim());

                        int duplicateCount = Convert.ToInt32(duplicateCommand.ExecuteScalar());

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

                    using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, con))
                    {
                        insertCommand.Parameters.AddWithValue("@firstName", firstName.Trim());
                        insertCommand.Parameters.AddWithValue("@lastName", lastName.Trim());
                        insertCommand.Parameters.AddWithValue("@email", email.Trim());
                        insertCommand.Parameters.AddWithValue("@username", username.Trim());
                        insertCommand.Parameters.AddWithValue("@password", password);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                return "Account request has been submitted. We will review the request as soon as we can!";
            }
            catch (Exception e)
            {
                return "Unable to submit the account request. Error: " + e.Message;
            }
        }

        [WebMethod(EnableSession = true)]

        public string LogIn(string username, string password)
        {
            try
            {
                MySqlConnection con = new MySqlConnection(getConString());
                con.Open();

                string sql = "SELECT user_id, first_name, last_name, role FROM users WHERE username = @username AND pass = @password AND active_status = 1";

                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Session["userId"] = reader["user_id"].ToString();
                    Session["username"] = username;
                    Session["firstName"] = reader["first_name"].ToString();
                    Session["lastName"] = reader["last_name"].ToString();
                    Session["role"] = reader["role"].ToString();

                    reader.Close();
                    con.Close();

                    return "Success";
                }

                reader.Close();
                con.Close();

                return "Invalid username or password.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string LogOut()
        {
            Session.Clear();
            Session.Abandon();

            return "Success";
        }

        ////////////////////////////////////////////////////////////////////////
        /// Request Management
        ////////////////////////////////////////////////////////////////////////

    [WebMethod(EnableSession = true)]
    public List<AccountRequestSummary> GetPendingAccountRequests()
    {
    List<AccountRequestSummary> requests =
        new List<AccountRequestSummary>();

    if (Session["userId"] == null ||
        Session["role"] == null ||
        Session["role"].ToString().ToLower() != "admin")
    {
        return requests;
    }

    using (MySqlConnection con =
        new MySqlConnection(getConString()))
    {
        con.Open();

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

        using (MySqlCommand command =
            new MySqlCommand(query, con))
        {
            using (MySqlDataReader reader =
                command.ExecuteReader())
            {
                while (reader.Read())
                {
                    AccountRequestSummary request =
                        new AccountRequestSummary();

                    request.RequestId =
                        Convert.ToInt32(reader["request_id"]);

                    request.FirstName =
                        Convert.ToString(reader["first_name"]);

                    request.LastName =
                        Convert.ToString(reader["last_name"]);

                    request.Email =
                        Convert.ToString(reader["email"]);

                    request.Username =
                        Convert.ToString(reader["username"]);

                    request.Status =
                        Convert.ToString(reader["status"]);

                    request.RequestedAt =
                        Convert.ToDateTime(
                            reader["requested_at"]
                        ).ToString("yyyy-MM-dd HH:mm:ss");

                    requests.Add(request);
                }
            }
        }
    }

    return requests;
}

public class AccountRequestSummary
{
    public int RequestId
    { get; set; }

    public string FirstName
    { get; set; }

    public string LastName
    { get; set; }

    public string Email
    { get; set; }

    public string Username
    { get; set; }

    public string Status
    { get; set; }

    public string RequestedAt
    { get; set; }
}
    public class UserSummary
{
    public int UserId
    {
        get;
        set;
    }

    public string FirstName
    {
        get;
        set;
    }

    public string LastName
    {
        get;
        set;
    }

    public string Email
    {
        get;
        set;
    }

    public string Username
    {
        get;
        set;
    }

    public string Role
    {
        get;
        set;
    }
}
        [WebMethod(EnableSession = true)]
        public string ApproveAccountRequest(int requestId)
        {
            if (Session["userId"] == null ||
         Session["role"] == null ||
         Session["role"].ToString().ToLower() != "admin")
            {
         return "Administrator access is required.";
            }
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
							SELECT first_name, last_name, email, username, pass, 'user', TRUE
							FROM account_requests
							WHERE request_id = @requestId
							  AND status = 'pending';";

                        using (MySqlCommand createUserCommand = new MySqlCommand(createUserQuery, con, transaction))
                        {
                            createUserCommand.Parameters.AddWithValue("@requestId", requestId);
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

                        using (MySqlCommand updateRequestCommand = new MySqlCommand(updateRequestQuery, con, transaction))
                        {
                            updateRequestCommand.Parameters.AddWithValue("@requestId", requestId);

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
            if (Session["userId"] == null ||
            Session["role"] == null ||
             Session["role"].ToString().ToLower() != "admin")
        {
        return "Administrator access is required.";
        }
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
public List<UserSummary> GetUsers()
{
    List<UserSummary> users =
        new List<UserSummary>();

    if (Session["userId"] == null ||
        Session["role"] == null ||
        Session["role"].ToString().ToLower() != "admin")
    {
        return users;
    }

    using (MySqlConnection con =
        new MySqlConnection(getConString()))
    {
        con.Open();

        string query = @"
            SELECT
                user_id,
                first_name,
                last_name,
                email,
                username,
                role
            FROM users
            ORDER BY last_name, first_name;";

        using (MySqlCommand command =
            new MySqlCommand(query, con))
        using (MySqlDataReader reader =
            command.ExecuteReader())
        {
            while (reader.Read())
            {
                UserSummary user =
                    new UserSummary();

                user.UserId =
                    Convert.ToInt32(reader["user_id"]);

                user.FirstName =
                    reader["first_name"].ToString();

                user.LastName =
                    reader["last_name"].ToString();

                user.Email =
                    reader["email"].ToString();

                user.Username =
                    reader["username"].ToString();

                user.Role =
                    reader["role"].ToString();

                users.Add(user);
            }
        }
    }

    return users;
}

[WebMethod(EnableSession = true)]
public string PromoteUser(int userId)
{
    if (Session["userId"] == null ||
        Session["role"] == null ||
        Session["role"].ToString().ToLower() != "admin")
    {
        return "Administrator access is required.";
    }

    try
    {
        using (MySqlConnection con =
            new MySqlConnection(getConString()))
        {
            con.Open();

            string query = @"
                UPDATE users
                SET role = 'admin'
                WHERE user_id = @userId
                  AND role = 'user';";

            using (MySqlCommand command =
                new MySqlCommand(query, con))
            {
                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                int changedRows =
                    command.ExecuteNonQuery();

                if (changedRows == 0)
                {
                    return "The user was not found or is already an administrator.";
                }
            }
        }

        return "User promoted successfully.";
    }
    catch (Exception e)
    {
        return "Unable to promote the user. Error: " +
            e.Message;
    }
}
[WebMethod(EnableSession = true)]
public string DeleteUser(int userId)
{
    if (Session["userId"] == null ||
        Session["role"] == null ||
        Session["role"].ToString().ToLower() != "admin")
    {
        return "Administrator access is required.";
    }

    int currentUserId =
        Convert.ToInt32(Session["userId"]);

    if (currentUserId == userId)
    {
        return "You cannot delete your own account.";
    }

    try
    {
        using (MySqlConnection con =
            new MySqlConnection(getConString()))
        {
            con.Open();

            string query = @"
                DELETE FROM users
                WHERE user_id = @userId;";

            using (MySqlCommand command =
                new MySqlCommand(query, con))
            {
                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                int changedRows =
                    command.ExecuteNonQuery();

                if (changedRows == 0)
                {
                    return "The user account was not found.";
                }
            }
        }

        return "User account deleted successfully.";
    }
    catch (Exception e)
    {
        return "Unable to delete the user. Error: " +
            e.Message;
    }
}

        ////////////////////////////////////////////////////////////////////////
        /// Page Utility
        ////////////////////////////////////////////////////////////////////////

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

            role = char.ToUpper(role[0]) + role.Substring(1).ToLower();

            return firstName + " " + lastName + " | " + role;
        }

        ////////////////////////////////////////////////////////////////////////
        /// Add Application
        ////////////////////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public string AddApplication(
            string companyName,
            string jobTitle,
            string location,
            string dateApplied,
            string applicationStatus,
            string jobPostingUrl,
            string followUpDate,
            string applicationNotes,
            string recruiterFirstName,
            string recruiterLastName,
            string recruiterEmail,
            string recruiterPhone,
            string recruiterFollowUpDate,
            string recruiterLastContactDate,
            string recruiterNotes,
            string resumeFileName,
            string resumeContentType,
            string resumeBase64,
            string resumeNotes,
            string coverLetterFileName,
            string coverLetterContentType,
            string coverLetterBase64,
            string coverLetterNotes)

        //Check if you are logged in
        {
            if (Session["userId"] == null)
            {
                return "You must be logged in.";
            }

            if (string.IsNullOrWhiteSpace(companyName) ||
                string.IsNullOrWhiteSpace(jobTitle) ||
                string.IsNullOrWhiteSpace(dateApplied))
            {
                return "Please complete every required field.";
            }

            //Changing date from string to date format
            DateTime parsedDateApplied;

            if (!DateTime.TryParse(dateApplied, out parsedDateApplied))
            {
                return "The date applied is invalid.";
            }

            //Using the TryGetOptionalDate function (the go to separate table
            object applicationFollowUpValue;
            object recruiterFollowUpValue;
            object recruiterLastContactValue;

            if (!TryGetOptionalDate(followUpDate, out applicationFollowUpValue))
            {
                return "The application follow-up date is invalid.";
            }

            if (!TryGetOptionalDate(recruiterFollowUpDate, out recruiterFollowUpValue))
            {
                return "The recruiter follow-up date is invalid.";
            }

            if (!TryGetOptionalDate(recruiterLastContactDate, out recruiterLastContactValue))
            {
                return "The recruiter last-contact date is invalid.";
            }

            //Check if status is valid
            bool validStatus =
                applicationStatus == "Applied" ||
                applicationStatus == "Interview" ||
                applicationStatus == "Offer" ||
                applicationStatus == "Rejected";

            if (!validStatus)
            {
                return "The application status is invalid.";
            }

            //Check if the recruiter section was completed
            bool hasRecruiterInformation =
                !string.IsNullOrWhiteSpace(recruiterFirstName) ||
                !string.IsNullOrWhiteSpace(recruiterLastName) ||
                !string.IsNullOrWhiteSpace(recruiterEmail) ||
                !string.IsNullOrWhiteSpace(recruiterPhone) ||
                !string.IsNullOrWhiteSpace(recruiterFollowUpDate) ||
                !string.IsNullOrWhiteSpace(recruiterLastContactDate) ||
                !string.IsNullOrWhiteSpace(recruiterNotes);

            if (hasRecruiterInformation &&
                (string.IsNullOrWhiteSpace(recruiterFirstName) ||
                 string.IsNullOrWhiteSpace(recruiterLastName) ||
                 string.IsNullOrWhiteSpace(recruiterEmail)))
            {
                return "Complete the recruiter's first name, " +
                       "last name, and email, or leave the " +
                       "recruiter section blank.";
            }

            //Change uploaded file into byte to store in database
            byte[] resumeData;
            byte[] coverLetterData;

            try
            {
                resumeData = DecodeOptionalFile(resumeFileName, resumeBase64);
                coverLetterData = DecodeOptionalFile(coverLetterFileName, coverLetterBase64);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            // Upload into each table
            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                using (MySqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string applicationSql = @"
                            INSERT INTO applications
                                (user_id, company_name, job_title, location, date_applied, application_status, job_posting_url, follow_up_date, notes)
                            VALUES
                                (@userId, @companyName, @jobTitle, @location, @dateApplied, @applicationStatus, @jobPostingUrl, @followUpDate, @notes);";

                        MySqlCommand applicationCommand = new MySqlCommand(applicationSql, con, transaction);

                        applicationCommand.Parameters.AddWithValue("@userId", userId);
                        applicationCommand.Parameters.AddWithValue("@companyName", companyName.Trim());
                        applicationCommand.Parameters.AddWithValue("@jobTitle", jobTitle.Trim());
                        applicationCommand.Parameters.AddWithValue("@location", EmptyToNull(location));
                        applicationCommand.Parameters.AddWithValue("@dateApplied", parsedDateApplied);
                        applicationCommand.Parameters.AddWithValue("@applicationStatus", applicationStatus);
                        applicationCommand.Parameters.AddWithValue("@jobPostingUrl", EmptyToNull(jobPostingUrl));
                        applicationCommand.Parameters.AddWithValue("@followUpDate", applicationFollowUpValue);
                        applicationCommand.Parameters.AddWithValue("@notes", EmptyToNull(applicationNotes));

                        applicationCommand.ExecuteNonQuery();

                        int applicationId = Convert.ToInt32(applicationCommand.LastInsertedId);

                        if (hasRecruiterInformation)
                        {
                            string recruiterSql = @"
                                INSERT INTO recruiters
                                    (application_id, first_name, last_name, company_name, email, 
                                    phone, follow_up_reminder_date, last_contact_date, notes)
                                VALUES
                                    (@applicationId, @firstName, @lastName, @companyName, @email, 
                                    @phone, @followUpDate, @lastContactDate, @notes);";

                            MySqlCommand recruiterCommand = new MySqlCommand(recruiterSql, con, transaction);

                            recruiterCommand.Parameters.AddWithValue("@applicationId", applicationId);
                            recruiterCommand.Parameters.AddWithValue("@firstName", recruiterFirstName.Trim());
                            recruiterCommand.Parameters.AddWithValue("@lastName", recruiterLastName.Trim());

                            /* Your recruiters table requires company_name. It uses the company entered for the application. */
                            recruiterCommand.Parameters.AddWithValue("@companyName", companyName.Trim());
                            recruiterCommand.Parameters.AddWithValue("@email", recruiterEmail.Trim());
                            recruiterCommand.Parameters.AddWithValue("@phone", EmptyToNull(recruiterPhone));
                            recruiterCommand.Parameters.AddWithValue("@followUpDate", recruiterFollowUpValue);
                            recruiterCommand.Parameters.AddWithValue("@lastContactDate", recruiterLastContactValue);
                            recruiterCommand.Parameters.AddWithValue("@notes", EmptyToNull(recruiterNotes));

                            recruiterCommand.ExecuteNonQuery();
                        }

                        InsertApplicationDocument(con, transaction, userId, applicationId, "Resume", resumeFileName,
                            resumeContentType, resumeData, resumeNotes, null);

                        InsertApplicationDocument(con, transaction, userId, applicationId, "Cover Letter", coverLetterFileName,
                            coverLetterContentType, coverLetterData, coverLetterNotes, null);

                        transaction.Commit();

                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        return "Unable to save the application. Error: " + ex.Message;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Add Application Utility
        ////////////////////////////////////////////////////////////////////////

        //Changing blank sections to NULL (isn't done automatically)
        private object EmptyToNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            return value.Trim();
        }

        //Changing the optional date to null
        private bool TryGetOptionalDate(string value, out object databaseValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                databaseValue = DBNull.Value;
                return true;
            }

            DateTime parsedDate;

            bool validDate = DateTime.TryParse(value, out parsedDate);

            if (!validDate)
            {
                databaseValue = DBNull.Value;
                return false;
            }

            databaseValue = parsedDate;
            return true;
        }

        private byte[] DecodeOptionalFile(string fileName, string base64)
        {
            if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(base64))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(base64))
            {
                throw new Exception("One of the selected files could not be read.");
            }

            byte[] fileData = Convert.FromBase64String(base64);

            int maximumFileSize = 5 * 1024 * 1024;

            if (fileData.Length > maximumFileSize)
            {
                throw new Exception(fileName + " is larger than the 5 MB limit.");
            }

            return fileData;
        }

        private void InsertApplicationDocument(
            MySqlConnection con,
            MySqlTransaction transaction,
            int userId,
            int applicationId,
            string documentType,
            string fileName,
            string contentType,
            byte[] fileData,
            string documentNotes,
            string applicationNotes)
        {
            if (fileData == null)
            {
                return;
            }

            string documentSql = @"
                INSERT INTO documents
                    (user_id, document_type, file_name, content_type, file_size, file_data, notes)
                VALUES
                    (@userId, @documentType, @fileName, @contentType, @fileSize, @fileData, @documentNotes);";

            int documentId;

            using (MySqlCommand documentCommand = new MySqlCommand(documentSql, con, transaction))
            {
                documentCommand.Parameters.AddWithValue("@userId", userId);
                documentCommand.Parameters.AddWithValue("@documentType", documentType);
                documentCommand.Parameters.AddWithValue("@fileName", fileName);
                documentCommand.Parameters.AddWithValue("@contentType", string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream" : contentType);
                documentCommand.Parameters.AddWithValue("@fileSize", fileData.Length);
                documentCommand.Parameters.AddWithValue("@fileData", fileData);
                documentCommand.Parameters.AddWithValue("@documentNotes", EmptyToNull(documentNotes));

                documentCommand.ExecuteNonQuery();

                documentId = Convert.ToInt32(documentCommand.LastInsertedId);
            }

            string linkSql = @"
                INSERT INTO application_documents
                    (application_id, document_id, application_notes)
                VALUES
                    (@applicationId, @documentId, @applicationNotes);";

            using (MySqlCommand linkCommand = new MySqlCommand(linkSql, con, transaction))
            {
                linkCommand.Parameters.AddWithValue("@applicationId", applicationId);
                linkCommand.Parameters.AddWithValue("@documentId", documentId);
                linkCommand.Parameters.AddWithValue("@applicationNotes", EmptyToNull(applicationNotes));

                linkCommand.ExecuteNonQuery();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// View Applications Function
        ////////////////////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public List<ApplicationSummary> GetApplications()
        {
            if (Session["userId"] == null)
            {
                return new List<ApplicationSummary>();
            }

            int userId =
                Convert.ToInt32(
                    Session["userId"]
                );

            List<ApplicationSummary> applications = new List<ApplicationSummary>();

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    SELECT
                        application_id, company_name, job_title, location, date_applied, application_status, follow_up_date, is_archived, updated_at
                    FROM applications
                    WHERE user_id = @userId
                    ORDER BY
                        date_applied DESC,
                        application_id DESC;";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ApplicationSummary application = new ApplicationSummary();

                            application.ApplicationId = Convert.ToInt32(reader["application_id"]);
                            application.CompanyName = Convert.ToString(reader["company_name"]);
                            application.JobTitle = Convert.ToString(reader["job_title"]);
                            application.Location = reader["location"] == DBNull.Value
                                    ? "" : Convert.ToString(reader["location"]);
                            application.DateApplied = Convert.ToDateTime(reader["date_applied"]).ToString("yyyy-MM-dd");
                            application.ApplicationStatus = Convert.ToString(reader["application_status"]);
                            application.FollowUpDate = reader["follow_up_date"] == DBNull.Value
                                    ? "" : Convert.ToDateTime(reader["follow_up_date"]).ToString("yyyy-MM-dd");
                            application.IsArchived = Convert.ToBoolean(reader["is_archived"]);
                            application.UpdatedAt = Convert.ToDateTime(reader["updated_at"]).ToString("yyyy-MM-dd HH:mm:ss");

                            applications.Add(application);
                        }
                    }
                }
            }

            return applications;
        }

        public class ApplicationSummary
        {
            public int ApplicationId
            { get; set; }
            public string CompanyName
            { get; set; }
            public string JobTitle
            { get; set; }
            public string Location
            { get; set; }
            public string DateApplied
            { get; set; }
            public string ApplicationStatus
            { get; set; }
            public string FollowUpDate
            { get; set; }
            public bool IsArchived
            { get; set; }
            public string UpdatedAt
            { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Documents Function
        ////////////////////////////////////////////////////////////////////////

        [WebMethod(EnableSession = true)]
        public ApplicationDocumentsResult GetApplicationDocuments(int applicationId)
        {
            if (Session["userId"] == null)
            {
                return null;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            ApplicationDocumentsResult result = null;

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string applicationSql = @"
                    SELECT
                        application_id, company_name, job_title, location, date_applied, application_status, job_posting_url, follow_up_date, is_archived
                    FROM applications
                    WHERE application_id = @applicationId
                      AND user_id = @userId;";

                using (MySqlCommand applicationCommand = new MySqlCommand(applicationSql, con))
                {
                    applicationCommand.Parameters.AddWithValue("@applicationId", applicationId);
                    applicationCommand.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = applicationCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        result = new ApplicationDocumentsResult();

                        result.ApplicationId = Convert.ToInt32(reader["application_id"]);
                        result.CompanyName = Convert.ToString(reader["company_name"]);
                        result.JobTitle = Convert.ToString(reader["job_title"]);
                        result.Location = reader["location"] == DBNull.Value
                            ? "" : Convert.ToString(reader["location"]);
                        result.DateApplied = Convert.ToDateTime(reader["date_applied"]
                            ).ToString("yyyy-MM-dd");
                        result.ApplicationStatus = Convert.ToString(reader["application_status"]);
                        result.JobPostingUrl = reader["job_posting_url"] == DBNull.Value
                                ? "" : Convert.ToString(reader["job_posting_url"]);
                        result.FollowUpDate = reader["follow_up_date"] == DBNull.Value
                                ? "" : Convert.ToDateTime(reader["follow_up_date"]).ToString("yyyy-MM-dd");
                        result.IsArchived = Convert.ToBoolean(reader["is_archived"]);
                        result.Documents = new List<ApplicationDocumentSummary>();
                    }
                }

                string documentsSql = @"
                    SELECT
                        d.document_id, d.document_type, d.file_name, d.file_size, d.notes, d.uploaded_at
                    FROM documents d
                    INNER JOIN application_documents ad
                        ON d.document_id = ad.document_id
                    WHERE ad.application_id = @applicationId
                      AND d.user_id = @userId
                    ORDER BY d.uploaded_at DESC;";

                using (MySqlCommand documentsCommand = new MySqlCommand(documentsSql, con))
                {
                    documentsCommand.Parameters.AddWithValue("@applicationId", applicationId);
                    documentsCommand.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = documentsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ApplicationDocumentSummary document = new ApplicationDocumentSummary();

                            document.DocumentId = Convert.ToInt32(reader["document_id"]);
                            document.DocumentType = Convert.ToString(reader["document_type"]);
                            document.FileName = Convert.ToString(reader["file_name"]);
                            document.FileSize = Convert.ToInt32(reader["file_size"]);
                            document.Notes = reader["notes"] == DBNull.Value
                                    ? "" : Convert.ToString(reader["notes"]);
                            document.UploadedAt = Convert.ToDateTime(reader["uploaded_at"]).ToString("yyyy-MM-dd HH:mm:ss");

                            result.Documents.Add(
                                document
                            );
                        }
                    }
                }
            }

            return result;
        }

        [WebMethod(EnableSession = true)]
        public string UploadApplicationDocument(
            int applicationId,
            string documentType,
            string fileName,
            string contentType,
            string fileBase64,
            string documentNotes,
            string applicationNotes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0)
            {
                return "A valid application is required.";
            }

            if (string.IsNullOrWhiteSpace(documentType) ||
                string.IsNullOrWhiteSpace(fileName) ||
                string.IsNullOrWhiteSpace(fileBase64))
            {
                return "Please select a document type and file.";
            }

            byte[] documentBytes;

            try
            {
                documentBytes = Convert.FromBase64String(fileBase64);
            }

            catch (FormatException)
            {
                return "The selected file could not be processed.";
            }

            int maximumFileSize = 5 * 1024 * 1024;

            if (documentBytes.Length > maximumFileSize)
            {
                return fileName + " is larger than the 5 MB limit.";
            }

            int userId = Convert.ToInt32(Session["userId"]);


            using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                using (MySqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string ownershipSql = @"
                            SELECT COUNT(*)
                            FROM applications
                            WHERE application_id = @applicationId
                              AND user_id = @userId;";

                        int applicationCount;

                        using (MySqlCommand ownershipCommand = new MySqlCommand(ownershipSql, con, transaction))
                        {
                            ownershipCommand.Parameters.AddWithValue("@applicationId", applicationId);
                            ownershipCommand.Parameters.AddWithValue("@userId", userId);
                            applicationCount = Convert.ToInt32(ownershipCommand.ExecuteScalar());
                        }

                        if (applicationCount == 0)
                        {
                            transaction.Rollback();

                            return "Application was not found.";
                        }

                        string documentSql = @"
                            INSERT INTO documents
                                (user_id, document_type, file_name, content_type, file_size, file_data, notes)
                            VALUES
                                (@userId, @documentType, @fileName, @contentType, @fileSize, @fileData, @documentNotes);";

                        int documentId;

                        using (MySqlCommand documentCommand = new MySqlCommand(documentSql, con, transaction))
                        {
                            documentCommand.Parameters.AddWithValue("@userId", userId);
                            documentCommand.Parameters.AddWithValue("@documentType", documentType.Trim());
                            documentCommand.Parameters.AddWithValue("@fileName", fileName.Trim());
                            documentCommand.Parameters.AddWithValue("@contentType",
                                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim());
                            documentCommand.Parameters.AddWithValue("@fileSize", documentBytes.Length);
                            documentCommand.Parameters.AddWithValue("@fileData", documentBytes);
                            documentCommand.Parameters.AddWithValue("@documentNotes", EmptyToNull(documentNotes));

                            documentCommand.ExecuteNonQuery();

                            documentId = Convert.ToInt32(documentCommand.LastInsertedId);
                        }

                        string linkSql = @"
                            INSERT INTO application_documents
                                (application_id, document_id, application_notes)
                            VALUES
                                (@applicationId, @documentId, @applicationNotes);";

                        using (MySqlCommand linkCommand = new MySqlCommand(linkSql, con, transaction))
                        {
                            linkCommand.Parameters.AddWithValue("@applicationId", applicationId);
                            linkCommand.Parameters.AddWithValue("@documentId", documentId);
                            linkCommand.Parameters.AddWithValue("@applicationNotes", EmptyToNull(applicationNotes));

                            linkCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        return "Unable to upload document. Error: " + ex.Message;
                    }
                }
            }
        }

        public class ApplicationDocumentsResult
        {
            public int ApplicationId
            { get; set; }
            public string CompanyName
            { get; set; }
            public string JobTitle
            { get; set; }
            public string Location
            { get; set; }
            public string DateApplied
            { get; set; }
            public string ApplicationStatus
            { get; set; }
            public string JobPostingUrl
            { get; set; }
            public string FollowUpDate
            { get; set; }
            public bool IsArchived { get; set; }
            public List<ApplicationDocumentSummary> Documents
            { get; set; }
        }

        public class ApplicationDocumentSummary
        {
            public int DocumentId
            { get; set; }
            public string DocumentType
            { get; set; }
            public string FileName
            { get; set; }
            public int FileSize
            { get; set; }
            public string Notes
            { get; set; }
            public string UploadedAt
            { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Add Recruiter Function
        ////////////////////////////////////////////////////////////////////////
        //        [WebMethod(EnableSession = true)]


        //        public string AddRecruiter(
        //    int applicationId,
        //    string firstName,
        //    string lastName,
        //    string companyName,
        //    string email,
        //    string phone,
        //    string followUpReminderDate,
        //    string lastContactDate,
        //    string notes)
        //{
        //    if (applicationId <= 0)
        //    {
        //        return "A valid job application is required.";
        //    }

        //    if (string.IsNullOrWhiteSpace(firstName) ||
        //        string.IsNullOrWhiteSpace(lastName) ||
        //        string.IsNullOrWhiteSpace(companyName) ||
        //        string.IsNullOrWhiteSpace(email))
        //    {
        //        return "Please complete all required fields.";
        //    }

        //    try
        //    {
        //        using (MySqlConnection con = new MySqlConnection(getConString()))
        //        {
        //            con.Open();

        //            string query = @"
        //                INSERT INTO recruiters
        //                (
        //                    application_id,
        //                    first_name,
        //                    last_name,
        //                    company_name,
        //                    email,
        //                    phone,
        //                    follow_up_reminder_date,
        //                    last_contact_date,
        //                    notes
        //                )
        //                VALUES
        //                (
        //                    @applicationId,
        //                    @firstName,
        //                    @lastName,
        //                    @companyName,
        //                    @email,
        //                    @phone,
        //                    @followUpReminderDate,
        //                    @lastContactDate,
        //                    @notes
        //                );";

        //            using (MySqlCommand cmd = new MySqlCommand(query, con))
        //            {
        //                cmd.Parameters.AddWithValue("@applicationId", applicationId);
        //                cmd.Parameters.AddWithValue("@firstName", firstName.Trim());
        //                cmd.Parameters.AddWithValue("@lastName", lastName.Trim());
        //                cmd.Parameters.AddWithValue("@companyName", companyName.Trim());
        //                cmd.Parameters.AddWithValue("@email", email.Trim());

        //                cmd.Parameters.AddWithValue(
        //                    "@phone",
        //                    string.IsNullOrWhiteSpace(phone)
        //                        ? (object)DBNull.Value
        //                        : phone.Trim());

        //                cmd.Parameters.AddWithValue(
        //                    "@followUpReminderDate",
        //                    string.IsNullOrWhiteSpace(followUpReminderDate)
        //                        ? (object)DBNull.Value
        //                        : DateTime.Parse(followUpReminderDate));

        //                cmd.Parameters.AddWithValue(
        //                    "@lastContactDate",
        //                    string.IsNullOrWhiteSpace(lastContactDate)
        //                        ? (object)DBNull.Value
        //                        : DateTime.Parse(lastContactDate));

        //                cmd.Parameters.AddWithValue(
        //                    "@notes",
        //                    string.IsNullOrWhiteSpace(notes)
        //                        ? (object)DBNull.Value
        //                        : notes.Trim());

        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        return "Recruiter information saved successfully.";
        //    }
        //    catch (FormatException)
        //    {
        //        return "One or more dates were entered incorrectly.";
        //    }
        //    catch (Exception e)
        //    {
        //        return "Unable to save recruiter information. Error: " + e.Message;
        //    }
        //}
    }
}

