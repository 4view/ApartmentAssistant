namespace ApartmentAssistant.Core.DTO;

public class TenementIndicationDTO
{
    public decimal BathroomHotWater { get; set; }

    public decimal BathroomColdWater { get; set; }

    public decimal KitchenHotWater { get; set; }

    public decimal KitchenColdWater { get; set; }

    public DateTimeOffset ContributionDate { get; set; }
}