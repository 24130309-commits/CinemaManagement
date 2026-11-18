namespace CinemaManagement.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Tier { get; set; } = "Standard";
}