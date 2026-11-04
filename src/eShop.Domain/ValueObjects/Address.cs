namespace eShop.Domain.ValueObjects;

/// <summary>
/// Value Object (Đối tượng Giá trị) đại diện cho Địa chỉ giao hàng.
/// Đặc tính DDD: Bất biến (Immutable), không có Id riêng, hai địa chỉ bằng nhau nếu tất cả thuộc tính bằng nhau.
/// </summary>
public record Address
{
    public string Street { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ZipCode { get; init; }
    public string Country { get; init; } = "Vietnam";

    // Constructor rỗng cần thiết cho EF Core
    public Address() { }

    public Address(string street, string district, string city, string? zipCode = null, string country = "Vietnam")
    {
        Street = street;
        District = district;
        City = city;
        ZipCode = zipCode;
        Country = country;
    }

    public override string ToString() => $"{Street}, {District}, {City}, {Country}";
}

