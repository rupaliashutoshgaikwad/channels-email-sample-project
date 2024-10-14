using CloudEmail.Common;

namespace CloudEmail.SampleProject.API.Models.Requests
{
    public class TestCustomSmtpConfigurationRequest
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public TlsOption TlsOption { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string SmtpServerVerificationToken { get; set; }
        public AuthenticationOption AuthenticationOption { get; set; }
        public byte[] CertificateData { get; set; }
    }
}
