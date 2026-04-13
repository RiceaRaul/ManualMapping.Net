namespace ManualMapping.Tests.Models;

public class OrderDto
{
    public int              Id               { get; set; }
    public DateTime         OrderDate        { get; set; }
    public string           CustomerFullName { get; set; } = "";
    public string           CustomerCity     { get; set; } = "";
    public List<OrderLineDto> Lines          { get; set; } = [];
}
