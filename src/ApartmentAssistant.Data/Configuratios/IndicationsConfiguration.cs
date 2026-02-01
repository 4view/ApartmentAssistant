namespace ApartmentAssistant.Data.Configurations;

public class IndicationsConfiguration : IEntityTypeConfiguration<TenementIndicationEntity>
{
    public void Configure(EntityTypeBuilder<TenementIndicationEntity> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.BathroomColdWater).IsRequired();
        builder.Property(i => i.BathroomHotWater).IsRequired();
        builder.Property(i => i.KitchenColdWater).IsRequired();
        builder.Property(i => i.KitchenHotWater).IsRequired();
    }
}
