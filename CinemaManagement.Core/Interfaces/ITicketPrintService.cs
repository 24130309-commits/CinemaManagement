namespace CinemaManagement.Core.Interfaces;

public interface ITicketPrintService
{
   string GenerateTicketContent(int bookingId);
   void ExportTicketToPdf(int bookingId, string filepath);
}