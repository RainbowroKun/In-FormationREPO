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

        private bool IsAdministrator()
        {
            return
                Session["userId"] != null &&
                Session["role"] != null &&
                Session["role"].ToString().Equals(
                    "admin",
                    StringComparison.OrdinalIgnoreCase);
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
            if (!IsAdministrator())
            {
                throw new HttpException(
                    403,
                    "Administrator access is required.");
            }

            List<AccountRequestSummary> requests =
                new List<AccountRequestSummary>();

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
            public int RequestId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Username { get; set; }
            public string Status { get; set; }
            public string RequestedAt { get; set; }
        }

        [WebMethod(EnableSession = true)]
        public string ApproveAccountRequest(int requestId)
        {
            if (!IsAdministrator())
            {
                return "Administrator access is required.";
            }

            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();

                    using (MySqlTransaction transaction =
                        con.BeginTransaction())
                    {
                        string createUserQuery = @"
                            INSERT INTO users
                            (
                                first_name,
                                last_name,
                                email,
                                username,
                                pass,
                                role,
                                active_status
                            )
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
                            new MySqlCommand(
                                createUserQuery,
                                con,
                                transaction))
                        {
                            createUserCommand.Parameters.AddWithValue(
                                "@requestId",
                                requestId);

                            int usersCreated =
                                createUserCommand.ExecuteNonQuery();

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
                            new MySqlCommand(
                                updateRequestQuery,
                                con,
                                transaction))
                        {
                            updateRequestCommand.Parameters.AddWithValue(
                                "@requestId",
                                requestId);

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
            if (!IsAdministrator())
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
                        UPDATE account_requests
                        SET status = 'rejected'
                        WHERE request_id = @requestId
                            AND status = 'pending';";

                    using (MySqlCommand command =
                        new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue(
                            "@requestId",
                            requestId);

                        int changedRows =
                            command.ExecuteNonQuery();

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
/// Edit Application
////////////////////////////////////////////////////////////////////////

[WebMethod(EnableSession = true)]
public ApplicationEditDetails GetApplicationById(int applicationId)
{
    if (Session["userId"] == null || applicationId <= 0)
    {
        return null;
    }

    int userId = Convert.ToInt32(Session["userId"]);

    using (MySqlConnection con = new MySqlConnection(getConString()))
    {
        con.Open();

        string query = @"
            SELECT
                application_id,
                company_name,
                job_title,
                location,
                date_applied,
                application_status,
                job_posting_url,
                follow_up_date,
                notes
            FROM applications
            WHERE application_id = @applicationId
              AND user_id = @userId
            LIMIT 1;";

        using (MySqlCommand command = new MySqlCommand(query, con))
        {
            command.Parameters.AddWithValue(
                "@applicationId",
                applicationId);

            command.Parameters.AddWithValue(
                "@userId",
                userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                ApplicationEditDetails application =
                    new ApplicationEditDetails();

                application.ApplicationId =
                    Convert.ToInt32(reader["application_id"]);

                application.CompanyName =
                    Convert.ToString(reader["company_name"]);

                application.JobTitle =
                    Convert.ToString(reader["job_title"]);

                application.Location =
                    reader["location"] == DBNull.Value
                        ? ""
                        : Convert.ToString(reader["location"]);

                application.DateApplied =
                    Convert.ToDateTime(reader["date_applied"])
                        .ToString("yyyy-MM-dd");

                application.ApplicationStatus =
                    Convert.ToString(reader["application_status"]);

                application.JobPostingUrl =
                    reader["job_posting_url"] == DBNull.Value
                        ? ""
                        : Convert.ToString(reader["job_posting_url"]);

                application.FollowUpDate =
                    reader["follow_up_date"] == DBNull.Value
                        ? ""
                        : Convert.ToDateTime(reader["follow_up_date"])
                            .ToString("yyyy-MM-dd");

                application.ApplicationNotes =
                    reader["notes"] == DBNull.Value
                        ? ""
                        : Convert.ToString(reader["notes"]);

                return application;
            }
        }
    }
}

[WebMethod(EnableSession = true)]
public string UpdateApplication(
    int applicationId,
    string companyName,
    string jobTitle,
    string location,
    string dateApplied,
    string applicationStatus,
    string jobPostingUrl,
    string followUpDate,
    string applicationNotes)
{
    if (Session["userId"] == null)
    {
        return "You must be logged in.";
    }

    if (applicationId <= 0)
    {
        return "A valid application is required.";
    }

    if (string.IsNullOrWhiteSpace(companyName) ||
        string.IsNullOrWhiteSpace(jobTitle) ||
        string.IsNullOrWhiteSpace(dateApplied))
    {
        return "Please complete every required field.";
    }

    DateTime parsedDateApplied;

    if (!DateTime.TryParse(dateApplied, out parsedDateApplied))
    {
        return "The date applied is invalid.";
    }

    object followUpDateValue;

    if (!TryGetOptionalDate(
        followUpDate,
        out followUpDateValue))
    {
        return "The application follow-up date is invalid.";
    }

    bool validStatus =
        applicationStatus == "Applied" ||
        applicationStatus == "Interview" ||
        applicationStatus == "Offer" ||
        applicationStatus == "Rejected";

    if (!validStatus)
    {
        return "The application status is invalid.";
    }

    int userId =
        Convert.ToInt32(Session["userId"]);

    try
    {
        using (
            MySqlConnection con =
                new MySqlConnection(getConString())
        )
        {
            con.Open();

            string query = @"
                UPDATE applications
                SET
                    company_name = @companyName,
                    job_title = @jobTitle,
                    location = @location,
                    date_applied = @dateApplied,
                    application_status = @applicationStatus,
                    job_posting_url = @jobPostingUrl,
                    follow_up_date = @followUpDate,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE application_id = @applicationId
                  AND user_id = @userId;";

            using (
                MySqlCommand command =
                    new MySqlCommand(query, con)
            )
            {
                command.Parameters.AddWithValue(
                    "@companyName",
                    companyName.Trim());

                command.Parameters.AddWithValue(
                    "@jobTitle",
                    jobTitle.Trim());

                command.Parameters.AddWithValue(
                    "@location",
                    EmptyToNull(location));

                command.Parameters.AddWithValue(
                    "@dateApplied",
                    parsedDateApplied);

                command.Parameters.AddWithValue(
                    "@applicationStatus",
                    applicationStatus);

                command.Parameters.AddWithValue(
                    "@jobPostingUrl",
                    EmptyToNull(jobPostingUrl));

                command.Parameters.AddWithValue(
                    "@followUpDate",
                    followUpDateValue);

                command.Parameters.AddWithValue(
                    "@notes",
                    EmptyToNull(applicationNotes));

                command.Parameters.AddWithValue(
                    "@applicationId",
                    applicationId);

                command.Parameters.AddWithValue(
                    "@userId",
                    userId);

                int rowsUpdated =
                    command.ExecuteNonQuery();

                if (rowsUpdated == 0)
                {
                    return "The application could not be found or does not belong to your account.";
                }
            }
        }

        return "Success";
    }
    catch (Exception ex)
    {
        return "Unable to update the application. Error: " +
            ex.Message;
    }
}

public class ApplicationEditDetails
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

    public string ApplicationNotes
    { get; set; }
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

        [WebMethod(EnableSession = true)]
        public string ToggleApplicationArchive(int applicationId)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0)
            {
                return "A valid application is required.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE applications
                        SET
                            is_archived =
                                CASE
                                    WHEN is_archived = 1 THEN 0
                                    ELSE 1
                                END,
                            updated_at = NOW()
                        WHERE application_id = @applicationId
                            AND user_id = @userId;";

                    using (MySqlCommand command =
                        new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue(
                            "@applicationId",
                            applicationId);

                        command.Parameters.AddWithValue(
                            "@userId",
                            userId);

                        int rowsUpdated =
                            command.ExecuteNonQuery();

                        if (rowsUpdated == 0)
                        {
                            return "Application was not found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to update the application. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string DeleteApplication(int applicationId)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0)
            {
                return "A valid application is required.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con =
                new MySqlConnection(getConString()))
            {
                con.Open();

                using (MySqlTransaction transaction =
                    con.BeginTransaction())
                {
                    try
                    {
                        string ownershipSql = @"
                            SELECT COUNT(*)
                            FROM applications
                            WHERE application_id = @applicationId
                                AND user_id = @userId;";

                        using (MySqlCommand ownershipCommand =
                            new MySqlCommand(
                                ownershipSql,
                                con,
                                transaction))
                        {
                            ownershipCommand.Parameters.AddWithValue(
                                "@applicationId",
                                applicationId);

                            ownershipCommand.Parameters.AddWithValue(
                                "@userId",
                                userId);

                            int applicationCount =
                                Convert.ToInt32(
                                    ownershipCommand.ExecuteScalar());

                            if (applicationCount == 0)
                            {
                                transaction.Rollback();
                                return "Application was not found.";
                            }
                        }


                        string interviewSql = @"
                            DELETE FROM interviews
                            WHERE application_id = @applicationId;";

                        using (MySqlCommand interviewCommand =
                            new MySqlCommand(
                                interviewSql,
                                con,
                                transaction))
                        {
                            interviewCommand.Parameters.AddWithValue(
                                "@applicationId",
                                applicationId);

                            interviewCommand.ExecuteNonQuery();
                        }


                        string documentLinkSql = @"
                            DELETE FROM application_documents
                            WHERE application_id = @applicationId;";

                        using (MySqlCommand documentLinkCommand =
                            new MySqlCommand(
                                documentLinkSql,
                                con,
                                transaction))
                        {
                            documentLinkCommand.Parameters.AddWithValue(
                                "@applicationId",
                                applicationId);

                            documentLinkCommand.ExecuteNonQuery();
                        }


                        string recruiterSql = @"
                            DELETE FROM recruiters
                            WHERE application_id = @applicationId;";

                        using (MySqlCommand recruiterCommand =
                            new MySqlCommand(
                                recruiterSql,
                                con,
                                transaction))
                        {
                            recruiterCommand.Parameters.AddWithValue(
                                "@applicationId",
                                applicationId);

                            recruiterCommand.ExecuteNonQuery();
                        }


                        string applicationSql = @"
                            DELETE FROM applications
                            WHERE application_id = @applicationId
                                AND user_id = @userId;";

                        using (MySqlCommand applicationCommand =
                            new MySqlCommand(
                                applicationSql,
                                con,
                                transaction))
                        {
                            applicationCommand.Parameters.AddWithValue(
                                "@applicationId",
                                applicationId);

                            applicationCommand.Parameters.AddWithValue(
                                "@userId",
                                userId);

                            int rowsDeleted =
                                applicationCommand.ExecuteNonQuery();

                            if (rowsDeleted == 0)
                            {
                                transaction.Rollback();
                                return "Application was not found.";
                            }
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        return "Unable to delete the application. Error: " +
                            ex.Message;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Documents Function
        ////////////////////////////////////////////////////////////////////////
        [WebMethod(EnableSession = true)]
        public string AddDocument(
            string documentType,
            string fileName,
            string contentType,
            string fileBase64,
            string documentNotes,
            int applicationId,
            string applicationNotes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (string.IsNullOrWhiteSpace(documentType) ||
                string.IsNullOrWhiteSpace(fileName) ||
                string.IsNullOrWhiteSpace(fileBase64))
            {
                return "Please select a document type and file.";
            }

            byte[] fileData;

            try
            {
                fileData = Convert.FromBase64String(fileBase64);
            }
            catch (FormatException)
            {
                return "The selected file could not be processed.";
            }

            int maximumFileSize = 5 * 1024 * 1024;

            if (fileData.Length > maximumFileSize)
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
                        if (applicationId > 0)
                        {
                            string ownershipSql = @"
                                SELECT COUNT(*)
                                FROM applications
                                WHERE application_id = @applicationId
                                    AND user_id = @userId;";

                            using (MySqlCommand ownershipCommand =
                                new MySqlCommand(ownershipSql, con, transaction))
                            {
                                ownershipCommand.Parameters.AddWithValue("@applicationId", applicationId);
                                ownershipCommand.Parameters.AddWithValue("@userId", userId);

                                int applicationCount =
                                    Convert.ToInt32(ownershipCommand.ExecuteScalar());

                                if (applicationCount == 0)
                                {
                                    transaction.Rollback();
                                    return "Application was not found.";
                                }
                            }
                        }

                        string documentSql = @"
                            INSERT INTO documents
                            (
                                user_id,
                                document_type,
                                file_name,
                                content_type,
                                file_size,
                                file_data,
                                notes
                            )
                            VALUES
                            (
                                @userId,
                                @documentType,
                                @fileName,
                                @contentType,
                                @fileSize,
                                @fileData,
                                @documentNotes
                            );";

                        int documentId;

                        using (MySqlCommand documentCommand =
                            new MySqlCommand(documentSql, con, transaction))
                        {
                            documentCommand.Parameters.AddWithValue("@userId", userId);
                            documentCommand.Parameters.AddWithValue("@documentType", documentType.Trim());
                            documentCommand.Parameters.AddWithValue("@fileName", fileName.Trim());
                            documentCommand.Parameters.AddWithValue(
                                "@contentType",
                                string.IsNullOrWhiteSpace(contentType)
                                    ? "application/octet-stream"
                                    : contentType.Trim());
                            documentCommand.Parameters.AddWithValue("@fileSize", fileData.Length);
                            documentCommand.Parameters.AddWithValue("@fileData", fileData);
                            documentCommand.Parameters.AddWithValue("@documentNotes", EmptyToNull(documentNotes));

                            documentCommand.ExecuteNonQuery();

                            documentId =
                                Convert.ToInt32(documentCommand.LastInsertedId);
                        }

                        if (applicationId > 0)
                        {
                            string linkSql = @"
                                INSERT INTO application_documents
                                (
                                    application_id,
                                    document_id,
                                    application_notes
                                )
                                VALUES
                                (
                                    @applicationId,
                                    @documentId,
                                    @applicationNotes
                                );";

                            using (MySqlCommand linkCommand =
                                new MySqlCommand(linkSql, con, transaction))
                            {
                                linkCommand.Parameters.AddWithValue("@applicationId", applicationId);
                                linkCommand.Parameters.AddWithValue("@documentId", documentId);
                                linkCommand.Parameters.AddWithValue("@applicationNotes", EmptyToNull(applicationNotes));

                                linkCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return "Unable to add the document. Error: " + ex.Message;
                    }
                }
            }
        }


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
                            documentCommand.Parameters.AddWithValue(
                                "@contentType",
                                string.IsNullOrWhiteSpace(contentType)
                                    ? "application/octet-stream"
                                    : contentType.Trim());
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

        [WebMethod(EnableSession = true)]
        public List<DocumentSummary> GetDocuments()
        {
            List<DocumentSummary> documents = new List<DocumentSummary>();

            if (Session["userId"] == null)
            {
                return documents;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    SELECT
                        d.document_id,
                        d.document_type,
                        d.file_name,
                        d.file_size,
                        d.notes,
                        d.uploaded_at,
                        COUNT(ad.application_id) AS application_count
                    FROM documents d
                    LEFT JOIN application_documents ad
                        ON d.document_id = ad.document_id
                    WHERE d.user_id = @userId
                    GROUP BY
                        d.document_id,
                        d.document_type,
                        d.file_name,
                        d.file_size,
                        d.notes,
                        d.uploaded_at
                    ORDER BY d.uploaded_at DESC, d.document_id DESC;";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DocumentSummary document = new DocumentSummary();
                            document.DocumentId = Convert.ToInt32(reader["document_id"]);
                            document.DocumentType = Convert.ToString(reader["document_type"]);
                            document.FileName = Convert.ToString(reader["file_name"]);
                            document.FileSize = Convert.ToInt32(reader["file_size"]);
                            document.Notes = reader["notes"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["notes"]);
                            document.UploadedAt = Convert.ToDateTime(reader["uploaded_at"])
                                .ToString("yyyy-MM-dd HH:mm:ss");
                            document.ApplicationCount = Convert.ToInt32(reader["application_count"]);
                            documents.Add(document);
                        }
                    }
                }
            }

            return documents;
        }

        [WebMethod(EnableSession = true)]
        public DocumentDetailsResult GetDocumentDetails(int documentId)
        {
            if (Session["userId"] == null || documentId <= 0)
            {
                return null;
            }

            int userId = Convert.ToInt32(Session["userId"]);
            DocumentDetailsResult result = null;

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string documentQuery = @"
                    SELECT
                        document_id,
                        document_type,
                        file_name,
                        content_type,
                        file_size,
                        notes,
                        uploaded_at
                    FROM documents
                    WHERE document_id = @documentId
                      AND user_id = @userId;";

                using (MySqlCommand command = new MySqlCommand(documentQuery, con))
                {
                    command.Parameters.AddWithValue("@documentId", documentId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        result = new DocumentDetailsResult();
                        result.DocumentId = Convert.ToInt32(reader["document_id"]);
                        result.DocumentType = Convert.ToString(reader["document_type"]);
                        result.FileName = Convert.ToString(reader["file_name"]);
                        result.ContentType = Convert.ToString(reader["content_type"]);
                        result.FileSize = Convert.ToInt32(reader["file_size"]);
                        result.Notes = reader["notes"] == DBNull.Value
                            ? ""
                            : Convert.ToString(reader["notes"]);
                        result.UploadedAt = Convert.ToDateTime(reader["uploaded_at"])
                            .ToString("yyyy-MM-dd HH:mm:ss");
                        result.Applications = new List<DocumentApplicationSummary>();
                    }
                }

                string applicationsQuery = @"
                    SELECT
                        a.application_id,
                        a.company_name,
                        a.job_title,
                        a.application_status,
                        a.date_applied,
                        a.is_archived,
                        ad.application_notes,
                        ad.linked_at
                    FROM application_documents ad
                    INNER JOIN applications a
                        ON ad.application_id = a.application_id
                    WHERE ad.document_id = @documentId
                      AND a.user_id = @userId
                    ORDER BY a.date_applied DESC, a.application_id DESC;";

                using (MySqlCommand command = new MySqlCommand(applicationsQuery, con))
                {
                    command.Parameters.AddWithValue("@documentId", documentId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DocumentApplicationSummary application =
                                new DocumentApplicationSummary();

                            application.ApplicationId = Convert.ToInt32(reader["application_id"]);
                            application.CompanyName = Convert.ToString(reader["company_name"]);
                            application.JobTitle = Convert.ToString(reader["job_title"]);
                            application.ApplicationStatus = Convert.ToString(reader["application_status"]);
                            application.DateApplied = Convert.ToDateTime(reader["date_applied"])
                                .ToString("yyyy-MM-dd");
                            application.IsArchived = Convert.ToBoolean(reader["is_archived"]);
                            application.ApplicationNotes = reader["application_notes"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["application_notes"]);
                            application.LinkedAt = Convert.ToDateTime(reader["linked_at"])
                                .ToString("yyyy-MM-dd HH:mm:ss");

                            result.Applications.Add(application);
                        }
                    }
                }
            }

            return result;
        }

        [WebMethod(EnableSession = true)]
        public string LinkApplicationDocument(
            int applicationId,
            int documentId,
            string applicationNotes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0 || documentId <= 0)
            {
                return "A valid application and document are required.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        INSERT INTO application_documents
                            (application_id, document_id, application_notes)
                        SELECT
                            a.application_id,
                            d.document_id,
                            @applicationNotes
                        FROM applications a
                        INNER JOIN documents d
                            ON d.document_id = @documentId
                            AND d.user_id = @userId
                        WHERE a.application_id = @applicationId
                            AND a.user_id = @userId
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM application_documents ad
                                WHERE ad.application_id = @applicationId
                                    AND ad.document_id = @documentId
                            );";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@applicationId", applicationId);
                        command.Parameters.AddWithValue("@documentId", documentId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@applicationNotes", EmptyToNull(applicationNotes));

                        int rowsAdded = command.ExecuteNonQuery();

                        if (rowsAdded == 0)
                        {
                            return "The application could not be linked to this document.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to link the application. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string UnlinkApplicationDocument(
            int applicationId,
            int documentId)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0 || documentId <= 0)
            {
                return "A valid application and document are required.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        DELETE ad
                        FROM application_documents ad
                        INNER JOIN applications a
                            ON ad.application_id = a.application_id
                        INNER JOIN documents d
                            ON ad.document_id = d.document_id
                        WHERE ad.application_id = @applicationId
                            AND ad.document_id = @documentId
                            AND a.user_id = @userId
                            AND d.user_id = @userId;";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@applicationId", applicationId);
                        command.Parameters.AddWithValue("@documentId", documentId);
                        command.Parameters.AddWithValue("@userId", userId);

                        int rowsDeleted = command.ExecuteNonQuery();

                        if (rowsDeleted == 0)
                        {
                            return "The application link could not be found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to unlink the application. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string DeleteDocument(int documentId)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (documentId <= 0)
            {
                return "A valid document is required.";
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
                            FROM documents
                            WHERE document_id = @documentId
                                AND user_id = @userId;";

                        int documentCount;

                        using (MySqlCommand ownershipCommand =
                            new MySqlCommand(ownershipSql, con, transaction))
                        {
                            ownershipCommand.Parameters.AddWithValue("@documentId", documentId);
                            ownershipCommand.Parameters.AddWithValue("@userId", userId);

                            documentCount =
                                Convert.ToInt32(ownershipCommand.ExecuteScalar());
                        }

                        if (documentCount == 0)
                        {
                            transaction.Rollback();
                            return "Document was not found.";
                        }

                        string linkSql = @"
                            DELETE FROM application_documents
                            WHERE document_id = @documentId;";

                        using (MySqlCommand linkCommand =
                            new MySqlCommand(linkSql, con, transaction))
                        {
                            linkCommand.Parameters.AddWithValue("@documentId", documentId);
                            linkCommand.ExecuteNonQuery();
                        }

                        string documentSql = @"
                            DELETE FROM documents
                            WHERE document_id = @documentId
                                AND user_id = @userId;";

                        using (MySqlCommand documentCommand =
                            new MySqlCommand(documentSql, con, transaction))
                        {
                            documentCommand.Parameters.AddWithValue("@documentId", documentId);
                            documentCommand.Parameters.AddWithValue("@userId", userId);

                            if (documentCommand.ExecuteNonQuery() == 0)
                            {
                                transaction.Rollback();
                                return "Document was not found.";
                            }
                        }

                        transaction.Commit();
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return "Unable to delete the document. Error: " + ex.Message;
                    }
                }
            }
        }
        ////////////////////////////////////////////////////////////////////////
        /// Interview Functions
        ////////////////////////////////////////////////////////////////////////

        [WebMethod(EnableSession = true)]
        public string AddInterview(
            int applicationId,
            int? recruiterId,
            string interviewTitle,
            string interviewType,
            string interviewDate,
            string interviewerName,
            string locationOrLink,
            string notes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0)
            {
                return "A valid job application is required.";
            }

            if (string.IsNullOrWhiteSpace(interviewTitle) ||
                string.IsNullOrWhiteSpace(interviewType) ||
                string.IsNullOrWhiteSpace(interviewDate))
            {
                return "Please enter the interview title, type, date, and time.";
            }

            if (interviewTitle.Trim().Length > 100 ||
                interviewerName != null && interviewerName.Trim().Length > 100 ||
                locationOrLink != null && locationOrLink.Trim().Length > 500 ||
                notes != null && notes.Trim().Length > 2000)
            {
                return "One or more interview fields are too long.";
            }

            if (interviewType != "Phone" &&
                interviewType != "Video" &&
                interviewType != "Onsite" &&
                interviewType != "Other")
            {
                return "The selected interview type is invalid.";
            }

            DateTime parsedInterviewDate;

            if (!DateTime.TryParse(interviewDate, out parsedInterviewDate))
            {
                return "The interview date or time is invalid.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            object recruiterValue = recruiterId.HasValue && recruiterId.Value > 0
                ? (object)recruiterId.Value
                : DBNull.Value;

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        INSERT INTO interviews
                            (application_id, recruiter_id, interview_title,
                            interview_type, interview_date, interviewer_name,
                            location_or_link, notes, interview_status)
                        SELECT
                            a.application_id,
                            @recruiterId,
                            @interviewTitle,
                            @interviewType,
                            @interviewDate,
                            @interviewerName,
                            @locationOrLink,
                            @notes,
                            'Scheduled'
                        FROM applications a
                        WHERE a.application_id = @applicationId
                            AND a.user_id = @userId
                            AND (
                                @recruiterId IS NULL
                                OR EXISTS (
                                    SELECT 1
                                    FROM recruiters r
                                    WHERE r.recruiter_id = @recruiterId
                                        AND r.application_id = a.application_id
                                )
                            );";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@applicationId", applicationId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@recruiterId", recruiterValue);
                        command.Parameters.AddWithValue("@interviewTitle", interviewTitle.Trim());
                        command.Parameters.AddWithValue("@interviewType", interviewType);
                        command.Parameters.AddWithValue("@interviewDate", parsedInterviewDate);
                        command.Parameters.AddWithValue("@interviewerName", EmptyToNull(interviewerName));
                        command.Parameters.AddWithValue("@locationOrLink", EmptyToNull(locationOrLink));
                        command.Parameters.AddWithValue("@notes", EmptyToNull(notes));

                        int rowsAdded = command.ExecuteNonQuery();

                        if (rowsAdded == 0)
                        {
                            return "The selected application or recruiter could not be found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to save the interview. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public InterviewSummary GetInterview(int interviewId)
        {
            if (Session["userId"] == null || interviewId <= 0)
            {
                return null;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    SELECT
                        i.interview_id,
                        i.application_id,
                        i.recruiter_id,
                        i.interview_title,
                        i.interview_type,
                        i.interview_date,
                        i.interviewer_name,
                        i.location_or_link,
                        i.notes,
                        i.interview_status,
                        i.interview_status,
                        TRIM(CONCAT_WS(' ', r.first_name, r.last_name)) AS recruiter_name,
                        a.company_name,
                        a.job_title
                    FROM interviews i
                    INNER JOIN applications a
                        ON i.application_id = a.application_id
                    LEFT JOIN recruiters r
                        ON i.recruiter_id = r.recruiter_id
                        AND r.application_id = i.application_id
                    WHERE i.interview_id = @interviewId
                        AND a.user_id = @userId
                    LIMIT 1;";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@interviewId", interviewId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            InterviewSummary interview = new InterviewSummary();

                            interview.InterviewId = Convert.ToInt32(reader["interview_id"]);
                            interview.ApplicationId = Convert.ToInt32(reader["application_id"]);
                            interview.RecruiterId = reader["recruiter_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["recruiter_id"]);
                            interview.InterviewTitle = Convert.ToString(reader["interview_title"]);
                            interview.InterviewType = Convert.ToString(reader["interview_type"]);
                            interview.InterviewDate = Convert.ToDateTime(reader["interview_date"]).ToString("yyyy-MM-dd HH:mm:ss");
                            interview.InterviewerName = reader["interviewer_name"] == DBNull.Value ? "" : Convert.ToString(reader["interviewer_name"]);
                            interview.LocationOrLink = reader["location_or_link"] == DBNull.Value ? "" : Convert.ToString(reader["location_or_link"]);
                            interview.Notes = reader["notes"] == DBNull.Value ? "" : Convert.ToString(reader["notes"]);
                            interview.InterviewStatus = Convert.ToString(reader["interview_status"]);
                            interview.RecruiterName = reader["recruiter_name"] == DBNull.Value ? "" : Convert.ToString(reader["recruiter_name"]);
                            interview.CompanyName = Convert.ToString(reader["company_name"]);
                            interview.ApplicationName = Convert.ToString(reader["job_title"]);

                            return interview;
                        }
                    }
                }
            }

            return null;
        }

        [WebMethod(EnableSession = true)]
        public string UpdateInterview(
            int interviewId,
            int applicationId,
            int? recruiterId,
            string interviewTitle,
            string interviewType,
            string interviewDate,
            string interviewerName,
            string locationOrLink,
            string notes,
            string interviewStatus)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (interviewId <= 0 || applicationId <= 0)
            {
                return "A valid interview and job application are required.";
            }

            if (string.IsNullOrWhiteSpace(interviewTitle) ||
                string.IsNullOrWhiteSpace(interviewType) ||
                string.IsNullOrWhiteSpace(interviewDate) ||
                string.IsNullOrWhiteSpace(interviewStatus))
            {
                return "Please enter the interview title, type, date, time, and status.";
            }

            if (interviewTitle.Trim().Length > 100 ||
                interviewerName != null && interviewerName.Trim().Length > 100 ||
                locationOrLink != null && locationOrLink.Trim().Length > 500 ||
                notes != null && notes.Trim().Length > 2000)
            {
                return "One or more interview fields are too long.";
            }

            if (interviewType != "Phone" &&
                interviewType != "Video" &&
                interviewType != "Onsite" &&
                interviewType != "Other")
            {
                return "The selected interview type is invalid.";
            }

            if (interviewStatus != "Scheduled" &&
                interviewStatus != "Completed" &&
                interviewStatus != "Cancelled")
            {
                return "The selected interview status is invalid.";
            }

            DateTime parsedInterviewDate;

            if (!DateTime.TryParse(interviewDate, out parsedInterviewDate))
            {
                return "The interview date or time is invalid.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            object recruiterValue = recruiterId.HasValue && recruiterId.Value > 0
                ? (object)recruiterId.Value
                : DBNull.Value;

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE interviews i
                        INNER JOIN applications currentApplication
                            ON i.application_id = currentApplication.application_id
                        SET
                            i.application_id = @applicationId,
                            i.recruiter_id = @recruiterId,
                            i.interview_title = @interviewTitle,
                            i.interview_type = @interviewType,
                            i.interview_date = @interviewDate,
                            i.interviewer_name = @interviewerName,
                            i.location_or_link = @locationOrLink,
                            i.notes = @notes,
                            i.interview_status = @interviewStatus
                        WHERE i.interview_id = @interviewId
                            AND currentApplication.user_id = @userId
                            AND EXISTS (
                                SELECT 1
                                FROM applications selectedApplication
                                WHERE selectedApplication.application_id = @applicationId
                                    AND selectedApplication.user_id = @userId
                            )
                            AND (
                                @recruiterId IS NULL
                                OR EXISTS (
                                    SELECT 1
                                    FROM recruiters r
                                    WHERE r.recruiter_id = @recruiterId
                                        AND r.application_id = @applicationId
                                )
                            );";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@interviewId", interviewId);
                        command.Parameters.AddWithValue("@applicationId", applicationId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@recruiterId", recruiterValue);
                        command.Parameters.AddWithValue("@interviewTitle", interviewTitle.Trim());
                        command.Parameters.AddWithValue("@interviewType", interviewType);
                        command.Parameters.AddWithValue("@interviewDate", parsedInterviewDate);
                        command.Parameters.AddWithValue("@interviewerName", EmptyToNull(interviewerName));
                        command.Parameters.AddWithValue("@locationOrLink", EmptyToNull(locationOrLink));
                        command.Parameters.AddWithValue("@notes", EmptyToNull(notes));
                        command.Parameters.AddWithValue("@interviewStatus", interviewStatus);

                        int rowsUpdated = command.ExecuteNonQuery();

                        if (rowsUpdated == 0)
                        {
                            return "The interview, application, or recruiter could not be found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to update the interview. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public List<InterviewSummary> GetApplicationInterviews(int applicationId)
        {
            List<InterviewSummary> interviews = new List<InterviewSummary>();

            if (Session["userId"] == null || applicationId <= 0)
            {
                return interviews;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    SELECT
                        i.interview_id,
                        i.application_id,
                        i.recruiter_id,
                        i.interview_title,
                        i.interview_type,
                        i.interview_date,
                        i.interviewer_name,
                        i.location_or_link,
                        i.notes,
                        i.interview_status,
                        TRIM(CONCAT_WS(' ', r.first_name, r.last_name)) AS recruiter_name
                    FROM interviews i
                    INNER JOIN applications a
                        ON i.application_id = a.application_id
                    LEFT JOIN recruiters r
                        ON i.recruiter_id = r.recruiter_id
                        AND r.application_id = i.application_id
                    WHERE i.application_id = @applicationId
                        AND a.user_id = @userId
                    ORDER BY i.interview_date ASC, i.interview_id ASC;";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@applicationId", applicationId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            InterviewSummary interview = new InterviewSummary();

                            interview.InterviewId = Convert.ToInt32(reader["interview_id"]);
                            interview.ApplicationId = Convert.ToInt32(reader["application_id"]);
                            interview.RecruiterId = reader["recruiter_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["recruiter_id"]);
                            interview.InterviewTitle = Convert.ToString(reader["interview_title"]);
                            interview.InterviewType = Convert.ToString(reader["interview_type"]);
                            interview.InterviewDate = Convert.ToDateTime(reader["interview_date"]).ToString("yyyy-MM-dd HH:mm:ss");
                            interview.InterviewerName = reader["interviewer_name"] == DBNull.Value ? "" : Convert.ToString(reader["interviewer_name"]);
                            interview.LocationOrLink = reader["location_or_link"] == DBNull.Value ? "" : Convert.ToString(reader["location_or_link"]);
                            interview.Notes = reader["notes"] == DBNull.Value ? "" : Convert.ToString(reader["notes"]);
                            interview.InterviewStatus = Convert.ToString(reader["interview_status"]);
                            interview.RecruiterName = reader["recruiter_name"] == DBNull.Value ? "" : Convert.ToString(reader["recruiter_name"]);

                            interviews.Add(interview);
                        }
                    }
                }
            }

            return interviews;
        }

        [WebMethod(EnableSession = true)]
        public bool DeleteInterview(int interviewId)
        {
            if (Session["userId"] == null || interviewId <= 0)
            {
                return false;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    DELETE FROM interviews
                    WHERE interview_id = @interviewId
                    AND application_id IN
                    (
                        SELECT application_id
                        FROM applications
                        WHERE user_id = @userId
                    );";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@interviewId", interviewId);
                    command.Parameters.AddWithValue("@userId", userId);

                    int deletedRows = command.ExecuteNonQuery();

                    return deletedRows > 0;
                }
            }
        }
        ////////////////////////////////////////////////////////////////////////
        /// Recruiter Functions
        ////////////////////////////////////////////////////////////////////////

        [WebMethod(EnableSession = true)]
        public string AddRecruiter(
            int applicationId,
            string firstName,
            string lastName,
            string email,
            string phone,
            string followUpReminderDate,
            string lastContactDate,
            string notes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (applicationId <= 0)
            {
                return "A valid job application is required.";
            }

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                return "Please enter the recruiter's first name, last name, and email.";
            }

            if (firstName.Trim().Length > 50 ||
                lastName.Trim().Length > 50 ||
                email.Trim().Length > 100 ||
                (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 25))
            {
                return "One or more recruiter fields are too long.";
            }

            object followUpReminderValue;
            object lastContactValue;

            if (!TryGetOptionalDate(followUpReminderDate, out followUpReminderValue))
            {
                return "The follow-up reminder date is invalid.";
            }

            if (!TryGetOptionalDate(lastContactDate, out lastContactValue))
            {
                return "The last-contact date is invalid.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        INSERT INTO recruiters
                            (application_id, first_name, last_name, company_name, email,
                            phone, follow_up_reminder_date, last_contact_date, notes)
                        SELECT
                            a.application_id,
                            @firstName,
                            @lastName,
                            a.company_name,
                            @email,
                            @phone,
                            @followUpReminderDate,
                            @lastContactDate,
                            @notes
                        FROM applications a
                        WHERE a.application_id = @applicationId
                            AND a.user_id = @userId;";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@applicationId", applicationId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@firstName", firstName.Trim());
                        command.Parameters.AddWithValue("@lastName", lastName.Trim());
                        command.Parameters.AddWithValue("@email", email.Trim());
                        command.Parameters.AddWithValue("@phone", EmptyToNull(phone));
                        command.Parameters.AddWithValue("@followUpReminderDate", followUpReminderValue);
                        command.Parameters.AddWithValue("@lastContactDate", lastContactValue);
                        command.Parameters.AddWithValue("@notes", EmptyToNull(notes));

                        int rowsAdded = command.ExecuteNonQuery();

                        if (rowsAdded == 0)
                        {
                            return "The selected application could not be found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to save recruiter information. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public List<RecruiterSummary> GetRecruiters()
        {
            List<RecruiterSummary> recruiters =
                new List<RecruiterSummary>();

            if (Session["userId"] == null)
            {
                return recruiters;
            }

            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        SELECT
                            r.recruiter_id,
                            r.application_id,
                            r.first_name,
                            r.last_name,
                            r.company_name,
                            r.email,
                            r.phone,
                            r.follow_up_reminder_date,
                            r.last_contact_date,
                            r.notes,
                            a.job_title
                        FROM recruiters r
                        INNER JOIN applications a
                            ON r.application_id = a.application_id
                        INNER JOIN users u
                            ON a.user_id = u.user_id
                        WHERE u.username = @username
                        ORDER BY r.updated_at DESC;";

                    using (MySqlCommand cmd =
                        new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@username",
                            Session["username"].ToString()
                        );

                        using (MySqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RecruiterSummary recruiter =
                                    new RecruiterSummary();

                                recruiter.RecruiterId =
                                    Convert.ToInt32(
                                        reader["recruiter_id"]
                                    );

                                recruiter.ApplicationId =
                                    Convert.ToInt32(
                                        reader["application_id"]
                                    );

                                recruiter.FirstName =
                                    reader["first_name"].ToString();

                                recruiter.LastName =
                                    reader["last_name"].ToString();

                                recruiter.CompanyName =
                                    reader["company_name"].ToString();

                                recruiter.Email =
                                    reader["email"].ToString();

                                recruiter.Phone =
                                    reader["phone"] == DBNull.Value
                                        ? ""
                                        : reader["phone"].ToString();

                                recruiter.JobTitle =
                                    reader["job_title"].ToString();

                                recruiter.FollowUpReminderDate =
                                    reader["follow_up_reminder_date"]
                                        == DBNull.Value
                                        ? ""
                                        : Convert.ToDateTime(
                                            reader[
                                                "follow_up_reminder_date"
                                            ]
                                        ).ToString("yyyy-MM-dd");

                                recruiter.LastContactDate =
                                    reader["last_contact_date"]
                                        == DBNull.Value
                                        ? ""
                                        : Convert.ToDateTime(
                                            reader["last_contact_date"]
                                        ).ToString("yyyy-MM-dd");

                                recruiter.Notes =
                                    reader["notes"] == DBNull.Value
                                        ? ""
                                        : reader["notes"].ToString();

                                recruiters.Add(recruiter);
                            }
                        }
                    }
                }
            }
            catch
            {
                return new List<RecruiterSummary>();
            }

            return recruiters;
        }

        [WebMethod(EnableSession = true)]
        public RecruiterSummary GetRecruiter(int recruiterId)
        {
            if (Session["userId"] == null || recruiterId <= 0)
            {
                return null;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                string query = @"
                    SELECT
                        r.recruiter_id,
                        r.application_id,
                        r.first_name,
                        r.last_name,
                        r.company_name,
                        r.email,
                        r.phone,
                        r.follow_up_reminder_date,
                        r.last_contact_date,
                        r.notes,
                        a.job_title
                    FROM recruiters r
                    INNER JOIN applications a
                        ON r.application_id = a.application_id
                    WHERE r.recruiter_id = @recruiterId
                        AND a.user_id = @userId
                    LIMIT 1;";

                using (MySqlCommand command = new MySqlCommand(query, con))
                {
                    command.Parameters.AddWithValue("@recruiterId", recruiterId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            RecruiterSummary recruiter = new RecruiterSummary();

                            recruiter.RecruiterId = Convert.ToInt32(reader["recruiter_id"]);
                            recruiter.ApplicationId = Convert.ToInt32(reader["application_id"]);
                            recruiter.FirstName = Convert.ToString(reader["first_name"]);
                            recruiter.LastName = Convert.ToString(reader["last_name"]);
                            recruiter.CompanyName = Convert.ToString(reader["company_name"]);
                            recruiter.Email = Convert.ToString(reader["email"]);
                            recruiter.Phone = reader["phone"] == DBNull.Value ? "" : Convert.ToString(reader["phone"]);
                            recruiter.JobTitle = Convert.ToString(reader["job_title"]);
                            recruiter.FollowUpReminderDate = reader["follow_up_reminder_date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["follow_up_reminder_date"]).ToString("yyyy-MM-dd");
                            recruiter.LastContactDate = reader["last_contact_date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["last_contact_date"]).ToString("yyyy-MM-dd");
                            recruiter.Notes = reader["notes"] == DBNull.Value ? "" : Convert.ToString(reader["notes"]);

                            return recruiter;
                        }
                    }
                }
            }

            return null;
        }

        [WebMethod(EnableSession = true)]
        public string UpdateRecruiter(
            int recruiterId,
            string firstName,
            string lastName,
            string email,
            string phone,
            string followUpReminderDate,
            string lastContactDate,
            string notes)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (recruiterId <= 0)
            {
                return "A valid recruiter is required.";
            }

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                return "Please enter the recruiter's first name, last name, and email.";
            }

            if (firstName.Trim().Length > 50 ||
                lastName.Trim().Length > 50 ||
                email.Trim().Length > 100 ||
                (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 25))
            {
                return "One or more recruiter fields are too long.";
            }

            object followUpReminderValue;
            object lastContactValue;

            if (!TryGetOptionalDate(followUpReminderDate, out followUpReminderValue))
            {
                return "The follow-up reminder date is invalid.";
            }

            if (!TryGetOptionalDate(lastContactDate, out lastContactValue))
            {
                return "The last-contact date is invalid.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        UPDATE recruiters r
                        INNER JOIN applications a
                            ON r.application_id = a.application_id
                        SET
                            r.first_name = @firstName,
                            r.last_name = @lastName,
                            r.email = @email,
                            r.phone = @phone,
                            r.follow_up_reminder_date = @followUpReminderDate,
                            r.last_contact_date = @lastContactDate,
                            r.notes = @notes
                        WHERE r.recruiter_id = @recruiterId
                            AND a.user_id = @userId;";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@recruiterId", recruiterId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@firstName", firstName.Trim());
                        command.Parameters.AddWithValue("@lastName", lastName.Trim());
                        command.Parameters.AddWithValue("@email", email.Trim());
                        command.Parameters.AddWithValue("@phone", EmptyToNull(phone));
                        command.Parameters.AddWithValue("@followUpReminderDate", followUpReminderValue);
                        command.Parameters.AddWithValue("@lastContactDate", lastContactValue);
                        command.Parameters.AddWithValue("@notes", EmptyToNull(notes));

                        int rowsUpdated = command.ExecuteNonQuery();

                        if (rowsUpdated == 0)
                        {
                            return "The recruiter could not be found.";
                        }
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Unable to update recruiter information. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public bool DeleteRecruiter(int recruiterId)
        {
            if (Session["userId"] == null || recruiterId <= 0)
            {
                return false;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            using (MySqlConnection con = new MySqlConnection(getConString()))
            {
                con.Open();

                using (MySqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string unlinkInterviewsQuery = @"
                            UPDATE interviews i
                            INNER JOIN applications a
                                ON i.application_id = a.application_id
                            SET i.recruiter_id = NULL
                            WHERE i.recruiter_id = @recruiterId
                                AND a.user_id = @userId;";

                        using (MySqlCommand command = new MySqlCommand(unlinkInterviewsQuery, con, transaction))
                        {
                            command.Parameters.AddWithValue("@recruiterId", recruiterId);
                            command.Parameters.AddWithValue("@userId", userId);

                            command.ExecuteNonQuery();
                        }

                        string deleteRecruiterQuery = @"
                            DELETE r
                            FROM recruiters r
                            INNER JOIN applications a
                                ON r.application_id = a.application_id
                            WHERE r.recruiter_id = @recruiterId
                                AND a.user_id = @userId;";

                        int deletedRows;

                        using (MySqlCommand command = new MySqlCommand(deleteRecruiterQuery, con, transaction))
                        {
                            command.Parameters.AddWithValue("@recruiterId", recruiterId);
                            command.Parameters.AddWithValue("@userId", userId);

                            deletedRows = command.ExecuteNonQuery();
                        }

                        if (deletedRows == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        transaction.Commit();

                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        [WebMethod(EnableSession = true)]
        public string SetFollowUpDate(string linkType, int linkedRecordId, string followUpDate)
        {
            if (Session["userId"] == null)
            {
                return "Please log in first.";
            }

            if (linkedRecordId <= 0)
            {
                return "A valid record must be selected.";
            }

            if (linkType != "Application" && linkType != "Recruiter")
            {
                return "The selected follow-up type is invalid.";
            }

            DateTime parsedFollowUpDate;

            if (!DateTime.TryParse(followUpDate, out parsedFollowUpDate))
            {
                return "The follow-up date is invalid.";
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query;

                    if (linkType == "Recruiter")
                    {
                        query = @"
                            UPDATE recruiters r
                            INNER JOIN applications a
                                ON r.application_id = a.application_id
                            SET r.follow_up_reminder_date = @followUpDate
                            WHERE r.recruiter_id = @linkedRecordId
                                AND a.user_id = @userId;";
                    }
                    else
                    {
                        query = @"
                            UPDATE applications
                            SET follow_up_date = @followUpDate
                            WHERE application_id = @linkedRecordId
                                AND user_id = @userId;";
                    }

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@followUpDate", parsedFollowUpDate.Date);
                        command.Parameters.AddWithValue("@linkedRecordId", linkedRecordId);
                        command.Parameters.AddWithValue("@userId", userId);

                        int rowsUpdated = command.ExecuteNonQuery();

                        if (rowsUpdated == 0)
                        {
                            return "The selected record could not be found.";
                        }
                    }
                }

                return "Follow-up saved successfully.";
            }
            catch (Exception ex)
            {
                return "Unable to save the follow-up. Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public List<FollowUpReminderSummary> GetFollowUpReminders()
        {
            List<FollowUpReminderSummary> followUps = new List<FollowUpReminderSummary>();

            if (Session["userId"] == null)
            {
                return followUps;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        SELECT
                            'Application' AS follow_up_type,
                            a.application_id AS related_id,
                            a.job_title AS follow_up_name,
                            a.application_id,
                            a.company_name,
                            a.job_title AS application_name,
                            a.follow_up_date
                        FROM applications a
                        WHERE a.user_id = @userId
                            AND a.follow_up_date IS NOT NULL

                        UNION ALL

                        SELECT
                            'Recruiter' AS follow_up_type,
                            r.recruiter_id AS related_id,
                            TRIM(CONCAT_WS(' ', r.first_name, r.last_name)) AS follow_up_name,
                            a.application_id,
                            a.company_name,
                            a.job_title AS application_name,
                            r.follow_up_reminder_date AS follow_up_date
                        FROM recruiters r
                        INNER JOIN applications a
                            ON r.application_id = a.application_id
                        WHERE a.user_id = @userId
                            AND r.follow_up_reminder_date IS NOT NULL

                        ORDER BY follow_up_date ASC, follow_up_type ASC;";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FollowUpReminderSummary followUp = new FollowUpReminderSummary();

                                followUp.FollowUpType = Convert.ToString(reader["follow_up_type"]);
                                followUp.RelatedId = Convert.ToInt32(reader["related_id"]);
                                followUp.FollowUpName = Convert.ToString(reader["follow_up_name"]);
                                followUp.ApplicationId = Convert.ToInt32(reader["application_id"]);
                                followUp.CompanyName = Convert.ToString(reader["company_name"]);
                                followUp.ApplicationName = Convert.ToString(reader["application_name"]);
                                followUp.FollowUpDate = Convert.ToDateTime(reader["follow_up_date"]).ToString("yyyy-MM-dd");

                                followUps.Add(followUp);
                            }
                        }
                    }
                }
            }
            catch
            {
                return new List<FollowUpReminderSummary>();
            }

            return followUps;
        }

        [WebMethod(EnableSession = true)]
        public List<InterviewSummary> GetUpcomingInterviews()
        {
            List<InterviewSummary> interviews = new List<InterviewSummary>();

            if (Session["userId"] == null)
            {
                return interviews;
            }

            int userId = Convert.ToInt32(Session["userId"]);

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        SELECT
                            i.interview_id,
                            i.application_id,
                            i.recruiter_id,
                            i.interview_title,
                            i.interview_type,
                            i.interview_date,
                            i.interviewer_name,
                            i.location_or_link,
                            i.notes,
                            i.interview_status,
                            a.company_name,
                            a.job_title AS application_name,
                            TRIM(CONCAT_WS(' ', r.first_name, r.last_name)) AS recruiter_name
                        FROM interviews i
                        INNER JOIN applications a
                            ON i.application_id = a.application_id
                        LEFT JOIN recruiters r
                            ON i.recruiter_id = r.recruiter_id
                            AND r.application_id = i.application_id
                        WHERE a.user_id = @userId
                            AND i.interview_date >= NOW()
                            AND i.interview_status = 'Scheduled'
                        ORDER BY i.interview_date ASC, i.interview_id ASC;";

                    using (MySqlCommand command = new MySqlCommand(query, con))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                InterviewSummary interview = new InterviewSummary();

                                interview.InterviewId = Convert.ToInt32(reader["interview_id"]);
                                interview.ApplicationId = Convert.ToInt32(reader["application_id"]);
                                interview.RecruiterId = reader["recruiter_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["recruiter_id"]);
                                interview.InterviewTitle = Convert.ToString(reader["interview_title"]);
                                interview.InterviewType = Convert.ToString(reader["interview_type"]);
                                interview.InterviewDate = Convert.ToDateTime(reader["interview_date"]).ToString("yyyy-MM-dd HH:mm:ss");
                                interview.InterviewerName = reader["interviewer_name"] == DBNull.Value ? "" : Convert.ToString(reader["interviewer_name"]);
                                interview.LocationOrLink = reader["location_or_link"] == DBNull.Value ? "" : Convert.ToString(reader["location_or_link"]);
                                interview.Notes = reader["notes"] == DBNull.Value ? "" : Convert.ToString(reader["notes"]);
                                interview.InterviewStatus = Convert.ToString(reader["interview_status"]);
                                interview.RecruiterName = reader["recruiter_name"] == DBNull.Value ? "" : Convert.ToString(reader["recruiter_name"]);
                                interview.CompanyName = Convert.ToString(reader["company_name"]);
                                interview.ApplicationName = Convert.ToString(reader["application_name"]);

                                interviews.Add(interview);
                            }
                        }
                    }
                }
            }
            catch
            {
                return new List<InterviewSummary>();
            }

            return interviews;
        }

        [WebMethod(EnableSession = true)]
        public List<ApplicationOption> GetApplicationOptions()
        {
            List<ApplicationOption> applications =
                new List<ApplicationOption>();

            if (Session["username"] == null)
            {
                return applications;
            }

            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        SELECT
                            a.application_id,
                            a.company_name,
                            a.job_title
                        FROM applications a
                        INNER JOIN users u
                            ON a.user_id = u.user_id
                        WHERE u.username = @username
                        AND a.is_archived = 0
                        ORDER BY a.company_name, a.job_title;";

                    using (MySqlCommand cmd =
                        new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@username",
                            Session["username"].ToString()
                        );

                        using (MySqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ApplicationOption application =
                                    new ApplicationOption();

                                application.ApplicationId =
                                    Convert.ToInt32(
                                        reader["application_id"]
                                    );

                                application.CompanyName =
                                    reader["company_name"].ToString();

                                application.JobTitle =
                                    reader["job_title"].ToString();

                                applications.Add(application);
                            }
                        }
                    }
                }
            }
            catch
            {
                return new List<ApplicationOption>();
            }

            return applications;
        }
        [WebMethod(EnableSession = true)]
        public List<SearchRecordSummary> GetSearchRecords()
        {
            List<SearchRecordSummary> records =
                new List<SearchRecordSummary>();

            if (Session["username"] == null)
            {
                return records;
            }

            try
            {
                using (MySqlConnection con =
                    new MySqlConnection(getConString()))
                {
                    con.Open();

                    string query = @"
                        SELECT
                            'Application' AS record_type,
                            a.application_id AS record_id,
                            a.job_title AS title,
                            a.company_name,
                            a.application_id AS related_application_id,
                            a.job_title AS related_application_name,
                            a.notes,
                            a.application_status AS record_status,
                            DATE_FORMAT(a.date_applied, '%Y-%m-%d') AS search_date,
                            DATE_FORMAT(a.updated_at, '%Y-%m-%d') AS last_updated
                        FROM applications a
                        INNER JOIN users u
                            ON a.user_id = u.user_id
                        WHERE u.username = @username

                        UNION ALL

                        SELECT
                            'Recruiter' AS record_type,
                            r.recruiter_id AS record_id,
                            CONCAT(r.first_name, ' ', r.last_name) AS title,
                            r.company_name,
                            a.application_id AS related_application_id,
                            a.job_title AS related_application_name,
                            r.notes,
                            '' AS record_status,
                            COALESCE(
                                DATE_FORMAT(r.last_contact_date, '%Y-%m-%d'),
                                DATE_FORMAT(r.follow_up_reminder_date, '%Y-%m-%d'),
                                DATE_FORMAT(r.updated_at, '%Y-%m-%d')
                            ) AS search_date,
                            DATE_FORMAT(r.updated_at, '%Y-%m-%d') AS last_updated
                        FROM recruiters r
                        INNER JOIN applications a
                            ON r.application_id = a.application_id
                        INNER JOIN users u
                            ON a.user_id = u.user_id
                        WHERE u.username = @username

                        UNION ALL

                        SELECT
                            'Document' AS record_type,
                            d.document_id AS record_id,
                            d.file_name AS title,
                            a.company_name,
                            a.application_id AS related_application_id,
                            a.job_title AS related_application_name,
                            d.notes,
                            '' AS record_status,
                            DATE_FORMAT(d.uploaded_at, '%Y-%m-%d') AS search_date,
                            DATE_FORMAT(d.uploaded_at, '%Y-%m-%d') AS last_updated
                        FROM documents d
                        INNER JOIN application_documents ad
                            ON d.document_id = ad.document_id
                        INNER JOIN applications a
                            ON ad.application_id = a.application_id
                        INNER JOIN users u
                            ON a.user_id = u.user_id
                        WHERE u.username = @username

                        ORDER BY last_updated DESC;";

                    using (MySqlCommand cmd =
                        new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@username",
                            Session["username"].ToString()
                        );

                        using (MySqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SearchRecordSummary record =
                                    new SearchRecordSummary();

                                record.RecordType =
                                    reader["record_type"].ToString();

                                record.RecordId =
                                    Convert.ToInt32(reader["record_id"]);

                                record.Title =
                                    reader["title"].ToString();

                                record.CompanyName =
                                    reader["company_name"].ToString();

                                record.RelatedApplicationId =
                                    Convert.ToInt32(
                                        reader["related_application_id"]
                                    );

                                record.RelatedApplicationName =
                                    reader["related_application_name"]
                                        .ToString();

                                record.Notes =
                                    reader["notes"] == DBNull.Value
                                        ? ""
                                        : reader["notes"].ToString();

                                record.Status =
                                    reader["record_status"] == DBNull.Value
                                        ? ""
                                        : reader["record_status"].ToString();

                                record.SearchDate =
                                    reader["search_date"] == DBNull.Value
                                        ? ""
                                        : reader["search_date"].ToString();

                                record.LastUpdated =
                                    reader["last_updated"] == DBNull.Value
                                        ? ""
                                        : reader["last_updated"].ToString();

                                records.Add(record);
                            }
                        }
                    }
                }
            }
            catch
            {
                return new List<SearchRecordSummary>();
            }

            return records;
        }

        public class RecruiterSummary
        {
            public int RecruiterId { get; set; }
            public int ApplicationId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string CompanyName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string JobTitle { get; set; }
            public string FollowUpReminderDate { get; set; }
            public string LastContactDate { get; set; }
            public string Notes { get; set; }
        }

        public class InterviewSummary
        {
            public int InterviewId { get; set; }
            public int ApplicationId { get; set; }
            public int? RecruiterId { get; set; }
            public string InterviewTitle { get; set; }
            public string InterviewType { get; set; }
            public string InterviewDate { get; set; }
            public string InterviewerName { get; set; }
            public string LocationOrLink { get; set; }
            public string Notes { get; set; }
            public string InterviewStatus { get; set; }
            public string RecruiterName { get; set; }
            public string CompanyName { get; set; }
            public string ApplicationName { get; set; }
        }

        public class SearchRecordSummary
        {
            public string RecordType { get; set; }
            public int RecordId { get; set; }
            public string Title { get; set; }
            public string CompanyName { get; set; }
            public int RelatedApplicationId { get; set; }
            public string RelatedApplicationName { get; set; }
            public string Notes { get; set; }
            public string Status { get; set; }
            public string SearchDate { get; set; }
            public string LastUpdated { get; set; }
        }

        public class ApplicationOption
        {
            public int ApplicationId { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
        }

        public class DocumentSummary
        {
            public int DocumentId { get; set; }
            public string DocumentType { get; set; }
            public string FileName { get; set; }
            public int FileSize { get; set; }
            public string Notes { get; set; }
            public string UploadedAt { get; set; }
            public int ApplicationCount { get; set; }
        }

        public class DocumentDetailsResult
        {
            public int DocumentId { get; set; }
            public string DocumentType { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public int FileSize { get; set; }
            public string Notes { get; set; }
            public string UploadedAt { get; set; }
            public List<DocumentApplicationSummary> Applications { get; set; }
        }

        public class DocumentApplicationSummary
        {
            public int ApplicationId { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
            public string ApplicationStatus { get; set; }
            public string DateApplied { get; set; }
            public bool IsArchived { get; set; }
            public string ApplicationNotes { get; set; }
            public string LinkedAt { get; set; }
        }

        public class ApplicationDocumentsResult
        {
            public int ApplicationId { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
            public string Location { get; set; }
            public string DateApplied { get; set; }
            public string ApplicationStatus { get; set; }
            public string JobPostingUrl { get; set; }
            public string FollowUpDate { get; set; }
            public bool IsArchived { get; set; }
            public List<ApplicationDocumentSummary> Documents { get; set; }
        }

        public class ApplicationDocumentSummary
        {
            public int DocumentId { get; set; }
            public string DocumentType { get; set; }
            public string FileName { get; set; }
            public int FileSize { get; set; }
            public string Notes { get; set; }
            public string UploadedAt { get; set; }
        }

        public class FollowUpReminderSummary
        {
            public string FollowUpType { get; set; }
            public int RelatedId { get; set; }
            public string FollowUpName { get; set; }
            public int ApplicationId { get; set; }
            public string CompanyName { get; set; }
            public string ApplicationName { get; set; }
            public string FollowUpDate { get; set; }
        }
    }
}
