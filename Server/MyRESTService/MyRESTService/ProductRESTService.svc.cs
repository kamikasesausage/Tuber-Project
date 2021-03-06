﻿using System;
using System.Collections.Generic;
using System.ServiceModel.Web;
using System.Net;
using System.Collections;
using System.Text;
using MySql.Data.MySqlClient;
using System.Device.Location;
using System.Security.Cryptography;
using System.Web.Security;
using System.Net.Mail;
using System.IO;


namespace ToDoList
{
    public class ProductRESTService : IToDoService
    {
        // Developmental DB
        public const string connectionString = "Server=sql3.freemysqlhosting.net;Port=3306;Database=sql3153117;UID=sql3153117;Password=vjbaNtDruW";


        //////////////////////
        // Account Functions 
        //////////////////////
        public MakeUserItem CreateUser(CreateUserItem item)
        {
            lock (this)
            {
                // Create password hash to store in DB
                String hashValue = computeHash(item.userPassword, null);

                // Store user information in DB
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlTransaction transaction = null;
                    try
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();
                        MySqlCommand command = conn.CreateCommand();
                        command.Transaction = transaction;

                        command.CommandText = "INSERT INTO users VALUES (?userEmail, ?hashValue, ?userFirstName, ?userLastName, ?userBillingAddress, ?userBillingCity, ?userBillingState, ?userBillingCCNumber, ?userBillingCCExpDate, ?userBillingCCV, 0)";
                        command.Parameters.AddWithValue("userEmail", item.userEmail);
                        command.Parameters.AddWithValue("hashValue", hashValue);
                        command.Parameters.AddWithValue("userFirstName", item.userFirstName);
                        command.Parameters.AddWithValue("userLastName", item.userLastName);
                        command.Parameters.AddWithValue("userBillingAddress", item.userBillingAddress);
                        command.Parameters.AddWithValue("userBillingCity", item.userBillingCity);
                        command.Parameters.AddWithValue("userBillingState", item.userBillingState);
                        command.Parameters.AddWithValue("userBillingCCNumber", item.userBillingCCNumber);
                        command.Parameters.AddWithValue("userBillingCCExpDate", item.userBillingCCExpDate);
                        command.Parameters.AddWithValue("userBillingCCV", item.userBillingCCV);

                        if (command.ExecuteNonQuery() > 0)
                        {
                            MakeUserItem user = new MakeUserItem();
                            user.userEmail = item.userEmail;
                            user.userPassword = item.userPassword;

                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                            transaction.Commit();
                            return user;
                        }
                        else
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            transaction.Rollback();
                            return new MakeUserItem();
                        }
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        MakeUserItem user = new MakeUserItem();
                        user.userEmail = e.ToString();
                        return user;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
        }

        public VerifiedUserItem VerifyUser(UserItem item)
        {
            lock (this)
            {
                String returnedUserEmail = "";
                String returnedUserPassword = "";
                String returnedUserFirstName = "";
                String returnedUserLastName = "";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlTransaction transaction = null;
                    try
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();
                        MySqlCommand command = conn.CreateCommand();
                        command.Transaction = transaction;

                        command.CommandText = "select email, password, first_name, last_name from users where email = ?userEmail";
                        command.Parameters.AddWithValue("userEmail", item.userEmail);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                returnedUserEmail = reader.GetString("email");
                                returnedUserPassword = reader.GetString("password");
                                returnedUserFirstName = reader.GetString("first_name");
                                returnedUserLastName = reader.GetString("last_name");
                            }
                        }

                        if (!verifyHash(item.userPassword, returnedUserPassword))
                        {
                            // Password does not match what is stored in the DB
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                            return new VerifiedUserItem();
                        }
                        else
                        {
                            // Get courses the user is currently enrolled in
                            ArrayList studentCourses = new ArrayList();
                            command.CommandText = "select name from student_courses where email = ?email";
                            command.Parameters.AddWithValue("email", returnedUserEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    studentCourses.Add(reader.GetString("name"));
                                }
                            }

                            // Get courses that user is currently able to tutor
                            ArrayList tutorCourses = new ArrayList();
                            command.CommandText = "select name from tutor_courses where email = ?email";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    tutorCourses.Add(reader.GetString("name"));
                                }
                            }

                            String userToken = Guid.NewGuid().ToString();

                            // Verify the user doesn't already have a session token, if they do, delete it and give new session token.
                            String existingSessionEmail = "";

                            command.CommandText = "SELECT email FROM sessions WHERE email = ?email";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    existingSessionEmail = reader.GetString("email");
                                }
                            }

                            if (existingSessionEmail == item.userEmail)
                            {
                                command.CommandText = "DELETE FROM sessions WHERE email = ?userEmail";

                                if (command.ExecuteNonQuery() >= 0)
                                {
                                    command.CommandText = "INSERT INTO sessions VALUES (?userEmail, ?userToken)";
                                    command.Parameters.AddWithValue("userToken", userToken);

                                    if (command.ExecuteNonQuery() <= 0)
                                    {
                                        // Inserting new session token failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new VerifiedUserItem();
                                    }
                                }
                                else
                                {
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new VerifiedUserItem();
                                }
                            }
                            else
                            {
                                command.CommandText = "INSERT INTO sessions VALUES (?userEmail, ?userToken)";
                                command.Parameters.AddWithValue("userToken", userToken);

                                if (command.ExecuteNonQuery() <= 0)
                                {
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new VerifiedUserItem();
                                }
                            }

                            // Check to see if firebase token was provided, if not, skip this
                            if (item.firebaseToken != "" || item.firebaseToken.Length != 0)
                            {
                                // Verify the user doesn't already have a firebase token, if they do, delete it and store new firebase token.
                                String existingFirebaseEmail = "";
                                String existingFirebaseToken = "";

                                command.CommandText = "SELECT email, token FROM firebase_tokens WHERE email = ?email";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        existingFirebaseEmail = reader.GetString("email");
                                        existingFirebaseToken = reader.GetString("token");
                                    }
                                }

                                if (existingFirebaseEmail == item.userEmail && existingFirebaseToken.Equals(item.firebaseToken))
                                {
                                    VerifiedUserItem user = new VerifiedUserItem();
                                    user.userEmail = returnedUserEmail;
                                    user.userStudentCourses = studentCourses;
                                    user.userTutorCourses = tutorCourses;
                                    user.userToken = userToken;
                                    user.firebaseToken = existingFirebaseToken;
                                    user.userFirstName = returnedUserFirstName;
                                    user.userLastName = returnedUserLastName;

                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return user;
                                }
                                else if (existingFirebaseEmail == item.userEmail && !(existingFirebaseToken.Equals(item.firebaseToken)))
                                {
                                    command.CommandText = "DELETE FROM firebase_tokens WHERE email = ?userEmail";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        command.CommandText = "INSERT INTO firebase_tokens VALUES (?userEmail, ?firebaseToken)";
                                        command.Parameters.AddWithValue("firebaseToken", item.firebaseToken);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            VerifiedUserItem user = new VerifiedUserItem();
                                            user.userEmail = returnedUserEmail;
                                            user.userStudentCourses = studentCourses;
                                            user.userTutorCourses = tutorCourses;
                                            user.userToken = userToken;
                                            user.firebaseToken = item.firebaseToken;
                                            user.userFirstName = returnedUserFirstName;
                                            user.userLastName = returnedUserLastName;

                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            return user;
                                        }
                                        else
                                        {
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                            return new VerifiedUserItem();
                                        }
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new VerifiedUserItem();
                                    }
                                }
                                else
                                {
                                    command.CommandText = "INSERT INTO firebase_tokens VALUES (?userEmail, ?firebaseToken)";
                                    command.Parameters.AddWithValue("firebaseToken", item.firebaseToken);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        VerifiedUserItem user = new VerifiedUserItem();
                                        user.userEmail = returnedUserEmail;
                                        user.userStudentCourses = studentCourses;
                                        user.userTutorCourses = tutorCourses;
                                        user.userToken = userToken;
                                        user.userFirstName = returnedUserFirstName;
                                        user.userLastName = returnedUserLastName;

                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return user;
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new VerifiedUserItem();
                                    }
                                }
                            }
                            else
                            {
                                VerifiedUserItem user = new VerifiedUserItem();
                                user.userEmail = returnedUserEmail;
                                user.userStudentCourses = studentCourses;
                                user.userTutorCourses = tutorCourses;
                                user.userToken = userToken;
                                user.userFirstName = returnedUserFirstName;
                                user.userLastName = returnedUserLastName;

                                transaction.Commit();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                return user;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
        }

        public EnableTutoringResponseItem EnableTutoring(EnableTutoringRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                // Check to see if the user already has tutoring enabled
                if (checkTutorEligibility(item.userEmail))
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                    return new EnableTutoringResponseItem();
                }
                // Enable tutoring for the user if they are eligible
                else
                {
                    // Check to see if the user is eligible -- make sure they have less than 5 reported tutor incidents 
                    int incidentCount = -1;

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Get the count of all the reported tutor incidents
                            command.CommandText = "SELECT count(*) AS count FROM reported_tutors WHERE tutorEmail = ?tutorEmail;";
                            command.Parameters.AddWithValue("tutorEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    incidentCount = reader.GetInt32("count");
                                }
                            }

                            // Make sure there are less than 5 reported tutor incidents
                            if (incidentCount < 5)
                            {
                                command.CommandText = "UPDATE users SET tutor_eligible = ?eligibleFlag WHERE email = ?tutorEmail";
                                command.Parameters.AddWithValue("eligibleFlag", 1);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Tutor eligibility activated successfully
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new EnableTutoringResponseItem();
                                }
                                else
                                {
                                    // Something went wrong updating the tutor_eligible field
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new EnableTutoringResponseItem();
                                }
                            }
                            else
                            {
                                // User has too many reported tutor incidents
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                return new EnableTutoringResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new EnableTutoringResponseItem();
            }
        }

        public DisableTutoringResponseItem DisableTutoring(DisableTutoringRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                // Check to see if the user already has tutoring disabled
                if (checkTutorEligibility(item.userEmail))
                {
                    // Disable tutoring for account 
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            command.CommandText = "UPDATE users SET tutor_eligible = ?eligibleFlag WHERE email = ?tutorEmail";
                            command.Parameters.AddWithValue("tutorEmail", item.userEmail);
                            command.Parameters.AddWithValue("eligibleFlag", 0);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                // Tutor deactivated successfully
                                transaction.Commit();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                return new DisableTutoringResponseItem();
                            }
                            else
                            {
                                // Something went wrong with updating the tutor_eligible field
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                return new DisableTutoringResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                // Tutor eligibility is already disabled
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                    return new DisableTutoringResponseItem();
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new DisableTutoringResponseItem();
            }
        }

        public ChangeUserPasswordResponseItem ChangeUserPassword(ChangeUserPasswordRequestItem item)
        {
            lock (this)
            {
                String returnedHashedPassword = "";

                // Verify user email & token match
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Verify the old password provided matches what is in the DB
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Retrieve the current hashed password 
                            command.CommandText = "SELECT password FROM users WHERE email = ?userEmail";
                            command.Parameters.AddWithValue("userEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedHashedPassword = reader.GetString("password");
                                }
                            }

                            // Verify password provided matches returnedHashedPassword
                            if (!verifyHash(item.currentPassword, returnedHashedPassword))
                            {
                                // Password provided does not match what is on file in the DB
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                                return new ChangeUserPasswordResponseItem();
                            }
                            // Store the new password
                            else
                            {
                                // Create password hash to store in DB
                                String newHashedPassword = computeHash(item.newPassword, null);

                                // Store newHashedPassword in the DB
                                command.CommandText = "UPDATE users SET password = ?newPassword WHERE email = ?userEmail";
                                command.Parameters.AddWithValue("newPassword", newHashedPassword);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new ChangeUserPasswordResponseItem();
                                }
                                else
                                {
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                    return new ChangeUserPasswordResponseItem();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new ChangeUserPasswordResponseItem();
                }
            }
        }

        public ForgotPasswordResponseItem ForgotPassword(ForgotPasswordRequestItem item)
        {
            lock (this)
            {
                // Generate new password and store it as user's password
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlTransaction transaction = null;
                    try
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();
                        MySqlCommand command = conn.CreateCommand();
                        command.Transaction = transaction;

                        // Generate the new temporary password
                        String newPassword = Membership.GeneratePassword(6, 2);

                        // Hash the new temporary password
                        String newHashedPassword = computeHash(newPassword, null);

                        // Store the new temporary password in the DB so user can log in
                        command.CommandText = "UPDATE users SET password = ?newPassword WHERE email = ?userEmail";
                        command.Parameters.AddWithValue("newPassword", newHashedPassword);
                        command.Parameters.AddWithValue("userEmail", item.userEmail);

                        if (command.ExecuteNonQuery() > 0)
                        {
                            // Send the email to the user with the new temporary password
                            // Create the sending client
                            SmtpClient client = new SmtpClient();
                            client.Port = 25;
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                            client.UseDefaultCredentials = false;
                            client.Host = "smtp.gmail.com";
                            client.Port = 587;
                            client.EnableSsl = true;
                            client.Credentials = new System.Net.NetworkCredential("cs4500tuber@gmail.com", "traflip53");

                            // Create the mail object
                            MailMessage mail = new MailMessage("forgot_password@tuber.com", item.userEmail);
                            mail.IsBodyHtml = true;
                            mail.Subject = "Tuber Forgot Password Recovery";
                            mail.Body = "Your new temporary password is: " + newPassword + " <br /><br /> Please change it as soon as you login to the application. <br /><br /> Thanks, <br /> The Tuber Team";

                            // Send the mail
                            client.Send(mail);

                            // Everything worked 
                            transaction.Commit();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                            return new ForgotPasswordResponseItem();
                        }
                        else
                        {
                            // Updating the user password failed
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                            return new ForgotPasswordResponseItem();
                        }
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
        }

        public AddStudentClassesResponseItem AddStudentClasses(AddStudentClassesRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                List<String> currentlyEnrolledCourses = new List<String>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlTransaction transaction = null;
                    try
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();
                        MySqlCommand command = conn.CreateCommand();
                        command.Transaction = transaction;

                        // Get all courses the student is currently enrolled in
                        command.CommandText = "SELECT name FROM student_courses WHERE email = ?studentEmail";
                        command.Parameters.AddWithValue("studentEmail", item.userEmail);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currentlyEnrolledCourses.Add(reader.GetString("name"));
                            }
                        }

                        // Remove all courses that the student is currently enrolled in from the add list
                        for (int i = 0; i < currentlyEnrolledCourses.Count; i++)
                        {
                            item.classesToBeAdded.Remove(currentlyEnrolledCourses[i]);
                        }

                        // Enroll student in the courses that remain
                        for (int i = 0; i < item.classesToBeAdded.Count; i++)
                        {
                            command.CommandText = "INSERT INTO student_courses VALUES (?studentEmail, ?courseName);";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);
                            command.Parameters.AddWithValue("courseName", item.classesToBeAdded[i]);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                continue;
                            }
                            else
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                break;
                            }
                        }

                        // Commit the changes
                        transaction.Commit();

                        // Get updated list of every class the student is enrolled in
                        currentlyEnrolledCourses = new List<String>();

                        command.CommandText = "SELECT name FROM student_courses WHERE email = ?studentEmail";

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currentlyEnrolledCourses.Add(reader.GetString("name"));
                            }
                        }

                        // Return the list of enrolled classes
                        AddStudentClassesResponseItem responseItem = new AddStudentClassesResponseItem();
                        responseItem.enrolledStudentClasses = currentlyEnrolledCourses;
                        return responseItem;
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new AddStudentClassesResponseItem();
            }
        }

        public RemoveStudentClassesResponseItem RemoveStudentClasses(RemoveStudentClassesRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                List<String> currentlyEnrolledCourses = new List<String>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlTransaction transaction = null;
                    try
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();
                        MySqlCommand command = conn.CreateCommand();
                        command.Transaction = transaction;

                        // Remove all courses specified
                        for (int i = 0; i < item.classesToBeRemoved.Count; i++)
                        {
                            command.CommandText = "DELETE FROM student_courses WHERE name = ?courseName AND email = ?studentEmail;";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);
                            command.Parameters.AddWithValue("courseName", item.classesToBeRemoved[i]);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                continue;
                            }
                            else
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                break;
                            }
                        }

                        // Commit the changes
                        transaction.Commit();

                        // Get updated list of every class the student is enrolled in
                        currentlyEnrolledCourses = new List<String>();

                        command.CommandText = "SELECT name FROM student_courses WHERE email = ?studentEmail";

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currentlyEnrolledCourses.Add(reader.GetString("name"));
                            }
                        }

                        // Return the list of tutoring sessions
                        RemoveStudentClassesResponseItem responseItem = new RemoveStudentClassesResponseItem();
                        responseItem.enrolledStudentClasses = currentlyEnrolledCourses;
                        return responseItem;
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new RemoveStudentClassesResponseItem();
            }
        }

        public AddTutorClassesResponseItem AddTutorClasses(AddTutorClassesRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                // Make sure tutor is eligible to tutor
                if (checkTutorEligibility(item.userEmail))
                {
                    List<String> currentlyEnrolledCourses = new List<String>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Get all courses the tutor is currently enrolled in
                            command.CommandText = "SELECT name FROM tutor_courses WHERE email = ?tutorEmail";
                            command.Parameters.AddWithValue("tutorEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    currentlyEnrolledCourses.Add(reader.GetString("name"));
                                }
                            }

                            // Remove all courses that the tutor is currently enrolled in from the add list
                            for (int i = 0; i < currentlyEnrolledCourses.Count; i++)
                            {
                                item.classesToBeAdded.Remove(currentlyEnrolledCourses[i]);
                            }

                            // Enroll tutor in the courses that remain
                            for (int i = 0; i < item.classesToBeAdded.Count; i++)
                            {
                                command.CommandText = "INSERT INTO tutor_courses VALUES (?tutorEmail, ?courseName);";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);
                                command.Parameters.AddWithValue("courseName", item.classesToBeAdded[i]);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    break;
                                }
                            }

                            // Commit the changes
                            transaction.Commit();

                            // Get updated list of every class the student is enrolled in
                            currentlyEnrolledCourses = new List<String>();

                            command.CommandText = "SELECT name FROM tutor_courses WHERE email = ?tutorEmail";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    currentlyEnrolledCourses.Add(reader.GetString("name"));
                                }
                            }

                            // Return the list of tutoring sessions
                            AddTutorClassesResponseItem responseItem = new AddTutorClassesResponseItem();
                            responseItem.enrolledTutorClasses = currentlyEnrolledCourses;
                            return responseItem;
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User has tutor_eligible set to 0-- not able to tutor any class
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    return new AddTutorClassesResponseItem();
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new AddTutorClassesResponseItem();
            }
        }

        public RemoveTutorClassesResponseItem RemoveTutorClasses(RemoveTutorClassesRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                // Make sure tutor is eligible to tutor
                if (checkTutorEligibility(item.userEmail))
                {
                    List<String> currentlyEnrolledCourses = new List<String>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Remove all courses specified
                            for (int i = 0; i < item.classesToBeRemoved.Count; i++)
                            {
                                command.CommandText = "DELETE FROM tutor_courses WHERE name = ?courseName AND email = ?tutorEmail;";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);
                                command.Parameters.AddWithValue("courseName", item.classesToBeRemoved[i]);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    break;
                                }
                            }

                            // Commit the changes
                            transaction.Commit();

                            // Get updated list of every class the tutor is enrolled in
                            currentlyEnrolledCourses = new List<String>();

                            command.CommandText = "SELECT name FROM tutor_courses WHERE email = ?tutorEmail";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    currentlyEnrolledCourses.Add(reader.GetString("name"));
                                }
                            }

                            // Return the list of tutoring sessions
                            RemoveTutorClassesResponseItem responseItem = new RemoveTutorClassesResponseItem();
                            responseItem.enrolledTutorClasses = currentlyEnrolledCourses;
                            return responseItem;
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User has tutor_eligible set to 0-- not able to tutor any class
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    return new RemoveTutorClassesResponseItem();
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new RemoveTutorClassesResponseItem();
            }
        }

        public GetTutorRatingResponseItem GetTutorRating(GetTutorRatingRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                double returnedRatingCount = -1;
                double returnedAverageRating = -1;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand command = conn.CreateCommand();

                        // Get the rating of the tutor specified
                        command.CommandText = "SELECT COUNT(*) as count, ROUND(AVG(rating), 1) as averageRating FROM tutor_ratings WHERE tutorEmail = ?tutorEmail";
                        command.Parameters.AddWithValue("tutorEmail", item.tutorEmail);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                returnedRatingCount = reader.GetDouble("count");

                                if (reader.IsDBNull(reader.GetOrdinal("averageRating")))
                                {
                                    continue;
                                }
                                else
                                {
                                    returnedAverageRating = reader.GetDouble("averageRating");
                                }
                            }
                        }

                        if (returnedRatingCount == -1)
                        {
                            // Getting the information out of the database failed
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                            return new GetTutorRatingResponseItem();
                        }
                        else if (returnedRatingCount == 0)
                        {
                            // The tutor doesn't have any ratings so we give them a rating of 5
                            GetTutorRatingResponseItem tutorRating = new GetTutorRatingResponseItem();
                            tutorRating.ratingsCount = returnedRatingCount;
                            tutorRating.ratingsAverage = 5;
                            return tutorRating;
                        }
                        else
                        {
                            // Everything worked and we return the tutor's rating information
                            GetTutorRatingResponseItem tutorRating = new GetTutorRatingResponseItem();
                            tutorRating.ratingsCount = returnedRatingCount;
                            tutorRating.ratingsAverage = returnedAverageRating;
                            return tutorRating;
                        }
                    }
                    catch (Exception e)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new GetTutorRatingResponseItem();
            }
        }

        public GetStudentRatingResponseItem GetStudentRating(GetStudentRatingRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                double returnedRatingCount = -1;
                double returnedAverageRating = -1;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand command = conn.CreateCommand();

                        // Get the rating of the student specified
                        command.CommandText = "SELECT COUNT(*) as count, ROUND(AVG(rating), 1) as averageRating FROM student_ratings WHERE studentEmail = ?studentEmail";
                        command.Parameters.AddWithValue("studentEmail", item.studentEmail);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                returnedRatingCount = reader.GetDouble("count");

                                if (reader.IsDBNull(reader.GetOrdinal("averageRating")))
                                {
                                    continue;
                                }
                                else
                                {
                                    returnedAverageRating = reader.GetDouble("averageRating");
                                }
                            }
                        }

                        if (returnedRatingCount == -1)
                        {
                            // Getting the information out of the database failed
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                            return new GetStudentRatingResponseItem();
                        }
                        else if (returnedRatingCount == 0)
                        {
                            // The student doesn't have any ratings so we give them a rating of 5
                            GetStudentRatingResponseItem studentRating = new GetStudentRatingResponseItem();
                            studentRating.ratingsCount = returnedRatingCount;
                            studentRating.ratingsAverage = 5;
                            return studentRating;
                        }
                        else
                        {
                            // Everything worked and we return the student's rating information
                            GetStudentRatingResponseItem studentRating = new GetStudentRatingResponseItem();
                            studentRating.ratingsCount = returnedRatingCount;
                            studentRating.ratingsAverage = returnedAverageRating;
                            return studentRating;
                        }
                    }
                    catch (Exception e)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new GetStudentRatingResponseItem();
            }
        }


        /////////////////////////////
        // Immediate Tutor Functions 
        /////////////////////////////
        public MakeTutorAvailableResponseItem MakeTutorAvailable(TutorUserItem item)
        {
            lock (this)
            {
                String returnedUserEmail = "";
                String returnedCourseName = "";

                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Verify the user is able to tutor the course specified 
                                command.CommandText = "SELECT * FROM tutor_courses WHERE email = ?userEmail AND name = ?tutorCourse";
                                command.Parameters.AddWithValue("userEmail", item.userEmail);
                                command.Parameters.AddWithValue("tutorCourse", item.tutorCourse);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedUserEmail = reader.GetString("email");
                                        returnedCourseName = reader.GetString("name");
                                    }
                                }

                                if (item.userEmail == returnedUserEmail && item.tutorCourse == returnedCourseName)
                                {
                                    // Insert tutor into the available_tutors table
                                    command.CommandText = "INSERT INTO available_tutors VALUES (?userEmail, ?tutorCourse, ?latitude, ?longitude)";
                                    command.Parameters.AddWithValue("latitude", item.latitude);
                                    command.Parameters.AddWithValue("longitude", item.longitude);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Insertion happend as expected
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new MakeTutorAvailableResponseItem();
                                    }
                                    else
                                    {
                                        // Something went wrong inserting user into available_tutors
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                        return new MakeTutorAvailableResponseItem();
                                    }
                                }
                                else
                                {
                                    // User does not have ability to tutor the class specified
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                    return new MakeTutorAvailableResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new MakeTutorAvailableResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new MakeTutorAvailableResponseItem();
                }
            }
        }

        public DeleteTutorResponseItem DeleteTutorAvailable(DeleteTutorUserItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        String returnedUserEmail = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Verify the user to be deleted is in the available_tutors table
                                command.CommandText = "SELECT email FROM available_tutors WHERE email = ?userEmail";
                                command.Parameters.AddWithValue("userEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedUserEmail = reader.GetString("email");
                                    }
                                }

                                if (item.userEmail == returnedUserEmail)
                                {
                                    // If user is in the available_tutors table, delete them from it
                                    command.CommandText = "DELETE FROM available_tutors WHERE email = ?userEmail";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        // Deletion happened as expected
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new DeleteTutorResponseItem();
                                    }
                                    else
                                    {
                                        // Something went wrong deleting user from available_tutors
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new DeleteTutorResponseItem();
                                    }
                                }
                                else
                                {
                                    // User is not in the available_tutors table
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                    return new DeleteTutorResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new DeleteTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new DeleteTutorResponseItem();
                }
            }
        }

        public FindAvailableTutorResponseItem FindAvailableTutors(TutorUserItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedStudentEmail = "";
                    String returnedTutorEmail = "";
                    String returnedTutorFirstName = "";
                    String returnedTutorLastName = "";
                    String returnedCourseName = "";
                    Double returnedTutorLatitude = 0;
                    Double returnedTutorLongitude = 0;

                    List<AvailableTutorUserItem> availableTutors = new List<AvailableTutorUserItem>();

                    var studentCoord = new GeoCoordinate(Convert.ToDouble(item.latitude), Convert.ToDouble(item.longitude));

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();
                            MySqlCommand command = conn.CreateCommand();

                            // Verify student is in the class provided
                            command.CommandText = "SELECT email FROM student_courses WHERE name = ?courseName";
                            command.Parameters.AddWithValue("courseName", item.tutorCourse);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedStudentEmail = reader.GetString("email");
                                }
                            }

                            // If student is in class provided, return list of available tutors
                            if (returnedStudentEmail == item.userEmail)
                            {
                                command.CommandText = "SELECT * FROM available_tutors, users WHERE available_tutors.email = users.email AND course = ?courseName";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedTutorEmail = reader.GetString("email");
                                        returnedTutorFirstName = reader.GetString("first_name");
                                        returnedTutorLastName = reader.GetString("last_name");
                                        returnedCourseName = reader.GetString("course");
                                        returnedTutorLatitude = reader.GetDouble("latitude");
                                        returnedTutorLongitude = reader.GetDouble("longitude");

                                        var tutorCoord = new GeoCoordinate(returnedTutorLatitude, returnedTutorLongitude);

                                        // Calculate distance between tutor and student
                                        double distanceToTutor = studentCoord.GetDistanceTo(tutorCoord);

                                        AvailableTutorUserItem tutor = new AvailableTutorUserItem();
                                        tutor.userEmail = returnedTutorEmail;
                                        tutor.firstName = returnedTutorFirstName;
                                        tutor.lastName = returnedTutorLastName;
                                        tutor.tutorCourse = returnedCourseName;
                                        tutor.latitude = returnedTutorLatitude;
                                        tutor.longitude = returnedTutorLongitude;
                                        tutor.distanceFromStudent = distanceToTutor / 1609.34;

                                        availableTutors.Add(tutor);
                                    }
                                }

                                // Get all tutor ratings
                                double returnedRatingCount = -1;
                                double returnedAverageRating = -1;

                                for (int i = 0; i < availableTutors.Count; i++)
                                {
                                    command.CommandText = "SELECT COUNT(*) as count, ROUND(AVG(rating), 1) as averageRating FROM tutor_ratings WHERE tutorEmail = ?tutorEmail";
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("tutorEmail", availableTutors[i].userEmail);

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            returnedRatingCount = reader.GetDouble("count");

                                            if (reader.IsDBNull(reader.GetOrdinal("averageRating")))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                returnedAverageRating = reader.GetDouble("averageRating");
                                            }
                                        }
                                    }

                                    if (returnedRatingCount == 0)
                                    {
                                        availableTutors[i].ratingCount = returnedRatingCount;
                                        availableTutors[i].averageRating = 5;
                                    }
                                    else
                                    {
                                        availableTutors[i].ratingCount = returnedRatingCount;
                                        availableTutors[i].averageRating = returnedAverageRating;
                                    }
                                }
                            }
                            else
                            {
                                // Student is not in the class provided
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                return new FindAvailableTutorResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }

                    // Return list of available tutors
                    FindAvailableTutorResponseItem tutorList = new FindAvailableTutorResponseItem();
                    tutorList.availableTutors = availableTutors;
                    return tutorList;
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new FindAvailableTutorResponseItem();
                }
            }
        }

        public StudentTutorPairedItem PairStudentTutor(StudentTutorRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedTutorEmail = "";
                    String returnedCourseName = "";
                    String returnedTutorLatitude = "";
                    String returnedTutorLongitude = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Check that the tutor is still available 
                            command.CommandText = "SELECT * FROM available_tutors WHERE email = ?tutorEmail";
                            command.Parameters.AddWithValue("tutorEmail", item.requestedTutorEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedTutorEmail = reader.GetString("email");
                                    returnedCourseName = reader.GetString("course");
                                    returnedTutorLatitude = reader.GetString("latitude");
                                    returnedTutorLongitude = reader.GetString("longitude");
                                }
                            }

                            if (returnedTutorEmail == item.requestedTutorEmail)
                            {
                                // Remove tutor from available_tutor table
                                command.CommandText = "DELETE FROM available_tutors WHERE email = ?tutorEmail";

                                if (command.ExecuteNonQuery() >= 0)
                                {
                                    // Insert student & tutor into the tutor_sesssion_pairing table 
                                    command.CommandText = "INSERT INTO tutor_sessions_pairing (studentEmail, tutorEmail, course, studentLatitude, studentLongitude, tutorLatitude, tutorLongitude) VALUES (?studentEmail, ?tutorEmail, ?course, ?studentLatitude, ?studentLongitude, ?tutorLatitude, ?tutorLongitude)";
                                    command.Parameters.AddWithValue("studentEmail", item.userEmail);
                                    command.Parameters.AddWithValue("course", returnedCourseName);
                                    command.Parameters.AddWithValue("studentLatitude", item.studentLatitude);
                                    command.Parameters.AddWithValue("studentLongitude", item.studentLongitude);
                                    command.Parameters.AddWithValue("tutorLatitude", returnedTutorLatitude);
                                    command.Parameters.AddWithValue("tutorLongitude", returnedTutorLongitude);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Return the paired object
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        StudentTutorPairedItem paired = new StudentTutorPairedItem();
                                        paired.userEmail = item.userEmail;
                                        paired.userToken = item.userToken;
                                        paired.requestedTutorEmail = item.requestedTutorEmail;
                                        paired.tutorCourse = returnedCourseName;
                                        paired.studentLatitude = item.studentLatitude;
                                        paired.studentLongitude = item.studentLongitude;
                                        paired.tutorLatitude = returnedTutorLatitude;
                                        paired.tutorLongitude = returnedTutorLongitude;
                                        return paired;
                                    }
                                    else
                                    {
                                        // Inserting into tutor_session_pairing table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        return new StudentTutorPairedItem();
                                    }
                                }
                                else
                                {
                                    // Deleting from the available_tutors table failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new StudentTutorPairedItem();
                                }
                            }
                            else
                            {
                                // Tutor is no longer available to pair
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                return new StudentTutorPairedItem();
                            }
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StudentTutorPairedItem();
                }
            }
        }

        public PairedStatusItem CheckPairedStatus(CheckPairedStatusItem item)
        {
            lock (this)
            {
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        // Check that the tutor is still available 
                        String returnedTutorEmail = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Check to see if the tutor is still in the available_tutors table
                                command.CommandText = "SELECT * FROM available_tutors WHERE email = ?userEmail";
                                command.Parameters.AddWithValue("userEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedTutorEmail = reader.GetString("email");
                                    }
                                }

                                if (returnedTutorEmail == "")
                                {
                                    // Check to see if the tutor is in the tutor_sessions_pairing table
                                    command.CommandText = "SELECT * FROM tutor_sessions_pairing WHERE tutorEmail = ?userEmail";

                                    PairedStatusItem pairedStatus = new PairedStatusItem();

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            pairedStatus.studentEmail = reader.GetString("studentEmail");
                                            pairedStatus.userEmail = reader.GetString("tutorEmail");
                                            pairedStatus.tutorCourse = reader.GetString("course");
                                            pairedStatus.studentLatitude = reader.GetDouble("studentLatitude");
                                            pairedStatus.studentLongitude = reader.GetDouble("studentLongitude");
                                            pairedStatus.tutorLatitude = reader.GetDouble("tutorLatitude");
                                            pairedStatus.tutorLongitude = reader.GetDouble("tutorLongitude");
                                        }
                                    }

                                    if (pairedStatus.userEmail == "" || pairedStatus.userEmail == null)
                                    {
                                        // Tutor was not in the available_tutor or tutor_sessions_pairing table, the tutor shouldn't be calling this method
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        return new PairedStatusItem();
                                    }
                                    else
                                    {
                                        var tutorCoord = new GeoCoordinate(pairedStatus.tutorLatitude, pairedStatus.tutorLongitude);
                                        var studentCoord = new GeoCoordinate(pairedStatus.studentLatitude, pairedStatus.studentLongitude);

                                        pairedStatus.distanceFromStudent = studentCoord.GetDistanceTo(tutorCoord) / 1609.34;

                                        // Found the tutor in the tutor_sessions_pairing table -- send back the paired student-tutor object for the tutor app to update
                                        return pairedStatus;
                                    }
                                }
                                else
                                {
                                    // Tutor is still waiting for a student to pair with them so update the tutor's location
                                    command.CommandText = "UPDATE available_tutors SET latitude = ?latitude, longitude = ?longitude WHERE email = ?userEmail";
                                    command.Parameters.AddWithValue("latitude", item.latitude);
                                    command.Parameters.AddWithValue("longitude", item.longitude);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new PairedStatusItem();
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        return new PairedStatusItem();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new PairedStatusItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new PairedStatusItem();
                }
            }
        }

        public CheckSessionActiveStatusStudentResponseItem CheckSessionActiveStatusStudent(CheckSessionActiveStatusStudentRequestItem item)
        {
            lock (this)
            {
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedStudentEmail = "";
                    String returnedTutorEmail = "";
                    String returnedCourse = "";
                    String returnedSessionStartTime = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Check to see if the pairing is still in the tutor_sessions_active table
                            command.CommandText = "SELECT studentEmail, tutorEmail, course, DATE_FORMAT(session_start_time, '%Y-%m-%d %T') as session_start_time FROM tutor_sessions_active WHERE studentEmail = ?userEmail";
                            command.Parameters.AddWithValue("userEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedStudentEmail = reader.GetString("studentEmail");
                                    returnedTutorEmail = reader.GetString("tutorEmail");
                                    returnedCourse = reader.GetString("course");
                                    returnedSessionStartTime = reader.GetString("session_start_time");
                                }
                            }

                            if (returnedTutorEmail == "")
                            {
                                // Check to see if the pairing is in the tutor_sessions_completed table
                                command.CommandText = "SELECT tutor_session_id, studentEmail, tutorEmail, course, DATE_FORMAT(session_start_time, '%Y-%m-%d %T') as session_start_time, DATE_FORMAT(session_end_time, '%Y-%m-%d %T') as session_end_time, session_cost FROM tutor_sessions_completed WHERE studentEmail = ?userEmail AND tutorEmail = ?tutorEmail AND course = ?course AND session_start_time = ?sessionStartTime";
                                command.Parameters.AddWithValue("tutorEmail", item.tutorEmail);
                                command.Parameters.AddWithValue("course", item.course);
                                command.Parameters.AddWithValue("sessionStartTime", item.sessionStartTime);

                                CheckSessionActiveStatusStudentResponseItem status = new CheckSessionActiveStatusStudentResponseItem();

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        status.userEmail = reader.GetString("studentEmail");
                                        status.tutorEmail = reader.GetString("tutorEmail");
                                        status.course = reader.GetString("course");
                                        status.tutorSessionID = reader.GetString("tutor_session_id");
                                        status.sessionStartTime = reader.GetString("session_start_time");
                                        status.sessionEndTime = reader.GetString("session_end_time");
                                        status.sessionCost = reader.GetDouble("session_cost");
                                    }
                                }

                                if (status.userEmail == "" || status.userEmail == null)
                                {
                                    // Tutor was not in the available_tutor or tutor_sessions_pairing table, the tutor shouldn't be calling this method
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                    return new CheckSessionActiveStatusStudentResponseItem();
                                }
                                else
                                {
                                    // Found the tutor session item in the tutor_sessions_completed  table
                                    return status;
                                }
                            }
                            else
                            {
                                CheckSessionActiveStatusStudentResponseItem status = new CheckSessionActiveStatusStudentResponseItem();
                                status.userEmail = returnedStudentEmail;
                                status.tutorEmail = returnedTutorEmail;
                                status.course = returnedCourse;
                                status.sessionStartTime = returnedSessionStartTime;
                                return status;
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new CheckSessionActiveStatusStudentResponseItem();
                }
            }
        }

        public GetSessionStatusTutorResponseItem GetSessionStatusTutor(GetSessionStatusTutorRequestItem item)
        {
            lock (this)
            {
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        // Check that the tutor is still available 
                        String returnedTutorEmail = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                MySqlCommand command = conn.CreateCommand();

                                // Check to see if the tutor is still in the available_tutors table
                                command.CommandText = "SELECT * FROM available_tutors WHERE email = ?userEmail";
                                command.Parameters.AddWithValue("userEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedTutorEmail = reader.GetString("email");
                                    }
                                }

                                if (returnedTutorEmail == "" || returnedTutorEmail == null)
                                {
                                    // Check to see if the tutor is in the tutor_sessions_pairing table
                                    command.CommandText = "SELECT * FROM tutor_sessions_pairing WHERE tutorEmail = ?userEmail";

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            returnedTutorEmail = reader.GetString("tutorEmail");
                                        }
                                    }

                                    if (returnedTutorEmail == "" || returnedTutorEmail == null)
                                    {
                                        // Check to see if the tutor is in the tutor_sessions_pending table
                                        command.CommandText = "SELECT * FROM tutor_sessions_pending WHERE tutorEmail = ?userEmail";

                                        using (MySqlDataReader reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                returnedTutorEmail = reader.GetString("tutorEmail");
                                            }
                                        }

                                        if (returnedTutorEmail == "" || returnedTutorEmail == null)
                                        {
                                            // Check to see if the tutor is in the tutor_sessions_active table
                                            command.CommandText = "SELECT * FROM tutor_sessions_active WHERE tutorEmail = ?userEmail";

                                            using (MySqlDataReader reader = command.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    returnedTutorEmail = reader.GetString("tutorEmail");
                                                }
                                            }

                                            if (returnedTutorEmail == "" || returnedTutorEmail == null)
                                            {
                                                // Check to see if the tutor is in the tutor_sessions_completed table
                                                command.CommandText = "SELECT * FROM tutor_sessions_completed WHERE tutorEmail = ?userEmail";

                                                using (MySqlDataReader reader = command.ExecuteReader())
                                                {
                                                    while (reader.Read())
                                                    {
                                                        returnedTutorEmail = reader.GetString("tutorEmail");
                                                    }
                                                }

                                                if (returnedTutorEmail == "" || returnedTutorEmail == null)
                                                {
                                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                                    return new GetSessionStatusTutorResponseItem();
                                                }
                                                else
                                                {
                                                    GetSessionStatusTutorResponseItem sessionStatus = new GetSessionStatusTutorResponseItem();
                                                    sessionStatus.session_status = "completed";
                                                    return sessionStatus;
                                                }
                                            }
                                            else
                                            {
                                                GetSessionStatusTutorResponseItem sessionStatus = new GetSessionStatusTutorResponseItem();
                                                sessionStatus.session_status = "active";
                                                return sessionStatus;
                                            }
                                        }
                                        else
                                        {
                                            GetSessionStatusTutorResponseItem sessionStatus = new GetSessionStatusTutorResponseItem();
                                            sessionStatus.session_status = "pending";
                                            return sessionStatus;
                                        }

                                    }
                                    else
                                    {
                                        GetSessionStatusTutorResponseItem sessionStatus = new GetSessionStatusTutorResponseItem();
                                        sessionStatus.session_status = "paired";
                                        return sessionStatus;
                                    }
                                }
                                else
                                {
                                    // Tutor is still waiting for a student to pair with them so update the tutor's location
                                    GetSessionStatusTutorResponseItem sessionStatus = new GetSessionStatusTutorResponseItem();
                                    sessionStatus.session_status = "available";
                                    return sessionStatus;

                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new GetSessionStatusTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new GetSessionStatusTutorResponseItem();
                }
            }
        }

        public GetSessionStatusStudentResponseItem GetSessionStatusStudent(GetSessionStatusStudentRequestItem item)
        {
            lock (this)
            {
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Check that the tutor is still available 
                    String returnedStudentEmail = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Check to see if the student is in the pairing table
                            command.CommandText = "SELECT * FROM tutor_sessions_pairing WHERE studentEmail = ?userEmail";
                            command.Parameters.AddWithValue("userEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedStudentEmail = reader.GetString("studentEmail");
                                }
                            }

                            if (returnedStudentEmail == "" || returnedStudentEmail == null)
                            {
                                // Check to see if the student is in the tutor_sessions_pending table
                                command.CommandText = "SELECT * FROM tutor_sessions_pending WHERE studentEmail = ?userEmail";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("studentEmail");
                                    }
                                }

                                if (returnedStudentEmail == "" || returnedStudentEmail == null)
                                {
                                    // Check to see if the student is in the tutor_sessions_active table
                                    command.CommandText = "SELECT * FROM tutor_sessions_active WHERE studentEmail = ?userEmail";

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            returnedStudentEmail = reader.GetString("studentEmail");
                                        }
                                    }

                                    if (returnedStudentEmail == "" || returnedStudentEmail == null)
                                    {
                                        // Check to see if the tutor is in the tutor_sessions_completed table
                                        command.CommandText = "SELECT * FROM tutor_sessions_completed WHERE studentEmail = ?userEmail";

                                        using (MySqlDataReader reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                returnedStudentEmail = reader.GetString("studentEmail");
                                            }
                                        }

                                        if (returnedStudentEmail == "" || returnedStudentEmail == null)
                                        {
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            return new GetSessionStatusStudentResponseItem();
                                        }
                                        else
                                        {
                                            GetSessionStatusStudentResponseItem sessionStatus = new GetSessionStatusStudentResponseItem();
                                            sessionStatus.session_status = "completed";
                                            return sessionStatus;
                                        }
                                    }
                                    else
                                    {
                                        GetSessionStatusStudentResponseItem sessionStatus = new GetSessionStatusStudentResponseItem();
                                        sessionStatus.session_status = "active";
                                        return sessionStatus;
                                    }

                                }
                                else
                                {
                                    GetSessionStatusStudentResponseItem sessionStatus = new GetSessionStatusStudentResponseItem();
                                    sessionStatus.session_status = "pending";
                                    return sessionStatus;
                                }
                            }
                            else
                            {
                                // Student is in the pairing table
                                GetSessionStatusStudentResponseItem sessionStatus = new GetSessionStatusStudentResponseItem();
                                sessionStatus.session_status = "paired";
                                return sessionStatus;
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new GetSessionStatusStudentResponseItem();
                }
            }
        }

        public StartTutorSessionTutorResponseItem StartTutorSessionTutor(StartTutorSessionTutorItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        // Get info from tutor_sessions_pairing table
                        String returnedStudentEmail = "";
                        String returnedTutorEmail = "";
                        String returnedCourseName = "";
                        String returnedStudentLatitude = "";
                        String returnedStudentLongitude = "";
                        String returnedTutorLatitude = "";
                        String returnedTutorLongitude = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                command.CommandText = "SELECT * FROM tutor_sessions_pairing WHERE tutorEmail = ?tutorEmail";
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("studentEmail");
                                        returnedTutorEmail = reader.GetString("tutorEmail");
                                        returnedCourseName = reader.GetString("course");
                                        returnedStudentLatitude = reader.GetString("studentLatitude");
                                        returnedStudentLongitude = reader.GetString("studentLongitude");
                                        returnedTutorLatitude = reader.GetString("tutorLatitude");
                                        returnedTutorLongitude = reader.GetString("tutorLongitude");
                                    }
                                }

                                if (returnedTutorEmail == item.userEmail)
                                {
                                    // Remove pairing from the tutor_sessions_pairing table
                                    command.CommandText = "DELETE FROM tutor_sessions_pairing WHERE tutorEmail = ?tutorEmail";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        // Insert pairing into the tutor_sesssions_pending table
                                        command.CommandText = "INSERT INTO tutor_sessions_pending VALUES (?studentEmail, ?tutorEmail, ?course)";
                                        command.Parameters.AddWithValue("studentEmail", returnedStudentEmail);
                                        command.Parameters.AddWithValue("course", returnedCourseName);
                                        command.Parameters.AddWithValue("studentLatitude", returnedStudentLatitude);
                                        command.Parameters.AddWithValue("studentLongitude", returnedStudentLongitude);
                                        command.Parameters.AddWithValue("tutorLatitude", returnedTutorLatitude);
                                        command.Parameters.AddWithValue("tutorLongitude", returnedTutorLongitude);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            // Everything went as planned
                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            return new StartTutorSessionTutorResponseItem();
                                        }
                                        else
                                        {
                                            // Inserting into tutor_sessions_pending table failed
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            return new StartTutorSessionTutorResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Deleting from tutor_sessions_pairing failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new StartTutorSessionTutorResponseItem();
                                    }
                                }
                                else
                                {
                                    // Pairing session the tutor is looking for is no longer available. 
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                    return new StartTutorSessionTutorResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new StartTutorSessionTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StartTutorSessionTutorResponseItem();
                }
            }
        }

        public StartTutorSessionStudentResponseItem StartTutorSessionStudent(StartTutorSessionStudentItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Get info from tutor_sessions_pending table
                    String returnedStudentEmail = "";
                    String returnedTutorEmail = "";
                    String returnedCourseName = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            command.CommandText = "SELECT * FROM tutor_sessions_pending WHERE studentEmail = ?studentEmail";
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedStudentEmail = reader.GetString("studentEmail");
                                    returnedTutorEmail = reader.GetString("tutorEmail");
                                    returnedCourseName = reader.GetString("course");
                                }
                            }

                            if (returnedStudentEmail == item.userEmail)
                            {
                                // Remove pairing from the tutor_sessions_pending table
                                command.CommandText = "DELETE FROM tutor_sessions_pending WHERE studentEmail = ?studentEmail";

                                if (command.ExecuteNonQuery() >= 0)
                                {
                                    // Insert pairing into the tutor_sesssions_active table
                                    command.CommandText = "INSERT INTO tutor_sessions_active VALUES (?studentEmail, ?tutorEmail, ?course, ?session_start_time)";
                                    command.Parameters.AddWithValue("tutorEmail", returnedTutorEmail);
                                    command.Parameters.AddWithValue("course", returnedCourseName);
                                    command.Parameters.AddWithValue("session_start_time", DateTime.Now);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Everything went as planned
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new StartTutorSessionStudentResponseItem();
                                    }
                                    else
                                    {
                                        // Inserting into tutor_sessions_active table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        return new StartTutorSessionStudentResponseItem();
                                    }
                                }
                                else
                                {
                                    // Deleting from tutor_sessions_pending failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new StartTutorSessionStudentResponseItem();
                                }
                            }
                            else
                            {
                                // The tutor has not started the tutoring session from their end.
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                return new StartTutorSessionStudentResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StartTutorSessionStudentResponseItem();
                }
            }
        }

        public EndTutorSessionResponseItem EndTutorSession(EndTutorSessionRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        // Get info from tutor_sessions_active table
                        String returnedStudentEmail = "";
                        String returnedTutorEmail = "";
                        String returnedCourseName = "";
                        DateTime returnedSessionStartTime = DateTime.Now;
                        DateTime sessionEndTime = DateTime.Now;

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                MySqlCommand command = conn.CreateCommand();
                                command.CommandText = "SELECT * FROM tutor_sessions_active WHERE tutorEmail = ?tutorEmail";
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("studentEmail");
                                        returnedTutorEmail = reader.GetString("tutorEmail");
                                        returnedCourseName = reader.GetString("course");
                                        returnedSessionStartTime = reader.GetDateTime("session_start_time");
                                    }
                                }

                                if (returnedTutorEmail == item.userEmail)
                                {
                                    // Remove pairing from tutor_sessions_active table
                                    command.CommandText = "DELETE FROM tutor_sessions_active WHERE tutorEmail = ?tutorEmail";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        // Calculate the total cost of the tutoring session
                                        TimeSpan diff = sessionEndTime.Subtract(returnedSessionStartTime);
                                        double cost = diff.TotalMinutes * 0.25;
                                        cost = Math.Round(cost, 2);

                                        // Insert pairing into the tutor_sesssions_complete table
                                        command.CommandText = "INSERT INTO tutor_sessions_completed (studentEmail, tutorEmail, course, session_start_time, session_end_time, session_cost) VALUES (?studentEmail, ?tutorEmail, ?course, ?session_start_time, ?session_end_time, ?session_cost)";
                                        command.Parameters.AddWithValue("studentEmail", returnedStudentEmail);
                                        command.Parameters.AddWithValue("course", returnedCourseName);
                                        command.Parameters.AddWithValue("session_start_time", returnedSessionStartTime);
                                        command.Parameters.AddWithValue("session_end_time", sessionEndTime);
                                        command.Parameters.AddWithValue("session_cost", cost);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            int returnedTutorSessionID = -1;

                                            // Get the tutor_session_id
                                            command.CommandText = "SELECT LAST_INSERT_ID() as tutor_session_id FROM tutor_sessions_completed";
                                            using (MySqlDataReader reader = command.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    returnedTutorSessionID = reader.GetInt32("tutor_session_id");
                                                }
                                            }

                                            if (returnedTutorSessionID != -1)
                                            {
                                                // Return the completed tutor session information
                                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                                EndTutorSessionResponseItem endresponse = new EndTutorSessionResponseItem();
                                                endresponse.tutorSessionID = returnedTutorSessionID;
                                                endresponse.userEmail = returnedTutorEmail;
                                                endresponse.studentEmail = returnedStudentEmail;
                                                endresponse.course = returnedCourseName;
                                                endresponse.sessionStartTime = returnedSessionStartTime.ToString();
                                                endresponse.sessionEndTime = sessionEndTime.ToString();
                                                endresponse.sessionCost = cost;
                                                return endresponse;
                                            }
                                            else
                                            {
                                                // Getting the tutor_session_id from the tutor_sessions_completed table failed
                                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ExpectationFailed;
                                                EndTutorSessionResponseItem endresponse = new EndTutorSessionResponseItem();
                                                endresponse.tutorSessionID = returnedTutorSessionID;
                                                endresponse.userEmail = returnedTutorEmail;
                                                endresponse.studentEmail = returnedStudentEmail;
                                                endresponse.course = returnedCourseName;
                                                endresponse.sessionStartTime = returnedSessionStartTime.ToString();
                                                endresponse.sessionEndTime = sessionEndTime.ToString();
                                                endresponse.sessionCost = cost;
                                                return endresponse;
                                            }
                                        }
                                        else
                                        {
                                            // Inserting into tutor_sessions_completed failed
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            return new EndTutorSessionResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Deleting from tutor_sessions_active failed
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new EndTutorSessionResponseItem();
                                    }
                                }
                                else
                                {
                                    // Could not find the pairing in the tutor_sessions_active table
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                    return new EndTutorSessionResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0-- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new EndTutorSessionResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new EndTutorSessionResponseItem();
                }
            }
        }

        public UpdateStudentLocationResponseItem UpdateStudentLocation(UpdateStudentLocationRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Insert student's new location into the tutor_sessions_pairing table
                            command.CommandText = "UPDATE tutor_sessions_pairing SET studentLatitude = ?studentLatitude, studentLongitude = ?studentLongitude WHERE studentEmail = ?studentEmail";
                            command.Parameters.AddWithValue("studentLatitude", item.latitude);
                            command.Parameters.AddWithValue("studentLongitude", item.longitude);
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                // Retrieve the tutor's location to send back to the student
                                command.CommandText = "SELECT tutorEmail, tutorLatitude, tutorLongitude FROM tutor_sessions_pairing WHERE studentEmail = ?studentEmail";

                                UpdateStudentLocationResponseItem locationResponse = new UpdateStudentLocationResponseItem();

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        locationResponse.tutorEmail = reader.GetString("tutorEmail");
                                        locationResponse.tutorLatitude = reader.GetString("tutorLatitude");
                                        locationResponse.tutorLongitude = reader.GetString("tutorLongitude");
                                    }
                                }

                                transaction.Commit();
                                return locationResponse;
                            }
                            else
                            {
                                // Updating the student's location in the tutor_sessions_pairing table failed
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                return new UpdateStudentLocationResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new UpdateStudentLocationResponseItem();
                }
            }
        }

        public UpdateTutorLocationResponseItem UpdateTutorLocation(UpdateTutorLocationRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Insert tutors's new location into the tutor_sessions_pairing table
                                command.CommandText = "UPDATE tutor_sessions_pairing SET tutorLatitude = ?tutorLatitude, tutorLongitude = ?tutorLongitude WHERE tutorEmail = ?tutorEmail";
                                command.Parameters.AddWithValue("tutorLatitude", item.latitude);
                                command.Parameters.AddWithValue("tutorLongitude", item.longitude);
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Retrieve the student's location to send back to the tutor
                                    command.CommandText = "SELECT studentEmail, studentLatitude, studentLongitude FROM tutor_sessions_pairing WHERE tutorEmail = ?tutorEmail";

                                    UpdateTutorLocationResponseItem locationResponse = new UpdateTutorLocationResponseItem();

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            locationResponse.studentEmail = reader.GetString("studentEmail");
                                            locationResponse.studentLatitude = reader.GetString("studentLatitude");
                                            locationResponse.studentLongitude = reader.GetString("studentLongitude");
                                        }
                                    }

                                    transaction.Commit();
                                    return locationResponse;
                                }
                                else
                                {
                                    // Updating the tutor's location in the tutor_sessions_pairing table failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                    return new UpdateTutorLocationResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new UpdateTutorLocationResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new UpdateTutorLocationResponseItem();
                }
            }
        }

        public RateTutorResponseItem RateTutor(RateTutorItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedStudentEmail = "";
                    String returnedTutorEmail = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Check to make sure the student hasn't already rated the tutor
                            command.CommandText = "SELECT tutorEmail FROM tutor_ratings WHERE tutor_session_id = ?tutorSessionID";
                            command.Parameters.AddWithValue("tutorSessionID", item.tutorSessionID);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedTutorEmail = reader.GetString("tutorEmail");
                                }
                            }

                            if (returnedTutorEmail == "" || returnedTutorEmail == null)
                            {
                                // Check to see if student & tutor were involved in the specified session ID
                                command.CommandText = "SELECT studentEmail, tutorEmail FROM tutor_sessions_completed WHERE tutor_session_id = ?tutorSessionID";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("studentEmail");
                                        returnedTutorEmail = reader.GetString("tutorEmail");
                                    }
                                }

                                if (returnedStudentEmail == item.userEmail && returnedTutorEmail == item.tutorEmail)
                                {
                                    // Add the student's raiting of the stutor into the tutor_ratings table
                                    command.CommandText = "INSERT INTO tutor_ratings VALUES (?tutorSessionID, ?tutorEmail, ?studentEmail, ?rating)";
                                    command.Parameters.AddWithValue("studentEmail", returnedStudentEmail);
                                    command.Parameters.AddWithValue("tutorEmail", returnedTutorEmail);
                                    command.Parameters.AddWithValue("rating", item.rating);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Rating added successfully
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new RateTutorResponseItem();
                                    }
                                    else
                                    {
                                        // Insert rating into tutor_ratings table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ExpectationFailed;
                                        return new RateTutorResponseItem();
                                    }
                                }
                                else
                                {
                                    // Student & tutor were not apart of the same tutor session
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new RateTutorResponseItem();
                                }
                            }
                            else
                            {
                                // There is already a record in the tutor_ratings table for this session ID
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotAcceptable;
                                return new RateTutorResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new RateTutorResponseItem();
                }
            }
        }

        public RateStudentResponseItem RateStudent(RateStudentItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        String returnedStudentEmail = "";
                        String returnedTutorEmail = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Check to make sure the tutor hasn't already rated the student
                                command.CommandText = "SELECT studentEmail FROM student_ratings WHERE tutor_session_id = ?tutorSessionID";
                                command.Parameters.AddWithValue("tutorSessionID", item.tutorSessionID);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("studentEmail");
                                    }
                                }

                                if (returnedStudentEmail == "" || returnedStudentEmail == null)
                                {
                                    // Check to see if student & tutor were involved in the specified session ID
                                    command.CommandText = "SELECT studentEmail, tutorEmail FROM tutor_sessions_completed WHERE tutor_session_id = ?tutorSessionID";

                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            returnedStudentEmail = reader.GetString("studentEmail");
                                            returnedTutorEmail = reader.GetString("tutorEmail");
                                        }
                                    }

                                    if (returnedStudentEmail == item.studentEmail && returnedTutorEmail == item.userEmail)
                                    {
                                        // Add the tutor's raiting of the student into the tutor_ratings table
                                        command.CommandText = "INSERT INTO student_ratings VALUES (?tutorSessionID, ?tutorEmail, ?studentEmail, ?rating)";
                                        command.Parameters.AddWithValue("studentEmail", returnedStudentEmail);
                                        command.Parameters.AddWithValue("tutorEmail", returnedTutorEmail);
                                        command.Parameters.AddWithValue("rating", item.rating);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            // Rating added successfully
                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            return new RateStudentResponseItem();
                                        }
                                        else
                                        {
                                            // Insert rating into tutor_ratings table failed
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ExpectationFailed;
                                            return new RateStudentResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Student & tutor were not apart of the same tutor session
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new RateStudentResponseItem();
                                    }
                                }
                                else
                                {
                                    // There is already a record in the tutor_ratings table for  this session ID
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotAcceptable;
                                    return new RateStudentResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new RateStudentResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new RateStudentResponseItem();
                }
            }
        }

        ////////////////////////////
        // Study Hotspot Functions 
        ///////////////////////////
        public CreateStudyHotspotResponseItem CreateStudyHotspot(CreateStudyHotspotRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Check that the student is in the specified course
                    if (verifyStudentInCourse(item.userEmail, item.course))
                    {
                        String returnedHotspotID = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Insert the hotspot into the study_hotspots table
                                command.CommandText = "INSERT INTO study_hotspots (owner_email, course_name, topic, latitude, longitude, location_description, student_count) VALUES (?owner_email, ?course_name, ?topic, ?latitude, ?longitude, ?locationDescription, 1)";
                                command.Parameters.AddWithValue("owner_email", item.userEmail);
                                command.Parameters.AddWithValue("course_name", item.course);
                                command.Parameters.AddWithValue("topic", item.topic);
                                command.Parameters.AddWithValue("latitude", item.latitude);
                                command.Parameters.AddWithValue("longitude", item.longitude);
                                command.Parameters.AddWithValue("locationDescription", item.locationDescription);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Retreive the new hotspot_id
                                    command.CommandText = "SELECT LAST_INSERT_ID() as hotspot_id FROM study_hotspots";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            returnedHotspotID = reader.GetString("hotspot_id");
                                        }
                                    }

                                    // Insert creator of hotspot into the hotspots_members table
                                    command.CommandText = "INSERT INTO study_hotspots_members (hotspot_id, email) VALUES (?hotspot_id, ?email)";
                                    command.Parameters.AddWithValue("email", item.userEmail);
                                    command.Parameters.AddWithValue("hotspot_id", returnedHotspotID);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Hotspot created successfully
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        CreateStudyHotspotResponseItem hotspot = new CreateStudyHotspotResponseItem();
                                        hotspot.hotspotID = returnedHotspotID;
                                        return hotspot;
                                    }
                                    else
                                    {
                                        // Creator assigned to hotspot in hotspots_members table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new CreateStudyHotspotResponseItem();
                                    }
                                }
                                else
                                {
                                    // Insertion of hotspot into study_hotspots table failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new CreateStudyHotspotResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Student is not in the specified course
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new CreateStudyHotspotResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new CreateStudyHotspotResponseItem();
                }
            }
        }

        public FindStudyHotspotReturnItem FindStudyHotspots(StudyHotspotItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Check that the student is in the specified course
                    if (verifyStudentInCourse(item.userEmail, item.course))
                    {
                        String returnedHotspotID = "";
                        String returnedOwnerEmail = "";
                        String returnedCourseName = "";
                        String returnedTopic = "";
                        Double returnedHotspotLatitude = 0;
                        Double returnedHotspotLongitude = 0;
                        String returnedLocationDescription = "";
                        String returnedStudentCount = "";

                        List<AvailableStudyHotspotItem> availableHotspots = new List<AvailableStudyHotspotItem>();

                        var studentCoord = new GeoCoordinate(Convert.ToDouble(item.latitude), Convert.ToDouble(item.longitude));

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                MySqlCommand command = conn.CreateCommand();

                                // Find all the hotspots associated with the course name provided
                                command.CommandText = "SELECT * FROM study_hotspots WHERE course_name = ?courseName";
                                command.Parameters.AddWithValue("courseName", item.course);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedHotspotID = reader.GetString("hotspot_id");
                                        returnedOwnerEmail = reader.GetString("owner_email");
                                        returnedCourseName = reader.GetString("course_name");
                                        returnedTopic = reader.GetString("topic");
                                        returnedHotspotLatitude = reader.GetDouble("latitude");
                                        returnedHotspotLongitude = reader.GetDouble("longitude");
                                        returnedLocationDescription = reader.GetString("location_description");
                                        returnedStudentCount = reader.GetString("student_count");

                                        var hotspotCoord = new GeoCoordinate(returnedHotspotLatitude, returnedHotspotLongitude);

                                        double distanceToHotspot = studentCoord.GetDistanceTo(hotspotCoord);

                                        AvailableStudyHotspotItem hotspot = new AvailableStudyHotspotItem();
                                        hotspot.hotspotID = returnedHotspotID;
                                        hotspot.ownerEmail = returnedOwnerEmail;
                                        hotspot.course = returnedCourseName;
                                        hotspot.topic = returnedTopic;
                                        hotspot.latitude = returnedHotspotLatitude;
                                        hotspot.longitude = returnedHotspotLongitude;
                                        hotspot.locationDescription = returnedLocationDescription;
                                        hotspot.student_count = returnedStudentCount;
                                        hotspot.distanceToHotspot = distanceToHotspot / 1609.34;

                                        availableHotspots.Add(hotspot);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }

                        // Return study hotspots
                        FindStudyHotspotReturnItem studyHotspotsList = new FindStudyHotspotReturnItem();
                        studyHotspotsList.studyHotspots = availableHotspots;
                        return studyHotspotsList;
                    }
                    else
                    {
                        // Student is not in the specified course
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new FindStudyHotspotReturnItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new FindStudyHotspotReturnItem();
                }
            }
        }

        public UserHotspotStatusResponseItem UserHotspotStatus(UserHotspotStatusRequestItem item)
        {
            // Check that the user token is valid
            if (checkUserToken(item.userEmail, item.userToken))
            {
                String returnedHotspotID = "";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand command = conn.CreateCommand();

                        // Check to see if user is in a hotspot
                        command.CommandText = "SELECT hotspot_id FROM study_hotspots_members WHERE email = ?email";
                        command.Parameters.AddWithValue("email", item.userEmail);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                returnedHotspotID = reader.GetString("hotspot_id");
                            }
                        }

                        if (returnedHotspotID != "")
                        {
                            // The user is in a hotspot, get hotspot info
                            command.CommandText = "SELECT * FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                            command.Parameters.AddWithValue("hotspotID", returnedHotspotID);

                            AvailableStudyHotspotItem hotspot = new AvailableStudyHotspotItem();

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    hotspot.hotspotID = returnedHotspotID;
                                    hotspot.ownerEmail = reader.GetString("owner_email");
                                    hotspot.course = reader.GetString("course_name");
                                    hotspot.topic = reader.GetString("topic");
                                    hotspot.latitude = reader.GetDouble("latitude");
                                    hotspot.longitude = reader.GetDouble("longitude");
                                    hotspot.locationDescription = reader.GetString("location_description");
                                    hotspot.student_count = reader.GetString("student_count");
                                }
                            }

                            // Check to see if user owns the hotspot
                            if (hotspot.ownerEmail == item.userEmail)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                UserHotspotStatusResponseItem hotspotResponse = new UserHotspotStatusResponseItem();
                                hotspotResponse.hotspotStatus = "owner";
                                hotspotResponse.hotspot = hotspot;
                                return hotspotResponse;
                            }
                            else
                            {
                                // User is in a hotspot, but does not own it
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                UserHotspotStatusResponseItem hotspotResponse = new UserHotspotStatusResponseItem();
                                hotspotResponse.hotspotStatus = "member";
                                hotspotResponse.hotspot = hotspot;
                                return hotspotResponse;
                            }
                        }
                        else
                        {
                            // User is not in a hotspot
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                            return new UserHotspotStatusResponseItem();
                        }
                    }
                    catch (Exception e)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        throw e;
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
            }
            else
            {
                // User's email & token combo is not valid
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return new UserHotspotStatusResponseItem();
            }
        }

        public StudyHotspotJoinResponseItem JoinStudyHotspot(StudyHotspotJoinItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Check that the student is in the specified course
                    if (verifyStudentInCourse(item.userEmail, item.course))
                    {
                        String returnedHotspotID = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Check to see if hotspot still exists
                                command.CommandText = "SELECT hotspot_id FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                                command.Parameters.AddWithValue("hotspotID", item.hotspotID);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedHotspotID = reader.GetString("hotspot_id");
                                    }
                                }

                                if (returnedHotspotID == item.hotspotID)
                                {
                                    // Insert user into hotspot 
                                    command.CommandText = "INSERT INTO study_hotspots_members (hotspot_id, email) VALUES (?hotspotID, ?email)";
                                    command.Parameters.AddWithValue("email", item.userEmail);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Update hotspot member count
                                        int returnedStudentCount = 0;

                                        command.CommandText = "SELECT student_count FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                                        using (MySqlDataReader reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                returnedStudentCount = reader.GetInt32("student_count");
                                            }
                                        }

                                        command.CommandText = "UPDATE study_hotspots SET student_count = ?studentCount where hotspot_id = ?hotspotID;";
                                        command.Parameters.AddWithValue("studentCount", returnedStudentCount + 1);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            // Adding user to study hotspot successful 
                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            return new StudyHotspotJoinResponseItem();
                                        }
                                        else
                                        {
                                            // Updating student count failed
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                            return new StudyHotspotJoinResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Inserting user into study_hotspots_members table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new StudyHotspotJoinResponseItem();
                                    }
                                }
                                else
                                {
                                    // Study hotspot no longer exists
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                    return new StudyHotspotJoinResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Student is not in the specified course
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new StudyHotspotJoinResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StudyHotspotJoinResponseItem();
                }
            }
        }

        public StudyHotspotLeaveRequestItem LeaveStudyHotspot(StudyHotspotLeaveItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedHotspotID = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Get the ID of the hotspot the user is leaving
                            command.CommandText = "SELECT hotspot_id FROM study_hotspots_members WHERE email = ?email";
                            command.Parameters.AddWithValue("email", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedHotspotID = reader.GetString("hotspot_id");
                                }
                            }

                            // Delete user from the study hotspot
                            command.CommandText = "DELETE FROM study_hotspots_members WHERE hotspot_id = ?hotspotID AND email = ?email";
                            command.Parameters.AddWithValue("hotspotID", returnedHotspotID);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                // Update hotspot member count
                                int returnedStudentCount = 0;

                                command.CommandText = "SELECT student_count FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentCount = reader.GetInt32("student_count");
                                    }
                                }

                                command.CommandText = "UPDATE study_hotspots SET student_count = ?studentCount where hotspot_id = ?hotspotID;";
                                command.Parameters.AddWithValue("studentCount", returnedStudentCount - 1);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Deleting user from study hotspot successful
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new StudyHotspotLeaveRequestItem();
                                }
                                else
                                {
                                    // Updating study hotspot count failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new StudyHotspotLeaveRequestItem();
                                }
                            }
                            else
                            {
                                // Deleting user from study_hotspots_members table failed
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                return new StudyHotspotLeaveRequestItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StudyHotspotLeaveRequestItem();
                }
            }
        }

        public StudyHotspotResponseItem GetStudyHotspotMembers(StudyHotspotGetMemberItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedEmail = "";
                    String returnedFirstName = "";
                    String returnedLastName = "";

                    List<String> memberEmails = new List<String>();

                    List<StudyHotspotMemberItem> hotspotMembers = new List<StudyHotspotMemberItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Get emails of all the members in the study hotspot
                            command.CommandText = "SELECT email FROM study_hotspots_members WHERE hotspot_id = ?hotspotID";
                            command.Parameters.AddWithValue("hotspotID", item.hotspotID);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedEmail = reader.GetString("email");
                                    memberEmails.Add(returnedEmail);
                                }
                            }

                            for (int i = 0; i < memberEmails.Count; i++)
                            {
                                // Get first and last name of all members in the hotspot
                                command.CommandText = "SELECT first_name, last_name FROM users WHERE email = ?email";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("email", memberEmails[i]);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedFirstName = reader.GetString("first_name");
                                        returnedLastName = reader.GetString("last_name");
                                    }
                                }

                                StudyHotspotMemberItem member = new StudyHotspotMemberItem();
                                member.userEmail = memberEmails[i];
                                member.firstName = returnedFirstName;
                                member.lastName = returnedLastName;

                                hotspotMembers.Add(member);
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }

                    // Return study hotspot members' names
                    StudyHotspotResponseItem members = new StudyHotspotResponseItem();
                    members.hotspotMembers = hotspotMembers;
                    return members;
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StudyHotspotResponseItem();
                }
            }
        }

        public StudyHotspotDeleteResponseItem DeleteStudyHotspot(StudyHotspotDeleteItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedUserEmail = "";
                    String returnedMemberEmail = "";

                    List<String> memberEmails = new List<String>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Check to see if the user owns the hotspot 
                            command.CommandText = "SELECT owner_email FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                            command.Parameters.AddWithValue("hotspotID", item.hotspotID);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedUserEmail = reader.GetString("owner_email");
                                }
                            }

                            if (returnedUserEmail == item.userEmail)
                            {
                                // Remove all members from the hotspot
                                command.CommandText = "SELECT email FROM study_hotspots_members WHERE hotspot_id = ?hotspotID";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedMemberEmail = reader.GetString("email");
                                        memberEmails.Add(returnedMemberEmail);
                                    }
                                }

                                for (int i = 0; i < memberEmails.Count; i++)
                                {
                                    command.CommandText = "DELETE FROM study_hotspots_members WHERE email = ?email";
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("email", memberEmails[i]);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        break;
                                    }
                                }

                                //  Remove delete the study hotspot
                                command.CommandText = "DELETE FROM study_hotspots WHERE hotspot_id = ?hotspotID";
                                command.Parameters.AddWithValue("hotspotID", item.hotspotID);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Deleting the study hotspot was successful
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new StudyHotspotDeleteResponseItem();
                                }
                                else
                                {
                                    // Deleting the study hotspot failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new StudyHotspotDeleteResponseItem();
                                }
                            }
                            else
                            {
                                // User trying to delete the study hotspot does not own the study hotspot
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                return new StudyHotspotDeleteResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StudyHotspotDeleteResponseItem();
                }
            }
        }


        ////////////////////////////
        // Schedule Tutor Functions 
        ///////////////////////////
        public ScheduleTutorResponseItem ScheduleTutor(ScheduleTutorItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Check that the student is in the specified course
                    if (verifyStudentInCourse(item.userEmail, item.course))
                    {
                        // Store student's tutor request in DB
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                command.CommandText = "INSERT INTO tutor_requests VALUES (?studentEmail, ?course, ?topic, ?dateTime, ?duration)";
                                command.Parameters.AddWithValue("studentEmail", item.userEmail);
                                command.Parameters.AddWithValue("course", item.course);
                                command.Parameters.AddWithValue("topic", item.topic);
                                command.Parameters.AddWithValue("dateTime", item.dateTime);
                                command.Parameters.AddWithValue("duration", item.duration);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Student's request stored successfully
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new ScheduleTutorResponseItem();
                                }
                                else
                                {
                                    // Student's request failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                    return new ScheduleTutorResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Student is not in the specified course
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new ScheduleTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new ScheduleTutorResponseItem();
                }
            }
        }

        public FindAllScheduleTutorResponseItem FindAllScheduleTutorRequests(FindAllScheduleTutorRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        List<ScheduleTutorRequestItem> studentRequests = new List<ScheduleTutorRequestItem>();

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                MySqlCommand command = conn.CreateCommand();

                                // Retrieve all tutor requests for the specified course
                                command.CommandText = "SELECT tutor_requests.student_email, tutor_requests.course, tutor_requests.topic, DATE_FORMAT(tutor_requests.date_time, '%Y-%m-%d %T') as date_time, tutor_requests.duration, users.first_name, users.last_name FROM tutor_requests, users WHERE tutor_requests.student_email = users.email AND course = ?courseName";
                                command.Parameters.AddWithValue("courseName", item.course);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        ScheduleTutorRequestItem request = new ScheduleTutorRequestItem();
                                        request.studentEmail = reader.GetString("student_email");
                                        request.firstName = reader.GetString("first_name");
                                        request.lastName = reader.GetString("last_name");
                                        request.course = reader.GetString("course");
                                        request.topic = reader.GetString("topic");
                                        request.dateTime = reader.GetString("date_time");
                                        request.duration = reader.GetString("duration");

                                        studentRequests.Add(request);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }

                        // Return the requests to the  tutor
                        FindAllScheduleTutorResponseItem studentRequestItemList = new FindAllScheduleTutorResponseItem();
                        studentRequestItemList.tutorRequestItems = studentRequests;
                        return studentRequestItemList;
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new FindAllScheduleTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new FindAllScheduleTutorResponseItem();
                }
            }
        }

        public FindAllScheduleTutorAcceptedResponsetItem FindAllScheduleTutorAcceptedRequests(FindAllScheduleTutorAcceptedRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        List<FindAllScheduleTutorAcceptedItem> studentRequests = new List<FindAllScheduleTutorAcceptedItem>();

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();

                                MySqlCommand command = conn.CreateCommand();

                                // Retrieve all tutor requests for the specified course
                                command.CommandText = "SELECT tutor_requests_accepted.student_email, tutor_requests_accepted.tutor_email, tutor_requests_accepted.course, tutor_requests_accepted.topic, DATE_FORMAT(tutor_requests_accepted.date_time, '%Y-%m-%d %T') as date_time, tutor_requests_accepted.duration, users.first_name, users.last_name FROM tutor_requests_accepted, users WHERE tutor_requests_accepted.student_email = users.email AND course = ?courseName";
                                command.Parameters.AddWithValue("courseName", item.course);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        FindAllScheduleTutorAcceptedItem request = new FindAllScheduleTutorAcceptedItem();
                                        request.studentEmail = reader.GetString("student_email");
                                        request.firstName = reader.GetString("first_name");
                                        request.lastName = reader.GetString("last_name");
                                        request.course = reader.GetString("course");
                                        request.topic = reader.GetString("topic");
                                        request.dateTime = reader.GetString("date_time");
                                        request.duration = reader.GetString("duration");

                                        studentRequests.Add(request);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }

                        // Return the requests to the  tutor
                        FindAllScheduleTutorAcceptedResponsetItem studentRequestItemList = new FindAllScheduleTutorAcceptedResponsetItem();
                        studentRequestItemList.tutorRequestItems = studentRequests;
                        return studentRequestItemList;
                    }
                    else
                    {
                        // User has tutor_eligible set to 0 -- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new FindAllScheduleTutorAcceptedResponsetItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new FindAllScheduleTutorAcceptedResponsetItem();
                }
            }
        }

        public AcceptStudentScheduleRequestResponseItem AcceptStudentScheduledRequest(AcceptStudentScheduleRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        String returnedStudentEmail = "";
                        String returnedCourseName = "";
                        String returnedTopic = "";
                        String returnedDateTime = "";
                        String returnedDuration = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Get selected student request information 
                                command.CommandText = "SELECT student_email, course, topic, DATE_FORMAT(date_time, '%Y-%m-%d %T') as date_time, duration FROM tutor_requests WHERE student_email = ?studentEmail AND course = ?course";
                                command.Parameters.AddWithValue("studentEmail", item.studentEmail);
                                command.Parameters.AddWithValue("course", item.course);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("student_email");
                                        returnedCourseName = reader.GetString("course");
                                        returnedTopic = reader.GetString("topic");
                                        returnedDateTime = reader.GetString("date_time");
                                        returnedDuration = reader.GetString("duration");
                                    }
                                }

                                if (returnedStudentEmail == item.studentEmail && returnedCourseName == item.course)
                                {
                                    // Remove scheduled tutor request from the tutor_requests table
                                    command.CommandText = "DELETE FROM tutor_requests WHERE student_email = ?studentEmail AND course = ?course";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        // Insert the pairing into the tutor_requests_accepted table
                                        command.CommandText = "INSERT INTO tutor_requests_accepted VALUES (?student_email, ?tutor_email, ?course, ?topic, ?dateTime, ?duration)";
                                        command.Parameters.Clear();
                                        command.Parameters.AddWithValue("student_email", item.studentEmail);
                                        command.Parameters.AddWithValue("tutor_email", item.userEmail);
                                        command.Parameters.AddWithValue("course", returnedCourseName);
                                        command.Parameters.AddWithValue("topic", returnedTopic);
                                        command.Parameters.AddWithValue("dateTime", returnedDateTime);
                                        command.Parameters.AddWithValue("duration", returnedDuration);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            // Pairing of the student and tutor scheduled request was successful
                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            AcceptStudentScheduleRequestResponseItem paired = new AcceptStudentScheduleRequestResponseItem();
                                            paired.student_email = item.studentEmail;
                                            paired.tutor_email = item.userEmail;
                                            paired.course = returnedCourseName;
                                            paired.topic = returnedTopic;
                                            paired.dateTime = returnedDateTime;
                                            paired.duration = returnedDuration;

                                            return paired;
                                        }
                                        else
                                        {
                                            // Insert pairing into tutor_requests_accepted table failed
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            return new AcceptStudentScheduleRequestResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Deleting from tutor_requests table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new AcceptStudentScheduleRequestResponseItem();
                                    }
                                }
                                else
                                {
                                    // Student schedule request no longer available
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                    return new AcceptStudentScheduleRequestResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0-- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new AcceptStudentScheduleRequestResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new AcceptStudentScheduleRequestResponseItem();
                }
            }
        }

        public CheckPairedStatusResponseItem CheckScheduledPairedStatus(CheckPairedStatusItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    List<PairedScheduledStatusItem> listings = new List<PairedScheduledStatusItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            // Check tutor_requests table for pending requests
                            MySqlCommand command = conn.CreateCommand();
                            command.CommandText = "SELECT student_email, course, topic, DATE_FORMAT(date_time, '%Y-%m-%d %T') as date_time, duration FROM tutor_requests WHERE student_email = ?userEmail";
                            command.Parameters.AddWithValue("userEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    PairedScheduledStatusItem statusItem = new PairedScheduledStatusItem();
                                    statusItem.studentEmail = reader.GetString("student_email");
                                    statusItem.course = reader.GetString("course");
                                    statusItem.topic = reader.GetString("topic");
                                    statusItem.dateTime = reader.GetString("date_time");
                                    statusItem.duration = reader.GetString("duration");
                                    statusItem.isPaired = false;
                                    listings.Add(statusItem);
                                }
                            }

                            // Check tutor_requests_accepted table for accepted requests
                            command.CommandText = "SELECT tutor_requests_accepted.student_email, tutor_requests_accepted.tutor_email, tutor_requests_accepted.course, tutor_requests_accepted.topic, DATE_FORMAT(tutor_requests_accepted.date_time, '%Y-%m-%d %T') as date_time, tutor_requests_accepted.duration, users.first_name, users.last_name FROM tutor_requests_accepted, users WHERE tutor_requests_accepted.tutor_email = users.email AND student_email = ?userEmail";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    PairedScheduledStatusItem statusItem = new PairedScheduledStatusItem();
                                    statusItem.studentEmail = reader.GetString("student_email");
                                    statusItem.tutorEmail = reader.GetString("tutor_email");
                                    statusItem.firstName = reader.GetString("first_name");
                                    statusItem.lastName = reader.GetString("last_name");
                                    statusItem.course = reader.GetString("course");
                                    statusItem.topic = reader.GetString("topic");
                                    statusItem.dateTime = reader.GetString("date_time");
                                    statusItem.duration = reader.GetString("duration");
                                    statusItem.isPaired = true;
                                    listings.Add(statusItem);
                                }
                            }

                            // Return all schedule tutor requests for the student
                            CheckPairedStatusResponseItem requests = new CheckPairedStatusResponseItem();
                            requests.requests = listings;
                            return requests;
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new CheckPairedStatusResponseItem();
                }
            }
        }

        public StartScheduledTutorSessionTutorResponseItem StartScheduledTutorSessionTutor(StartScheduledTutorSessionTutorItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Make sure tutor is eligible to tutor
                    if (checkTutorEligibility(item.userEmail))
                    {
                        // Get info from tutor_sessions_pairing table
                        String returnedStudentEmail = "";
                        String returnedTutorEmail = "";
                        String returnedCourseName = "";
                        String returnedTopic = "";
                        String returnedDuration = "";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            MySqlTransaction transaction = null;
                            try
                            {
                                conn.Open();
                                transaction = conn.BeginTransaction();
                                MySqlCommand command = conn.CreateCommand();
                                command.Transaction = transaction;

                                // Get information from tutor_requests_accepted table
                                command.CommandText = "SELECT student_email, tutor_email, course, topic, duration FROM tutor_requests_accepted WHERE tutor_email = ?tutorEmail  AND course = ?course AND date_time = ?dateTime";
                                command.Parameters.AddWithValue("tutorEmail", item.userEmail);
                                command.Parameters.AddWithValue("course", item.course);
                                command.Parameters.AddWithValue("dateTime", item.dateTime);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedStudentEmail = reader.GetString("student_email");
                                        returnedTutorEmail = reader.GetString("tutor_email");
                                        returnedCourseName = reader.GetString("course");
                                        returnedTopic = reader.GetString("topic");
                                        returnedDuration = reader.GetString("duration");
                                    }
                                }

                                if (returnedTutorEmail == item.userEmail && returnedCourseName == item.course)
                                {
                                    // Remove pairing from tutor_requests_accepted table
                                    command.CommandText = "DELETE FROM tutor_requests_accepted WHERE tutor_email = ?tutorEmail AND course = ?course AND date_time = ?dateTime";

                                    if (command.ExecuteNonQuery() >= 0)
                                    {
                                        // Insert pairing into the tutor_sesssions_pending table
                                        command.CommandText = "INSERT INTO tutor_sessions_pending VALUES (?studentEmail, ?tutorEmail, ?course)";
                                        command.Parameters.AddWithValue("studentEmail", returnedStudentEmail);

                                        if (command.ExecuteNonQuery() > 0)
                                        {
                                            // Tutor session started successfully
                                            transaction.Commit();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                            return new StartScheduledTutorSessionTutorResponseItem();
                                        }
                                        else
                                        {
                                            // Insert into tutor_sessions_active table failed
                                            transaction.Rollback();
                                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                            return new StartScheduledTutorSessionTutorResponseItem();
                                        }
                                    }
                                    else
                                    {
                                        // Deleting from tutor_requests_accepted table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                        return new StartScheduledTutorSessionTutorResponseItem();
                                    }
                                }
                                else
                                {
                                    // Pairing is no longer active
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                    return new StartScheduledTutorSessionTutorResponseItem();
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                            finally
                            {
                                if (conn != null)
                                {
                                    conn.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        // User has tutor_eligible set to 0-- not able to tutor any class
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                        return new StartScheduledTutorSessionTutorResponseItem();
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StartScheduledTutorSessionTutorResponseItem();
                }
            }
        }

        public StartScheduledTutorSessionStudentResponseItem StartScheduledTutorSessionStudent(StartScheduledTutorSessionStudentItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Get info from tutor_sessions_pending table
                    String returnedStudentEmail = "";
                    String returnedTutorEmail = "";
                    String returnedCourseName = "";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Get information from tutor_sessions_pending table
                            command.CommandText = "SELECT * FROM tutor_sessions_pending WHERE studentEmail = ?studentEmail AND course = ?course";
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);
                            command.Parameters.AddWithValue("course", item.course);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedStudentEmail = reader.GetString("studentEmail");
                                    returnedTutorEmail = reader.GetString("tutorEmail");
                                    returnedCourseName = reader.GetString("course");
                                }
                            }

                            if (returnedStudentEmail == item.userEmail && returnedCourseName == item.course)
                            {
                                // Remove pairing from tutor_requests_pending table
                                command.CommandText = "DELETE FROM tutor_sessions_pending WHERE studentEmail = ?studentEmail AND course = ?course";

                                if (command.ExecuteNonQuery() >= 0)
                                {
                                    // Insert pairing into the tutor_sesssions_active table
                                    command.CommandText = "INSERT INTO tutor_sessions_active VALUES (?studentEmail, ?tutorEmail, ?course, ?sessionStartTime)";
                                    command.Parameters.AddWithValue("tutorEmail", returnedTutorEmail);
                                    command.Parameters.AddWithValue("sessionStartTime", DateTime.Now);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Tutor session started successfully
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new StartScheduledTutorSessionStudentResponseItem();
                                    }
                                    else
                                    {
                                        // Insert into tutor_sessions_active table failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                        return new StartScheduledTutorSessionStudentResponseItem();
                                    }
                                }
                                else
                                {
                                    // Deleting from tutor_requests_pending table failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new StartScheduledTutorSessionStudentResponseItem();
                                }
                            }
                            else
                            {
                                // The tutor has not started the tutoring session from their end
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Gone;
                                return new StartScheduledTutorSessionStudentResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new StartScheduledTutorSessionStudentResponseItem();
                }
            }
        }


        ///////////////////////////
        // Report Tutor Functions 
        //////////////////////////
        public ReportTutorGetTutorListResponseItem ReportTutorGetTutorList(ReportTutorGetTutorListRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    String returnedEmail = "";
                    String returnedFirstName = "";
                    String returnedLastName = "";

                    List<String> tutorEmails = new List<String>();

                    List<ReportTutorGetTutorListItem> tutorResponseItems = new List<ReportTutorGetTutorListItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Get all tutor's that the student has met with
                            command.CommandText = "SELECT tutorEmail FROM tutor_sessions_completed WHERE studentEmail = ?studentEmail";
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedEmail = reader.GetString("tutorEmail");
                                    if (tutorEmails.Contains(returnedEmail))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        tutorEmails.Add(returnedEmail);
                                    }
                                }
                            }

                            for (int i = 0; i < tutorEmails.Count; i++)
                            {
                                // Get the tutors' first and last names
                                command.CommandText = "SELECT first_name, last_name FROM users WHERE email = ?email";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("email", tutorEmails[i]);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedFirstName = reader.GetString("first_name");
                                        returnedLastName = reader.GetString("last_name");
                                    }
                                }

                                ReportTutorGetTutorListItem tutor = new ReportTutorGetTutorListItem();
                                tutor.tutorEmail = tutorEmails[i];
                                tutor.tutorFirstName = returnedFirstName;
                                tutor.tutorLastName = returnedLastName;

                                tutorResponseItems.Add(tutor);
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }

                    // Return the list of tutors that the student met with
                    ReportTutorGetTutorListResponseItem responseList = new ReportTutorGetTutorListResponseItem();
                    responseList.tutorList = tutorResponseItems;
                    return responseList;
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new ReportTutorGetTutorListResponseItem();
                }
            }
        }

        public ReportTutorGetSessionListResponseItem ReportTutorGetSessionList(ReportTutorGetSessionListRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    List<ReportTutorGetSessionListItem> tutorResponseItems = new List<ReportTutorGetSessionListItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Get all tutoring sessions the student had with the specified tutor
                            command.CommandText = "SELECT tutor_session_id, course, session_start_time, session_end_time, session_cost FROM tutor_sessions_completed WHERE studentEmail = ?studentEmail AND tutorEmail = ?tutorEmail";
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);
                            command.Parameters.AddWithValue("tutorEmail", item.tutorEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    ReportTutorGetSessionListItem sessionItem = new ReportTutorGetSessionListItem();
                                    sessionItem.tutorEmail = item.tutorEmail;
                                    sessionItem.tutorFirstName = item.tutorFirstName;
                                    sessionItem.tutorLastName = item.tutorLastName;
                                    sessionItem.tutorSessionID = reader.GetString("tutor_session_id");
                                    sessionItem.course = reader.GetString("course");
                                    sessionItem.sessionStartTime = reader.GetString("session_start_time");
                                    sessionItem.sessionEndTime = reader.GetString("session_end_time");
                                    sessionItem.sessionCost = reader.GetString("session_cost");

                                    tutorResponseItems.Add(sessionItem);
                                }
                            }

                            // Return the list of tutoring sessions
                            ReportTutorGetSessionListResponseItem responseItem = new ReportTutorGetSessionListResponseItem();
                            responseItem.tutorList = tutorResponseItems;
                            return responseItem;
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new ReportTutorGetSessionListResponseItem();
                }
            }
        }

        public ReportTutorResponseItem ReportTutor(ReportTutorRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Insert report into reported_tutor table
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            // Store user report in reported_tutors table
                            command.CommandText = "INSERT INTO reported_tutors VALUES (?tutorSessionID, ?studentEmail, ?tutorEmail, ?message, ?reportDate)";
                            command.Parameters.AddWithValue("tutorSessionID", item.tutorSessionID);
                            command.Parameters.AddWithValue("studentEmail", item.userEmail);
                            command.Parameters.AddWithValue("tutorEmail", item.tutorEmail);
                            command.Parameters.AddWithValue("message", item.message);
                            command.Parameters.AddWithValue("reportDate", DateTime.Now);

                            if (command.ExecuteNonQuery() > 0)
                            {
                                // See if tutor now has 5 reports, if so, deactivate tutor status
                                int reportCount = -1;

                                command.CommandText = "SELECT count(*) as count FROM reported_tutors WHERE tutorEmail = ?tutorEmail";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        reportCount = reader.GetInt32("count");
                                    }
                                }

                                if (reportCount >= 5)
                                {
                                    command.CommandText = "UPDATE users SET tutor_eligible = ?eligibleFlag WHERE email = ?tutorEmail";
                                    command.Parameters.AddWithValue("eligibleFlag", 0);

                                    if (command.ExecuteNonQuery() > 0)
                                    {
                                        // Reporting tutor & deactivating tutor succeeded
                                        transaction.Commit();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                        return new ReportTutorResponseItem();
                                    }
                                    else
                                    {
                                        // Reporting tutor succeeded, but deactivating tutor failed
                                        transaction.Rollback();
                                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotModified;
                                        return new ReportTutorResponseItem();
                                    }
                                }
                                else
                                {
                                    // Reporting tutor & no deactivating tutor succeeded
                                    transaction.Commit();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                                    return new ReportTutorResponseItem();
                                }
                            }
                            else
                            {
                                // Inserting student's report for tutor failed
                                transaction.Rollback();
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                return new ReportTutorResponseItem();
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new ReportTutorResponseItem();
                }
            }
        }

        ////////////////////////
        // Messaging Functions 
        ///////////////////////

        public SendMessageResponseItem SendMessage(SendMessageRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Get token of person you are sending message to
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        MySqlTransaction transaction = null;
                        try
                        {
                            String returnedFirebaseToken = "";

                            conn.Open();
                            transaction = conn.BeginTransaction();
                            MySqlCommand command = conn.CreateCommand();
                            command.Transaction = transaction;

                            command.CommandText = "SELECT token FROM firebase_tokens WHERE email = ?recipientEmail";
                            command.Parameters.AddWithValue("recipientEmail", item.recipientEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    returnedFirebaseToken = reader.GetString("token");
                                }
                            }

                            if (returnedFirebaseToken == "" || returnedFirebaseToken == null)
                            {
                                // The user you are trying to send the message to does not have a firebase token on file.
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                                return new SendMessageResponseItem();
                            }
                            else
                            {
                                // Get sender's first and last name for message
                                String returnedFirstName = "";
                                String returnedLastName = "";

                                command.CommandText = "SELECT first_name, last_name FROM users WHERE email = ?senderEmail";
                                command.Parameters.AddWithValue("senderEmail", item.userEmail);

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        returnedFirstName = reader.GetString("first_name");
                                        returnedLastName = reader.GetString("last_name");
                                    }
                                }

                                // Get correct time to store into the DB
                                DateTime serverTime = DateTime.Now;
                                DateTime utcTime = serverTime.ToUniversalTime();
                                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
                                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);

                                // Insert message into the messages table
                                command.CommandText = "INSERT INTO messages (toEmail, fromEmail, time, message) VALUES (?toEmail, ?fromEmail, ?time, ?message);";
                                command.Parameters.AddWithValue("toEmail", item.recipientEmail);
                                command.Parameters.AddWithValue("fromEmail", item.userEmail);
                                command.Parameters.AddWithValue("time", localTime);
                                command.Parameters.AddWithValue("message", item.message);

                                if (command.ExecuteNonQuery() > 0)
                                {
                                    // Storing message in database was successful so try to send the message
                                    // Set up post request
                                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                                    httpWebRequest.Method = "POST";

                                    // Set up headers
                                    WebHeaderCollection headers = httpWebRequest.Headers;
                                    headers.Add("Authorization", "key=AAAA-w0mo_Q:APA91bGZO2IQDKvjJculAn8v9tqm3lIU0VBFoKZXpfFFfXr7lPq3bnF89BxXvGasUzcwlKWp8rBazrnvwXGoFDRmBF3Cx4G4W6iWa-eHMJj6OKHXrF7wf6kaDBwfBYcAINb4I_DL6m3D");

                                    httpWebRequest.Headers = headers;
                                    httpWebRequest.ContentType = "application/json";

                                    // Send the post requeset
                                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                                    {
                                        string json = "{\"notification\": { \"body\": \"" + item.message + "\", \"title\": \"Tuber User Message\", \"sound\": \"default\", \"priority\": \"high\", \"tag\": \"" + item.userEmail + ", " + returnedFirstName + ", " + returnedLastName + "\"}, \"data\": { \"id\": 1}, \"to\": \"" + returnedFirebaseToken + "\"}";

                                        streamWriter.Write(json);
                                        streamWriter.Flush();
                                        streamWriter.Close();
                                    }

                                    // Receive the response -- Need to parse response to make sure nothing failed
                                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                    {
                                        var result = streamReader.ReadToEnd();
                                    }

                                    transaction.Commit();
                                    return new SendMessageResponseItem();
                                }
                                else
                                {
                                    // Storing message in database failed
                                    transaction.Rollback();
                                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Conflict;
                                    return new SendMessageResponseItem();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();

                            if (e.ToString().Contains("400"))
                            {
                                // Google rejected the message
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotAcceptable;
                                throw e;
                            }
                            else
                            {
                                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                                throw e;
                            }
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new SendMessageResponseItem();
                }
            }
        }

        public GetMessageConversationResponseItem GetMessageConversation(GetMessageConversationRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Get all of the messages between the two users sepcified
                    List<MessageItem> messages = new List<MessageItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Find all the messages associated with the two users specified
                            command.CommandText = "SELECT * FROM messages WHERE (toEmail = ?toEmail1 AND fromEmail = ?fromEmail1) OR (toEmail = ?toEmail2 AND fromEmail = ?fromEmail2) ORDER BY time ASC";
                            command.Parameters.AddWithValue("toEmail1", item.recipientEmail);
                            command.Parameters.AddWithValue("fromEmail1", item.userEmail);
                            command.Parameters.AddWithValue("toEmail2", item.userEmail);
                            command.Parameters.AddWithValue("fromEmail2", item.recipientEmail);

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    MessageItem message = new MessageItem();
                                    message.messageID = reader.GetString("message_id");
                                    message.toEmail = reader.GetString("toEmail");
                                    message.fromEmail = reader.GetString("fromEmail");
                                    message.time = reader.GetString("time");
                                    message.message = reader.GetString("message");

                                    messages.Add(message);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }

                    // Return messages
                    GetMessageConversationResponseItem messageConversation = new GetMessageConversationResponseItem();
                    messageConversation.messages = messages;
                    return messageConversation;
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new GetMessageConversationResponseItem();
                }
            }
        }

        public GetUsersResponseItem GetUsers(GetUsersRequestItem item)
        {
            lock (this)
            {
                // Check that the user token is valid
                if (checkUserToken(item.userEmail, item.userToken))
                {
                    // Get all of the users
                    List<UserMessagingItem> users = new List<UserMessagingItem>();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();

                            MySqlCommand command = conn.CreateCommand();

                            // Get all of the user's emails and names
                            command.CommandText = "SELECT email, first_name, last_name FROM users";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    UserMessagingItem user = new UserMessagingItem();
                                    user.email = reader.GetString("email");
                                    user.firstName = reader.GetString("first_name");
                                    user.lastName = reader.GetString("last_name");

                                    users.Add(user);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                            throw e;
                        }
                        finally
                        {
                            if (conn != null)
                            {
                                conn.Close();
                            }
                        }
                    }

                    // Return messages
                    GetUsersResponseItem usersResponse = new GetUsersResponseItem();
                    usersResponse.users = users;
                    return usersResponse;
                }
                else
                {
                    // User's email & token combo is not valid
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                    return new GetUsersResponseItem();
                }
            }
        }



        ////////////////////
        // Helper Functions 
        ////////////////////

        private string computeHash(String password, byte[] saltBytes)
        {
            // If no salt, then create it
            if (saltBytes == null)
            {
                // Min and max size for salt array
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number to determine the salt size
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Create the salt byte array
                saltBytes = new byte[saltSize];

                // Fill the salt array
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                rng.GetNonZeroBytes(saltBytes);
            }
            // Convert password string into byte array
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(password);

            // Create array to hold the plainTextBytes and saltBytes
            byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into plainTextWithSaltBytes array
            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }

            // Copy salt bytes into end of plainTextWithSaltBytes array
            for (int i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }

            // Create hash function
            HashAlgorithm hash = new SHA256Managed();

            // Create hash of plainTextWithSaltBytes array
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array to hold hash and original salt bytes
            byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            // Copy hash bytes into hashWithSaltBytes array
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashWithSaltBytes[i] = hashBytes[i];
            }

            // Copy salt bytes into hashWithSaltBytes array
            for (int i = 0; i < saltBytes.Length; i++)
            {
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
            }

            // Convert hashWithSaltBytes into a base64-encoded string
            String hashValue = Convert.ToBase64String(hashWithSaltBytes);

            return hashValue;
        }

        private Boolean verifyHash(String password, String hashFromDB)
        {
            // Convert base64-encoded hash value into byte array
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashFromDB);

            // Create array to hold origianl salt bytes from hash
            byte[] saltBytes = new byte[hashWithSaltBytes.Length - 32];

            // Copy salt from hash to saltBytes array
            for (int i = 0; i < saltBytes.Length; i++)
            {
                saltBytes[i] = hashWithSaltBytes[32 + i];
            }

            // Compute new hash string
            String expectedHashString = computeHash(password, saltBytes);

            // Make sure the hash from the DB and newly computed hash match
            return (hashFromDB == expectedHashString);
        }

        private Boolean checkUserToken(String userEmail, String userToken)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "SELECT * FROM sessions WHERE email = ?userEmail";
                    command.Parameters.AddWithValue("userEmail", userEmail);

                    String returnedUserEmail = "";
                    String returnedUserToken = "";

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            returnedUserEmail = reader.GetString("email");
                            returnedUserToken = reader.GetString("sessionToken");
                        }
                    }

                    if (returnedUserEmail == userEmail && returnedUserToken == userToken)
                        return true;
                    else
                        return false;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private Boolean checkTutorEligibility(String userEmail)
        {
            int returnedTutorEligible = -1;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    MySqlCommand command = conn.CreateCommand();

                    command.CommandText = "SELECT tutor_eligible FROM users WHERE email = ?userEmail";
                    command.Parameters.AddWithValue("userEmail", userEmail);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            returnedTutorEligible = reader.GetInt32("tutor_eligible");
                        }
                    }

                    if (returnedTutorEligible == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private Boolean verifyStudentInCourse(String userEmail, String courseName)
        {
            String returnedStudentEmail = "";
            String returnedCourseName = "";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    MySqlCommand command = conn.CreateCommand();

                    command.CommandText = "SELECT email, name FROM student_courses WHERE email = ?userEmail and name = ?courseName";
                    command.Parameters.AddWithValue("userEmail", userEmail);
                    command.Parameters.AddWithValue("courseName", courseName);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            returnedStudentEmail = reader.GetString("email");
                            returnedCourseName = reader.GetString("name");
                        }
                    }

                    if (returnedStudentEmail == userEmail && returnedCourseName == courseName)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }
}
