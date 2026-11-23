
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private InMemoryUserRepository _users = null!;
    private CurrentUserSession _session = null!;
    private AuthService _auth = null!;

    [SetUp]
    public void SetUp()
    {
        _users = new InMemoryUserRepository();
        _session = new CurrentUserSession();

        _auth = new AuthService(
            _users,
            _session,
            new UserValidator());

        _users.Insert(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = _auth.HashPassword("ValidPass123"),
            FullName = "Quản trị viên",
            Role = UserRole.Admin,
            IsActive = true
        });
    }

    [Test]
    public void Login_WithCorrectCredentials_ReturnsUser()
    {
        var result = _auth.Login(
            "admin",
            "ValidPass123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Username, Is.EqualTo("admin"));
        Assert.That(result.Role, Is.EqualTo(UserRole.Admin));
        Assert.That(_session.User, Is.Not.Null);
    }

    [Test]
    public void Login_WithWrongPassword_ThrowsBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            _auth.Login(
                "admin",
                "WrongPass123"));

        Assert.That(_session.User, Is.Null);
    }

    [Test]
    public void Login_WithUnknownUsername_ThrowsBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            _auth.Login(
                "unknown",
                "ValidPass123"));

        Assert.That(_session.User, Is.Null);
    }

    [Test]
    public void Login_WithInactiveAccount_ThrowsBusinessException()
    {
        _users.SetActive(1, false);

        Assert.Throws<BusinessException>(() =>
            _auth.Login(
                "admin",
                "ValidPass123"));

        Assert.That(_session.User, Is.Null);
    }

    [Test]
    public void Login_AfterFiveFailedAttempts_ThrowsBusinessException()
    {
        for (var i = 0; i < 5; i++)
        {
            Assert.Throws<BusinessException>(() =>
                _auth.Login(
                    "admin",
                    "WrongPass123"));
        }
    }

    [Test]
    public void Login_WhenAccountIsLocked_ThrowsBusinessException()
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                _auth.Login(
                    "admin",
                    "WrongPass123");
            }
            catch (BusinessException)
            {
            }
        }

        Assert.Throws<BusinessException>(() =>
            _auth.Login(
                "admin",
                "ValidPass123"));
    }

    [Test]
    public void Login_AfterSuccessfulLogin_ClearsFailedAttempts()
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.Throws<BusinessException>(() =>
                _auth.Login(
                    "admin",
                    "WrongPass123"));
        }

        var result = _auth.Login(
            "admin",
            "ValidPass123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));

        _auth.Logout();

        for (var i = 0; i < 4; i++)
        {
            Assert.Throws<BusinessException>(() =>
                _auth.Login(
                    "admin",
                    "WrongPass123"));
        }
    }

    [Test]
    public void ChangePassword_WithCorrectOldPassword_UpdatesPassword()
    {
        Assert.DoesNotThrow(() =>
            _auth.ChangePassword(
                1,
                "ValidPass123",
                "NewPassword456"));

        var user = _users.GetById(1);

        Assert.That(user, Is.Not.Null);
        Assert.That(
            _auth.VerifyPassword(
                "NewPassword456",
                user!.PasswordHash),
            Is.True);
    }

    [Test]
    public void ChangePassword_WithWrongOldPassword_ThrowsBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            _auth.ChangePassword(
                1,
                "WrongPass123",
                "NewPassword456"));
    }

    [Test]
    public void ChangePassword_WithWeakNewPassword_ThrowsBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            _auth.ChangePassword(
                1,
                "ValidPass123",
                "123"));
    }

    [Test]
    public void ChangePassword_WithSamePassword_ThrowsBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            _auth.ChangePassword(
                1,
                "ValidPass123",
                "ValidPass123"));
    }

    [Test]
    public void Logout_ClearsCurrentSession()
    {
        _auth.Login(
            "admin",
            "ValidPass123");

        Assert.That(_session.User, Is.Not.Null);

        _auth.Logout();

        Assert.That(_session.User, Is.Null);
        Assert.That(_session.IsAuthenticated, Is.False);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _items = [];

        private int _nextId = 1;

        public User? GetById(int id)
        {
            return _items.TryGetValue(id, out var user)
                ? user
                : null;
        }

        public IEnumerable<User> GetAll()
        {
            return _items.Values.ToList();
        }

        public int Insert(User entity)
        {
            var id = entity.Id == 0
                ? _nextId++
                : entity.Id;

            entity.Id = id;

            _nextId = Math.Max(
                _nextId,
                id + 1);

            _items[id] = entity;

            return id;
        }

        public bool Update(User entity)
        {
            if (!_items.ContainsKey(entity.Id))
                return false;

            _items[entity.Id] = entity;

            return true;
        }

        public bool Delete(int id)
        {
            return _items.Remove(id);
        }

        public User? GetByUsername(string username)
        {
            return _items.Values.FirstOrDefault(x =>
                string.Equals(
                    x.Username,
                    username,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool UpdatePassword(
            int id,
            string newHash)
        {
            if (!_items.TryGetValue(id, out var user))
                return false;

            user.PasswordHash = newHash;

            return true;
        }

        public bool SetActive(
            int id,
            bool isActive)
        {
            if (!_items.TryGetValue(id, out var user))
                return false;

            user.IsActive = isActive;

            return true;
        }
    }
}
