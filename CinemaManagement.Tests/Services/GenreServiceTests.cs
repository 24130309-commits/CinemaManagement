using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class GenreServiceTests
{
    [Test]
    public void Create_RejectsStaffWithoutWriting()
    {
        var repository = new FakeGenreRepository();
        var service = new GenreService(repository, Session(UserRole.Staff));

        Assert.Throws<BusinessException>(() =>
            service.Create(new Genre { Name = "Drama" }));
        Assert.That(repository.WriteCount, Is.Zero);
    }

    [Test]
    public void Create_TrimsNameAndAssignsRepositoryId()
    {
        var repository = new FakeGenreRepository();
        var service = new GenreService(repository, Session(UserRole.Admin));
        var genre = new Genre { Name = "  Drama  " };

        service.Create(genre);

        Assert.That(genre.Id, Is.EqualTo(1));
        Assert.That(genre.Name, Is.EqualTo("Drama"));
        Assert.That(repository.Items.Single().Name, Is.EqualTo("Drama"));
    }

    private static CurrentUserSession Session(UserRole role)
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 4, Username = "tester", Role = role });
        return session;
    }

    private sealed class FakeGenreRepository : IGenreRepository
    {
        public List<Genre> Items { get; } = [];
        public int WriteCount { get; private set; }

        public Genre? GetById(int id) => Items.FirstOrDefault(x => x.Id == id);
        public IEnumerable<Genre> GetAll() => Items;

        public int Insert(Genre entity)
        {
            WriteCount++;
            entity.Id = Items.Count + 1;
            Items.Add(entity);
            return entity.Id;
        }

        public bool Update(Genre entity)
        {
            WriteCount++;
            var index = Items.FindIndex(x => x.Id == entity.Id);
            if (index < 0) return false;
            Items[index] = entity;
            return true;
        }

        public bool Delete(int id)
        {
            WriteCount++;
            return Items.RemoveAll(x => x.Id == id) > 0;
        }

        public bool IsNameExists(string name, int? excludeId = null) =>
            Items.Any(x => x.Id != excludeId &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public bool IsInUse(int genreId) => false;
    }
}
