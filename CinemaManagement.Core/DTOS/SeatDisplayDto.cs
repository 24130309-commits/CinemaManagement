using CinemaManagement.Core.Enums;
namespace CinemaManagement.Core.DTOS;
public class SeatDisplayDto
{
  public int Id  { get; set; }
  public string RowName  { get; set; }
  public int SeatNumber  { get; set; }
  public SeatStatus Status   { get; set; } 
  public decimal Price     { get; set; } 
}