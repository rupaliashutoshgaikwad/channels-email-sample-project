using CloudEmail.SampleProject.API.Services.Interface;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class ExceptionService : IExceptionService
    {
        static readonly string[] smtpCommandExceptionList = {
            "domain does not exist",
            "message is too long",
            "your message exceeded google's message size limits"
        };

        /// <summary>
        /// Check valid smtp command exception from list
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public bool ValidateSmtpCommandException(Exception exception)
        {
            if (exception is SmtpCommandException &&
                smtpCommandExceptionList.Any(s => exception.Message.ToLower().Contains(s)))
                return true;



            return false;
        }
    }
}