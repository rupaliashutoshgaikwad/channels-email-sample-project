using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudEmail.API.Models.Requests;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IEmailStorageService
    {
        Task PutEmailToUnsendables(SendEmailRequest sendEmailRequest);
    }
}
