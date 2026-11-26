using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    [Test]
    public void GetAllUsers_RejectsUnauthenticatedCallerBeforeRepositoryAccess()
    {
        var repository = new RecordingUserRepository();
        var service = CreateService(repository, new CurrentUserSession());

        Assert.Throws<BusinessException>(() => service.GetAllUsers());
        Assert.That(repository.ReadCount, Is.Zero);
    }

    [Test]
    public void ResetPassword_RejectsStaffCallerBeforeRepositoryAccess()
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 7, Username = "staff", Role = UserRole.Staff });
        var repository = new RecordingUserRepository();
        var service = CreateService(repository, session);

        Assert.Throws<BusinessException>(() => service.ResetPassword(9, "NewPassword123"));
        Assert.That(repository.ReadCount, Is.Zero);
        Assert.That(repository.WriteCount, Is.Zero);
    }

    private static UserService CreateService(
        RecordingUserRepository repository,
        CurrentUserSession session)
    {
        return new UserService(
            repository,
            new FakeAuthService(),
            new UserValidator(),
            session);
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }

        public User? GetById(int id)
        {
            ReadCount++;
            return null;
        }

        public IEnumerable<User> GetAll()
        {
            ReadCount++;
            return Array.Empty<User>();
        }

        public int Insert(User entity)
        {
            WriteCount++;
            return 0;
        }

        public bool Update(User entity)
        {
            WriteCount++;
            return false;
        }

        public bool Delete(int id)
        {
            WriteCount++;
            return false;
        }

        public User? GetByUsername(string username)
        {
            ReadCount++;
            return null;
        }

        public bool UpdatePassword(int id, string newHash)
        {
            WriteCount++;
            return false;
        }

        public bool SetActive(int id, bool isActive)
        {
            WriteCount++;
            return false;
        }
    }

    private sealed class FakeAuthService : IAuthService
    {
        public User Login(string username, string password) => throw new NotSupportedException();
        public void Logout() => throw new NotSupportedException();
        public void ChangePassword(int userId, string oldPassword, string newPassword) => throw new NotSupportedException();
        public string HashPassword(string password) => throw new NotSupportedException();
        public bool VerifyPassword(string password, string hash) => throw new NotSupportedException();
    }
}
