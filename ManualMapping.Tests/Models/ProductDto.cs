namespace ManualMapping.Tests.Models;

public class ProductDto
{
    public int    Id          { get; set; }
    public string DisplayName { get; set; } = "";
    public decimal Price      { get; set; }
    public string TagsSummary { get; set; } = "";
}
