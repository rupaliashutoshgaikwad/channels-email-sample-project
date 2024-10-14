using MimeKit;
using System.Collections.Generic;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IEmailAuditService
    {
        void LogCustomSmtpTestEmailStart(string emailId);
        void LogSendEmailFromStorageStart(string emailId);
        void LogMimeBuilderLambdaFailure(string emailId, string response);
        void LogSendRoutedEmailStart(string emailId, int businessUnit, List<string> tos, List<string> froms, string contactId, string fileServerVip);
        void LogSendRoutedEmailSuccess(string emailId, int businessUnit, List<string> tos, List<string> froms, string contactId, string fileServerVip);
        void LogPutToCloudStorageQueueResult(string emailId, bool putToCloudStorageQueueSuccessful);
        void LogPutToLogEmailQueueResult(string emailId, bool putToLogEmailQueueSuccessful);
        void LogSendSesStart(InternetAddressList to, InternetAddressList from, string emailId, string messageId, bool enforceTLS);
        void LogSendKerioStart(InternetAddressList to, InternetAddressList from, string emailId);
        void LogSendCustomSmtpStart(InternetAddressList to, InternetAddressList from, string host, int port, string emailId);
        void LogPutEmailToUnsendablesSuccess(string emailId, int businessUnit, string contactId);
    }
}
