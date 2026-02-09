namespace ApartmentAssistant.Core.Entities;

public class UserEntity
{
    public long Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
