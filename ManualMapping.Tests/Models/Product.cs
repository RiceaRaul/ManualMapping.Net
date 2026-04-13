namespace ManualMapping.Tests.Models;

public class Product
{
    public int         Id       { get; set; }
    public string      Name     { get; set; } = "";
    public string      Category { get; set; } = "";
    public decimal     Price    { get; set; }
    public List<string> Tags    { get; set; } = [];

    // Navigation to Category entity (optional — used by Order/OrderLine tests)
    public int?      CategoryId     { get; set; }
    public Category? ProductCategory { get; set; }
}
