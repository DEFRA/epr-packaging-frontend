using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

public class PrnMaterialObligationViewModel
{
	public Guid OrganisationId { get; set; }

	public MaterialType MaterialName { get; set; }

	public int? ObligationToMeet { get; set; }

	public int TonnageAwaitingAcceptance { get; set; }

	public int TonnageAccepted { get; set; }

	public int? TonnageOutstanding { get; set; }

	public ObligationStatus Status { get; set; }

	public static string MaterialNameResource(MaterialType material) => material switch
	{
		MaterialType.Aluminium => "aluminium",
		MaterialType.Glass => "glass",
		MaterialType.Paper => "paper_board_fibre",
		MaterialType.Plastic => "plastic",
		MaterialType.Steel => "steel",
		MaterialType.Wood => "wood",
		MaterialType.GlassRemelt => "glass_remelt",
		MaterialType.RemainingGlass => "remaining_glass",
		_ => "totals"
	};

	// separate glass and non-glass
	public static string MaterialCategoryResource(MaterialType material) => material switch
	{
		MaterialType.Glass => MaterialNameResource(MaterialType.Glass),
		MaterialType.GlassRemelt => MaterialNameResource(MaterialType.Glass),
		MaterialType.RemainingGlass => MaterialNameResource(MaterialType.Glass),
		_ => MaterialNameResource(material)
	};

	public string StatusResource => Status switch
	{
		ObligationStatus.NoDataYet => "no_data_yet",
		ObligationStatus.NotMet => "not_met",
		ObligationStatus.Met => "met",
		_ => "no_data_yet"
	};

	public string StatusDisplayCssColor => Status switch
	{
		ObligationStatus.NoDataYet => "grey",
		ObligationStatus.NotMet => "yellow",
		ObligationStatus.Met => "green",
		_ => "grey"
	};

	public double Tonnage { get; set; }

	public double MaterialTarget { get; set; }

	public double MaterialTargetPercentage { get; set; }

	public static implicit operator PrnMaterialObligationViewModel(PrnMaterialObligationModel model)
	{
		return new PrnMaterialObligationViewModel
		{
			OrganisationId = model.OrganisationId,
			MaterialName = Enum.Parse<MaterialType>(model.MaterialName),
			ObligationToMeet = model.ObligationToMeet,
			TonnageAwaitingAcceptance = model.TonnageAwaitingAcceptance,
			TonnageAccepted = model.TonnageAccepted,
			TonnageOutstanding = model.TonnageOutstanding,
			Status = Enum.Parse<ObligationStatus>(model.Status),
			Tonnage = model.Tonnage,
			MaterialTarget = model.MaterialTarget,
			MaterialTargetPercentage = model.MaterialTarget * 100
		};
	}
}
