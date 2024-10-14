using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IExceptionService
    {
        /// <summary>
        /// Check valid smtp command exception from list
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        bool ValidateSmtpCommandException(Exception e);
    }
}
