﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace ToDoList
{
    public class UserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userPassword { get; set; }

        [DataMember]
        public string firebaseToken { get; set; }
    }

    public class CreateUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userPassword { get; set; }

        [DataMember]
        public string userFirstName { get; set; }

        [DataMember]
        public string userLastName { get; set; }

        [DataMember]
        public string userBillingAddress { get; set; }

        [DataMember]
        public string userBillingCity { get; set; }

        [DataMember]
        public string userBillingState { get; set; }

        [DataMember]
        public string userBillingCCNumber { get; set; }

        [DataMember]
        public string userBillingCCExpDate { get; set; }

        [DataMember]
        public string userBillingCCV { get; set; }
    }

    public class VerifiedUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userPassword { get; set; }

        [DataMember]
        public string userFirstName { get; set; }

        [DataMember]
        public string userLastName { get; set; }

        [DataMember]
        public ArrayList userStudentCourses { get; set; }

        [DataMember]
        public ArrayList userTutorCourses { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string firebaseToken { get; set; }
    }

    public class MakeUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userPassword { get; set; }

    }

    public class TutorUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorCourse { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }
    }



    public class AddStudentClassesRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public List<String> classesToBeAdded { get; set; }
    }

    public class AddStudentClassesResponseItem
    {
        [DataMember]
        public List<String> enrolledStudentClasses { get; set; }
    }

    public class RemoveStudentClassesRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public List<String> classesToBeRemoved { get; set; }
    }

    public class RemoveStudentClassesResponseItem
    {
        [DataMember]
        public List<String> enrolledStudentClasses { get; set; }
    }

    public class AddTutorClassesRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public List<String> classesToBeAdded { get; set; }
    }

    public class AddTutorClassesResponseItem
    {
        [DataMember]
        public List<String> enrolledTutorClasses { get; set; }
    }

    public class RemoveTutorClassesRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public List<String> classesToBeRemoved { get; set; }
    }

    public class RemoveTutorClassesResponseItem
    {
        [DataMember]
        public List<String> enrolledTutorClasses { get; set; }
    }

    public class EnableTutoringRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class EnableTutoringResponseItem
    {

    }

    public class DisableTutoringRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class DisableTutoringResponseItem
    {

    }

    public class ChangeUserPasswordRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string currentPassword { get; set; }

        [DataMember]
        public string newPassword { get; set; }
    }

    public class ChangeUserPasswordResponseItem
    {

    }

    public class ForgotPasswordRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }
    }

    public class ForgotPasswordResponseItem
    {

    }


    public class MakeTutorAvailableResponseItem
    {

    }

    public class AvailableTutorUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public double ratingCount { get; set; }

        [DataMember]
        public double averageRating { get; set; }

        [DataMember]
        public string tutorCourse { get; set; }

        [DataMember]
        public double latitude { get; set; }

        [DataMember]
        public double longitude { get; set; }

        [DataMember]
        public double distanceFromStudent { get; set; }
    }

    public class FindAvailableTutorResponseItem
    {
        [DataMember]
        public List<AvailableTutorUserItem> availableTutors { get; set; }
    }

    public class DeleteTutorUserItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class DeleteTutorResponseItem
    {

    }

    public class StudentTutorRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string requestedTutorEmail { get; set; }

        [DataMember]
        public string studentLatitude { get; set; }

        [DataMember]
        public string studentLongitude { get; set; }
    }

    public class StudentTutorPairedItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string requestedTutorEmail { get; set; }

        [DataMember]
        public string tutorCourse { get; set; }

        [DataMember]
        public string studentLatitude { get; set; }

        [DataMember]
        public string studentLongitude { get; set; }

        [DataMember]
        public string tutorLatitude { get; set; }

        [DataMember]
        public string tutorLongitude { get; set; }
    }

    // Sent to server to see if the tutor has been paired with a student for immediate tutor case
    public class CheckPairedStatusItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }
    }

    public class PairedStatusItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string tutorCourse { get; set; }

        [DataMember]
        public double studentLatitude { get; set; }

        [DataMember]
        public double studentLongitude { get; set; }

        [DataMember]
        public double tutorLatitude { get; set; }

        [DataMember]
        public double tutorLongitude { get; set; }

        [DataMember]
        public double distanceFromStudent { get; set; }
    }

    public class CheckSessionActiveStatusStudentRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string sessionStartTime { get; set; }
    }

    public class CheckSessionActiveStatusStudentResponseItem
    {
        [DataMember]
        public string tutorSessionID { get; set; }

        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string sessionStartTime { get; set; }

        [DataMember]
        public string sessionEndTime { get; set; }

        [DataMember]
        public double sessionCost { get; set; }
    }

    public class GetSessionStatusTutorRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class GetSessionStatusTutorResponseItem
    {

        [DataMember]
        public string session_status { get; set; }
    }

    public class GetSessionStatusStudentRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class GetSessionStatusStudentResponseItem
    {

        [DataMember]
        public string session_status { get; set; }
    }

    public class UpdateStudentLocationRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }
    }

    public class UpdateStudentLocationResponseItem
    {
        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string tutorLatitude { get; set; }

        [DataMember]
        public string tutorLongitude { get; set; }
    }

    public class UpdateTutorLocationRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }
    }

    public class UpdateTutorLocationResponseItem
    {
        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string studentLatitude { get; set; }

        [DataMember]
        public string studentLongitude { get; set; }
    }

    public class StartTutorSessionTutorItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class StartTutorSessionTutorResponseItem
    {

    }

    public class StartTutorSessionStudentItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class StartTutorSessionStudentResponseItem
    {

    }

    public class EndTutorSessionRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class EndTutorSessionResponseItem
    {
        [DataMember]
        public int tutorSessionID { get; set; }

        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string sessionStartTime { get; set; }

        [DataMember]
        public string sessionEndTime { get; set; }

        [DataMember]
        public double sessionCost { get; set; }
    }

    public class RateTutorItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorSessionID { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string rating { get; set; }
    }

    public class RateTutorResponseItem
    {

    }

    public class RateStudentItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorSessionID { get; set; }

        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string rating { get; set; }
    }

    public class RateStudentResponseItem
    {

    }

    public class GetTutorRatingRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }
    }

    public class GetTutorRatingResponseItem
    {
        [DataMember]
        public double ratingsCount { get; set; }

        [DataMember]
        public double ratingsAverage { get; set; }
    }

    public class GetStudentRatingRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string studentEmail { get; set; }
    }

    public class GetStudentRatingResponseItem
    {
        [DataMember]
        public double ratingsCount { get; set; }

        [DataMember]
        public double ratingsAverage { get; set; }
    }

    public class CreateStudyHotspotRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }

        [DataMember]
        public string locationDescription { get; set; }
    }

    public class CreateStudyHotspotResponseItem
    {
        [DataMember]
        public string hotspotID { get; set; }
    }

    public class StudyHotspotItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string latitude { get; set; }

        [DataMember]
        public string longitude { get; set; }
    }

    public class AvailableStudyHotspotItem
    {
        [DataMember]
        public string hotspotID { get; set; }

        [DataMember]
        public string ownerEmail { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public double latitude { get; set; }

        [DataMember]
        public double longitude { get; set; }

        public string locationDescription { get; set; }

        [DataMember]
        public string student_count { get; set; }

        [DataMember]
        public double distanceToHotspot { get; set; }
    }

    public class FindStudyHotspotReturnItem
    {
        [DataMember]
        public List<AvailableStudyHotspotItem> studyHotspots { get; set; }
    }

    public class UserHotspotStatusRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class UserHotspotStatusResponseItem
    {
        [DataMember]
        public string hotspotStatus { get; set; }

        [DataMember]
        public AvailableStudyHotspotItem hotspot { get; set; }
    }

    public class StudyHotspotJoinItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string hotspotID { get; set; }
    }

    public class StudyHotspotJoinResponseItem
    {

    }

    public class StudyHotspotLeaveItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class StudyHotspotLeaveRequestItem
    {

    }

    public class StudyHotspotGetMemberItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string hotspotID { get; set; }
    }

    public class StudyHotspotMemberItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }
    }

    public class StudyHotspotResponseItem
    {
        [DataMember]
        public List<StudyHotspotMemberItem> hotspotMembers { get; set; }
    }

    public class StudyHotspotDeleteItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string hotspotID { get; set; }
    }

    public class StudyHotspotDeleteResponseItem
    {

    }

    public class ScheduleTutorItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string dateTime { get; set; }

        [DataMember]
        public string duration { get; set; }
    }

    public class ScheduleTutorResponseItem
    {

    }

    public class FindAllScheduleTutorRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class ScheduleTutorRequestItem
    {
        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string dateTime { get; set; }

        [DataMember]
        public string duration { get; set; }
    }

    public class FindAllScheduleTutorResponseItem
    {
        [DataMember]
        public List<ScheduleTutorRequestItem> tutorRequestItems { get; set; }
    }

    public class AcceptStudentScheduleRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class AcceptStudentScheduleRequestResponseItem
    {
        [DataMember]
        public string student_email { get; set; }

        [DataMember]
        public string tutor_email { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string dateTime { get; set; }

        [DataMember]
        public string duration { get; set; }
    }

    public class PairedScheduledStatusItem
    {
        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string dateTime { get; set; }

        [DataMember]
        public string duration { get; set; }

        [DataMember]
        public Boolean isPaired { get; set; }
    }

    public class CheckPairedStatusResponseItem
    {
        [DataMember]
        public List<PairedScheduledStatusItem> requests { get; set; }
    }

    public class FindAllScheduleTutorAcceptedRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }
    }

    public class FindAllScheduleTutorAcceptedItem
    {
        [DataMember]
        public string studentEmail { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string topic { get; set; }

        [DataMember]
        public string dateTime { get; set; }

        [DataMember]
        public string duration { get; set; }
    }

    public class FindAllScheduleTutorAcceptedResponsetItem
    {
        [DataMember]
        public List<FindAllScheduleTutorAcceptedItem> tutorRequestItems { get; set; }
    }

    public class StartScheduledTutorSessionTutorItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string dateTime { get; set; }
    }

    public class StartScheduledTutorSessionTutorResponseItem
    {

    }

    public class StartScheduledTutorSessionStudentItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string dateTime { get; set; }
    }

    public class StartScheduledTutorSessionStudentResponseItem
    {

    }

    public class ReportTutorGetTutorListRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class ReportTutorGetTutorListItem
    {
        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string tutorFirstName { get; set; }

        [DataMember]
        public string tutorLastName { get; set; }
    }

    public class ReportTutorGetTutorListResponseItem
    {
        [DataMember]
        public List<ReportTutorGetTutorListItem> tutorList { get; set; }
    }

    public class ReportTutorGetSessionListRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string tutorFirstName { get; set; }

        [DataMember]
        public string tutorLastName { get; set; }
    }

    public class ReportTutorGetSessionListItem
    {
        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string tutorFirstName { get; set; }

        [DataMember]
        public string tutorLastName { get; set; }

        [DataMember]
        public string tutorSessionID { get; set; }

        [DataMember]
        public string course { get; set; }

        [DataMember]
        public string sessionStartTime { get; set; }

        [DataMember]
        public string sessionEndTime { get; set; }

        [DataMember]
        public string sessionCost { get; set; }
    }

    public class ReportTutorGetSessionListResponseItem
    {
        [DataMember]
        public List<ReportTutorGetSessionListItem> tutorList { get; set; }
    }

    public class ReportTutorRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string tutorEmail { get; set; }

        [DataMember]
        public string tutorSessionID { get; set; }

        [DataMember]
        public string message { get; set; }
    }

    public class ReportTutorResponseItem
    {

    }


    public class SendMessageRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string recipientEmail { get; set; }

        [DataMember]
        public string message { get; set; }
    }

    public class SendMessageResponseItem
    {

    }

    public class GetMessageConversationRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }

        [DataMember]
        public string recipientEmail { get; set; }
    }

    public class MessageItem
    {
        [DataMember]
        public string messageID { get; set; }

        [DataMember]
        public string toEmail { get; set; }

        [DataMember]
        public string fromEmail { get; set; }

        [DataMember]
        public string time { get; set; }

        [DataMember]
        public string message { get; set; }
    }

    public class GetMessageConversationResponseItem
    {
        [DataMember]
        public List<MessageItem> messages { get; set; }
    }

    public class GetUsersRequestItem
    {
        [DataMember]
        public string userEmail { get; set; }

        [DataMember]
        public string userToken { get; set; }
    }

    public class UserMessagingItem
    {
        [DataMember]
        public string email { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }
    }

    public class GetUsersResponseItem
    {
        [DataMember]
        public List<UserMessagingItem> users { get; set; }
    }











    public class PayPalTestResponseItem
    {
        [DataMember]
        public string string1 { get; set; }

        [DataMember]
        public string string2 { get; set; }
    }
}

