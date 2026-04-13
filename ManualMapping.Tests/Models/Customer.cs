namespace ManualMapping.Tests.Models;

public class Customer
{
    public int      Id        { get; set; }
    public string   FirstName { get; set; } = "";
    public string   LastName  { get; set; } = "";
    public string   Email     { get; set; } = "";
    public int?     AddressId { get; set; }
    public Address? Address   { get; set; }
}
