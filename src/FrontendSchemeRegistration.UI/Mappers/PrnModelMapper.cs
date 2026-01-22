namespace FrontendSchemeRegistration.UI.Mappers;

using Application.Constants;
using Application.DTOs.Prns;
using AutoMapper;
using Constants;
using ViewModels.Prns;

public class PrnModelMapper : Profile
{
    public PrnModelMapper()
    {
        CreateMap<PrnModel, BasePrnViewModel>()
            .ForMember(dest => dest.ObligationYear,
                opt => opt.MapFrom((src, _) => int.TryParse(src.ObligationYear, out var oYear) ? oYear : 0))
            .ForMember(dest => dest.ApprovalStatus,
                opt => opt.MapFrom(src => MapStatus(src.PrnStatus)))
            .ForMember(dest => dest.AvailableAcceptanceYears, opt => opt.MapFrom<PrnAvailableAcceptanceYearsResolver>())
            .ForMember(dest => dest.PrnOrPernNumber,
                opt => opt.MapFrom(src => src.PrnNumber))
            .ForMember(dest => dest.Material,
                opt => opt.MapFrom(src => src.MaterialName))
            .ForMember(dest => dest.DateIssued,
                opt => opt.MapFrom(src => src.IssueDate))
            .ForMember(dest => dest.IsDecemberWaste,
                opt => opt.MapFrom(src => src.DecemberWaste))
            .ForMember(dest => dest.IssuedBy,
                opt => opt.MapFrom(src => src.IssuedByOrg))
            .ForMember(dest => dest.IssuedBy,
                opt => opt.MapFrom(src => src.IssuedByOrg))
            .ForMember(dest => dest.Tonnage,
                opt => opt.MapFrom(src => src.TonnageValue))
            .ForMember(dest => dest.AdditionalNotes,
                opt => opt.MapFrom(src => src.IssuerNotes))
            .ForMember(dest => dest.NoteType,
                opt => opt.MapFrom(src => src.IsExport ? PrnConstants.PernText : PrnConstants.PrnText))
            ;

        CreateMap<PrnModel, PrnViewModel>()
            .IncludeBase<PrnModel, BasePrnViewModel>()
            .ForMember(dest => dest.ReproccessingSiteAddress,
                opt => opt.MapFrom(src => src.ReprocessingSite))
            .ForMember(dest => dest.AuthorisedBy,
                opt => opt.MapFrom(src => src.PrnSignatory))
            .ForMember(dest => dest.NameOfProducerOrComplianceScheme,
                opt => opt.MapFrom(src => src.OrganisationName))
            .ForMember(dest => dest.Position,
                opt => opt.MapFrom(src => src.PrnSignatoryPosition ?? string.Empty))
            .ForMember(dest => dest.RecyclingProcess,
                opt => opt.MapFrom(src => src.ProcessToBeUsed ?? string.Empty))
            .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
            ;

        CreateMap<PrnModel, PrnSearchResultViewModel>()
            .IncludeBase<PrnModel, BasePrnViewModel>()
            ;

        CreateMap<PrnModel, AwaitingAcceptanceResultViewModel>()
            .IncludeBase<PrnModel, BasePrnViewModel>()
            .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
            ;
    }

    private static string MapStatus(string oldStatus)
    {
        return oldStatus switch
        {
            "AWAITINGACCEPTANCE" => PrnStatus.AwaitingAcceptance,
            "CANCELED" => PrnStatus.Cancelled,
            _ => oldStatus
        };
    }
}