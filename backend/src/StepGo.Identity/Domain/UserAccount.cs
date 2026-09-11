using StepGo.Shared.Domain;

namespace StepGo.Identity.Domain;

public sealed class UserAccount : AggregateRoot<Guid>
{
    public string FullName { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public Role Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private UserAccount(Guid id, string fullName, string phoneNumber, string? email, Role role, DateTimeOffset createdAt)
        : base(id)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Email = email;
        Role = role;
        CreatedAt = createdAt;
    }

    /// <summary>Phone number is mandatory for registration; email is optional.</summary>
    public static UserAccount Register(Guid id, string fullName, string phoneNumber, string? email, Role role, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("phone_number_required", "手機號碼為必填欄位。");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("full_name_required", "姓名為必填欄位。");
        }

        return new UserAccount(id, fullName.Trim(), phoneNumber.Trim(), string.IsNullOrWhiteSpace(email) ? null : email.Trim(), role, now);
    }

    public static UserAccount Rehydrate(Guid id, string fullName, string phoneNumber, string? email, Role role, DateTimeOffset createdAt)
        => new(id, fullName, phoneNumber, email, role, createdAt);
}
