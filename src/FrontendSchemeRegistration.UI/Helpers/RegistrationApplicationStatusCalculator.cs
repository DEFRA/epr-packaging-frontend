namespace FrontendSchemeRegistration.UI.Helpers;

using Application.DTOs.Submission;
using Application.Enums;
using FrontendSchemeRegistration.Application.DTOs;
using Sessions;

/// <summary>
/// Centralized calculator for registration application status properties.
/// This ensures consistent status computation logic across RegistrationApplicationSession and view model building.
/// </summary>
public static class RegistrationApplicationStatusCalculator
{
    /// <summary>
    /// Calculates FileUploadStatus based on application status and fee calculation details.
    /// </summary>
    /// <param name="applicationStatus">The current application status</param>
    /// <param name="fileReachedSynapse">Whether the file has reached synapse (has fee calculation details)</param>
    /// <returns>The file upload task list status</returns>
    public static RegistrationTaskListStatus CalculateFileUploadStatus(
        ApplicationStatusType applicationStatus,
        bool fileReachedSynapse)
    {
        if (applicationStatus is
            ApplicationStatusType.CancelledByRegulator
            or ApplicationStatusType.QueriedByRegulator
            or ApplicationStatusType.RejectedByRegulator)
        {
            return RegistrationTaskListStatus.NotStarted;
        }

        if (applicationStatus is ApplicationStatusType.SubmittedToRegulator)
        {
            return fileReachedSynapse ? RegistrationTaskListStatus.Completed : RegistrationTaskListStatus.Pending;
        }

        if (applicationStatus is
            ApplicationStatusType.AcceptedByRegulator
            or ApplicationStatusType.ApprovedByRegulator)
        {
            return RegistrationTaskListStatus.Completed;
        }

        if (!fileReachedSynapse && applicationStatus is
                ApplicationStatusType.FileUploaded or
                ApplicationStatusType.SubmittedAndHasRecentFileUpload)
        {
            return RegistrationTaskListStatus.Pending;
        }

        return RegistrationTaskListStatus.NotStarted;
    }

    /// <summary>
    /// Calculates FileUploadStatus from RegistrationApplicationDetails.
    /// </summary>
    public static RegistrationTaskListStatus CalculateFileUploadStatus(RegistrationApplicationDetails details)
    {
        var fileReachedSynapse = details.RegistrationFeeCalculationDetails is { Length: > 0 };
        return CalculateFileUploadStatus(details.ApplicationStatus, fileReachedSynapse);
    }

    /// <summary>
    /// Calculates PaymentViewStatus based on file upload status and payment method.
    /// </summary>
    /// <param name="fileUploadStatus">The file upload task list status</param>
    /// <param name="isRegistrationFeePaid">Whether the registration fee has been paid</param>
    /// <returns>The payment view task list status</returns>
    public static RegistrationTaskListStatus CalculatePaymentViewStatus(
        RegistrationTaskListStatus fileUploadStatus,
        bool isRegistrationFeePaid)
    {
        if (fileUploadStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Pending)
        {
            return RegistrationTaskListStatus.CanNotStartYet;
        }

        if (fileUploadStatus is RegistrationTaskListStatus.Completed && !isRegistrationFeePaid)
        {
            return RegistrationTaskListStatus.NotStarted;
        }

        if (fileUploadStatus == RegistrationTaskListStatus.Completed && isRegistrationFeePaid)
        {
            return RegistrationTaskListStatus.Completed;
        }

        return RegistrationTaskListStatus.NotStarted;
    }

    /// <summary>
    /// Calculates PaymentViewStatus from RegistrationApplicationDetails.
    /// </summary>
    public static RegistrationTaskListStatus CalculatePaymentViewStatus(
        RegistrationTaskListStatus fileUploadStatus,
        RegistrationApplicationDetails details)
    {
        var isRegistrationFeePaid = IsRegistrationFeePaid(details.RegistrationFeePaymentMethod);
        return CalculatePaymentViewStatus(fileUploadStatus, isRegistrationFeePaid);
    }

    /// <summary>
    /// Calculates AdditionalDetailsStatus based on payment status and submission date.
    /// </summary>
    /// <param name="paymentViewStatus">The payment view task list status</param>
    /// <param name="registrationApplicationSubmitted">Whether the registration application has been submitted</param>
    /// <returns>The additional details task list status</returns>
    public static RegistrationTaskListStatus CalculateAdditionalDetailsStatus(
        RegistrationTaskListStatus paymentViewStatus,
        bool registrationApplicationSubmitted)
    {
        if (paymentViewStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Pending)
        {
            return RegistrationTaskListStatus.CanNotStartYet;
        }

        if (paymentViewStatus is RegistrationTaskListStatus.Completed && !registrationApplicationSubmitted)
        {
            return RegistrationTaskListStatus.NotStarted;
        }

        if (paymentViewStatus == RegistrationTaskListStatus.Completed && registrationApplicationSubmitted)
        {
            return RegistrationTaskListStatus.Completed;
        }

        return RegistrationTaskListStatus.CanNotStartYet;
    }

    /// <summary>
    /// Calculates AdditionalDetailsStatus from RegistrationApplicationDetails.
    /// </summary>
    public static RegistrationTaskListStatus CalculateAdditionalDetailsStatus(
        RegistrationTaskListStatus paymentViewStatus,
        RegistrationApplicationDetails details)
    {
        var registrationApplicationSubmitted = details.RegistrationApplicationSubmittedDate is not null;
        return CalculateAdditionalDetailsStatus(paymentViewStatus, registrationApplicationSubmitted);
    }

    /// <summary>
    /// Determines if the registration fee has been paid based on payment method.
    /// </summary>
    public static bool IsRegistrationFeePaid(string? registrationFeePaymentMethod)
    {
        return registrationFeePaymentMethod is "PayByPhone" or "PayOnline" or "PayByBankTransfer" or "No-Outstanding-Payment";
    }

    /// <summary>
    /// Determines if the file has reached synapse (has fee calculation details).
    /// </summary>
    public static bool FileReachedSynapse(RegistrationFeeCalculationDetails[]? registrationFeeCalculationDetails)
    {
        return registrationFeeCalculationDetails is { Length: > 0 };
    }
}

