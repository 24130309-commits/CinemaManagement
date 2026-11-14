namespace CinemaManagement.Core.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TotalRows { get; set; }
        public int SeatsPerRow { get; set; }
        public string RoomType { get; set; } // VD: 2D, 3D, IMAX
        public bool IsActive { get; set; }
    }
}