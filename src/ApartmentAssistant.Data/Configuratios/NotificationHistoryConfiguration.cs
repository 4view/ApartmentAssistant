namespace ApartmentAssistant.Data.Configurations;

public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistoryEntity>
{
    public void Configure(EntityTypeBuilder<NotificationHistoryEntity> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.NotificationDateTime).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
    }
}
