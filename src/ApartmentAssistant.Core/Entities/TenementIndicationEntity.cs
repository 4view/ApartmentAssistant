namespace ApartmentAssistant.Core.Entities;

/// <summary>
/// Показания счетчиков
/// </summary>
public class TenementIndicationEntity
{
    public Guid Id { get; set; }

    public long UserId { get; set; }

    public UserEntity? User { get; set; }

    public decimal BathroomHotWater { get; set; }

    public decimal BathroomColdWater { get; set; }

    public decimal KitchenHotWater { get; set; }

    public decimal KitchenColdWater { get; set; }

    public DateTimeOffset ContributionDate { get; set; }
}
