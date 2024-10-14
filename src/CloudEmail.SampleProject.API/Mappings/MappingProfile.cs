using AutoMapper;
using CloudEmail.API.Models;
using CloudEmail.Mime.Libraries.Models;

namespace CloudEmail.SampleProject.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MimeWrapper, LibrariesMimeWrapper>();

            CreateMap<WrapperAttachment, LibrariesWrapperAttachment>();
        }
    }
}