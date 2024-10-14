using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IInvokeMimeBuilderLambdaService
    {
        Task<string> InvokeMimeBuilderLambda(string emailId, string sentTimeStampString, int queueReceiveCount);
    }
}
