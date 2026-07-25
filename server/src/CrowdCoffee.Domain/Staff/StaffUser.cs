namespace CrowdCoffee.Domain.Staff;

public enum StaffRole
{
    Manager,
    Barista
}

public class StaffUser
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;

    /// Sized for BCrypt/Argon2 (Epic 11 decides which); not a plain hash column.
    public string PasswordHash { get; private set; } = string.Empty;
    public StaffRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    private StaffUser()
    {
    }

    public StaffUser(Guid id, string username, string passwordHash, StaffRole role)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
}
