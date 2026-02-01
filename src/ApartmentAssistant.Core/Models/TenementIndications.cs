namespace ApartmentAssistant.Core.Models;

/// <summary>
/// Показания счетчиков
/// </summary>
public class TenementInidications
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public required User user { get; set; }

    public decimal BathroomHotWater { get; set; }

    public decimal BathroomColdWater { get; set; }

    public decimal KitchenHotWater { get; set; }

    public decimal KitchenColdWater { get; set; }

    public DateTimeOffset ContributionDate { get; set; }
}
