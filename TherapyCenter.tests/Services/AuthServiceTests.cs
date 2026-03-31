using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Implementations;
using TherapyCenter.Tests;
using Xunit;
using TherapyCenter.DTO_s.Auth;

namespace TherapyCenter.tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            var config = TestHelpers.CreateJwtConfig();
            _authService = new AuthService(_userRepoMock.Object, config);
        }

        // ── RegisterAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_WithValidGuardianRole_ReturnsAuthResponse()
        {
            // Arrange
            // DTOs are plain classes — use object initializer syntax, not positional constructor
            var request = new RegisterRequest
            {
                FirstName = "Ramesh",
                LastName = "Kumar",
                Email = "ramesh@therapy.com",
                Password = "Guardian@123",
                Role = "Guardian",
                PhoneNumber = "9123456789"
            };

            _userRepoMock.Setup(r => r.EmailExistsAsync(request.Email))
                         .ReturnsAsync(false);

            _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => { u.UserId = 4; return u; });

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("Guardian");
            result.FullName.Should().Be("Ramesh Kumar");
            result.Token.Should().NotBeNullOrWhiteSpace();
            result.UserId.Should().Be(4);
        }

        [Fact]
        public async Task RegisterAsync_WithValidPatientRole_ReturnsAuthResponse()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FirstName = "Meena",
                LastName = "Patel",
                Email = "meena@therapy.com",
                Password = "Patient@123",
                Role = "Patient",
                PhoneNumber = "9000011111"
            };

            _userRepoMock.Setup(r => r.EmailExistsAsync(request.Email))
                         .ReturnsAsync(false);

            _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => { u.UserId = 5; return u; });

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            result.Role.Should().Be("Patient");
            result.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Doctor")]
        [InlineData("Receptionist")]
        public async Task RegisterAsync_WithRestrictedRole_ThrowsInvalidOperationException(string role)
        {
            // Arrange
            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@therapy.com",
                Password = "Test@123",
                Role = role
            };

            // Act
            var act = async () => await _authService.RegisterAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Only Patient or Guardian*");
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FirstName = "Another",
                LastName = "User",
                Email = "ramesh@therapy.com",
                Password = "Pass@123",
                Role = "Guardian"
            };

            _userRepoMock.Setup(r => r.EmailExistsAsync(request.Email))
                         .ReturnsAsync(true);

            // Act
            var act = async () => await _authService.RegisterAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*already registered*");
        }

        // ── LoginAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenAndRole()
        {
            // Arrange
            var hasher = new PasswordHasher<User>();
            var user = TestHelpers.CreateAdminUser();
            user.PasswordHash = hasher.HashPassword(user, "Admin@123");

            _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email))
                         .ReturnsAsync(user);

            var request = new LoginRequest
            {
                Email = user.Email,
                Password = "Admin@123"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrWhiteSpace();
            result.Role.Should().Be("Admin");
            result.UserId.Should().Be(user.UserId);
            result.FullName.Should().Be("Super Admin");
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var hasher = new PasswordHasher<User>();
            var user = TestHelpers.CreateAdminUser();
            user.PasswordHash = hasher.HashPassword(user, "Admin@123");

            _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email))
                         .ReturnsAsync(user);

            var request = new LoginRequest
            {
                Email = user.Email,
                Password = "WrongPassword"
            };

            // Act
            var act = async () => await _authService.LoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                     .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                         .ReturnsAsync((User?)null);

            var request = new LoginRequest
            {
                Email = "nobody@therapy.com",
                Password = "Pass@123"
            };

            // Act
            var act = async () => await _authService.LoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                     .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_GeneratedToken_ContainsCorrectRole()
        {
            // Arrange
            var hasher = new PasswordHasher<User>();
            var user = TestHelpers.CreateDoctorUser();
            user.PasswordHash = hasher.HashPassword(user, "Doctor@123");

            _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email))
                         .ReturnsAsync(user);

            var request = new LoginRequest
            {
                Email = user.Email,
                Password = "Doctor@123"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Role.Should().Be("Doctor");

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);
            jwt.Claims.Should().Contain(c =>
                c.Type == System.Security.Claims.ClaimTypes.Role &&
                c.Value == "Doctor");
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WithDoctorRole_CreatesAccountSuccessfully()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FirstName = "Dr. Rahul",
                LastName = "Mehta",
                Email = "rahul@therapy.com",
                Password = "Doctor@123",
                Role = "Doctor",
                PhoneNumber = "9001122334"
            };

            _userRepoMock.Setup(r => r.EmailExistsAsync(request.Email))
                         .ReturnsAsync(false);

            _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => { u.UserId = 6; return u; });

            // Act
            var result = await _authService.CreateStaffAccountAsync(request);

            // Assert
            result.Role.Should().Be("Doctor");
            result.Token.Should().NotBeNullOrWhiteSpace();
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
        }
    }
}
