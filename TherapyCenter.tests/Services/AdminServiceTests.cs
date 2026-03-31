using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TherapyCenter.DTO_s.Admin;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Implementations;
using TherapyCenter.Tests;
using Xunit;
namespace TherapyCenter.tests.Services
{
    public class AdminServiceTests
    {
        private readonly Mock<ITherapyRepository> _therapyRepoMock;
        private readonly Mock<IDoctorRepository> _doctorRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ISlotRepository> _slotRepoMock;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            _therapyRepoMock = new Mock<ITherapyRepository>();
            _doctorRepoMock = new Mock<IDoctorRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _slotRepoMock = new Mock<ISlotRepository>();

            _adminService = new AdminService(
                _therapyRepoMock.Object,
                _doctorRepoMock.Object,
                _userRepoMock.Object,
                _slotRepoMock.Object);
        }

        // ── CreateTherapyAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task CreateTherapyAsync_WithValidRequest_ReturnsCreatedTherapy()
        {
            // Arrange
            var request = new CreateTherapyRequest
            {
                Name = "Speech Therapy",
                Description = "Helps children with speech disorders",
                DurationMinutes = 60,
                Cost = 1500.00m
            };

            _therapyRepoMock.Setup(r => r.CreateAsync(It.IsAny<Therapy>()))
                            .ReturnsAsync((Therapy t) => { t.TherapyId = 1; return t; });

            // Act
            var result = await _adminService.CreateTherapyAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Speech Therapy");
            result.DurationMinutes.Should().Be(60);
            result.Cost.Should().Be(1500.00m);
            result.TherapyId.Should().Be(1);
        }

        [Fact]
        public async Task CreateTherapyAsync_CallsRepositoryCreateOnce()
        {
            // Arrange
            var request = new CreateTherapyRequest
            {
                Name = "Test Therapy",
                DurationMinutes = 30,
                Cost = 500m
            };

            _therapyRepoMock.Setup(r => r.CreateAsync(It.IsAny<Therapy>()))
                            .ReturnsAsync(TestHelpers.CreateTherapy());

            // Act
            await _adminService.CreateTherapyAsync(request);

            // Assert
            _therapyRepoMock.Verify(r => r.CreateAsync(It.IsAny<Therapy>()), Times.Once);
        }

        // ── UpdateTherapyAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTherapyAsync_WithExistingTherapy_UpdatesFields()
        {
            // Arrange
            var existing = TestHelpers.CreateTherapy(1);

            _therapyRepoMock.Setup(r => r.GetByIdAsync(1))
                            .ReturnsAsync(existing);

            _therapyRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Therapy>()))
                            .ReturnsAsync((Therapy t) => t);

            var updateRequest = new UpdateTherapyRequest
            {
                Name = "Speech Therapy Updated",
                Description = "New description",
                DurationMinutes = 45,
                Cost = 1800.00m
            };

            // Act
            var result = await _adminService.UpdateTherapyAsync(1, updateRequest);

            // Assert
            result.Name.Should().Be("Speech Therapy Updated");
            result.DurationMinutes.Should().Be(45);
            result.Cost.Should().Be(1800.00m);
        }

        [Fact]
        public async Task UpdateTherapyAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _therapyRepoMock.Setup(r => r.GetByIdAsync(999))
                            .ReturnsAsync((Therapy?)null);

            var request = new UpdateTherapyRequest
            {
                Name = "X",
                DurationMinutes = 30,
                Cost = 100m
            };

            // Act
            var act = async () => await _adminService.UpdateTherapyAsync(999, request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*999*");
        }

        // ── CreateDoctorProfileAsync ───────────────────────────────────────────

        [Fact]
        public async Task CreateDoctorProfileAsync_WithValidDoctorUser_CreatesProfile()
        {
            // Arrange
            var doctorUser = TestHelpers.CreateDoctorUser(2);

            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(doctorUser);

            _doctorRepoMock.Setup(r => r.CreateAsync(It.IsAny<Doctor>()))
                           .ReturnsAsync((Doctor d) => { d.DoctorId = 1; return d; });

            var request = new CreateDoctorProfileRequest
            {
                UserId = 2,
                Specialization = "Speech-Language Pathology",
                Bio = "10 years exp",
                AvailableDays = "Mon,Tue,Wed,Thu,Fri",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            };

            // Act
            var result = await _adminService.CreateDoctorProfileAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(2);
            result.Specialization.Should().Be("Speech-Language Pathology");
            result.DoctorId.Should().Be(1);
        }

        [Fact]
        public async Task CreateDoctorProfileAsync_WithNonDoctorRole_ThrowsInvalidOperationException()
        {
            // Arrange
            var receptionist = TestHelpers.CreateReceptionistUser(3);
            _userRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(receptionist);

            var request = new CreateDoctorProfileRequest
            {
                UserId = 3,
                Specialization = "Something"
            };

            // Act
            var act = async () => await _adminService.CreateDoctorProfileAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Doctor role*");
        }

        [Fact]
        public async Task CreateDoctorProfileAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetByIdAsync(999))
                         .ReturnsAsync((User?)null);

            var request = new CreateDoctorProfileRequest { UserId = 999 };

            // Act
            var act = async () => await _adminService.CreateDoctorProfileAsync(request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*User not found*");
        }

        // ── GenerateSlotsForDoctorAsync ────────────────────────────────────────

        [Fact]
        public async Task GenerateSlotsForDoctorAsync_ForOneWeek_GeneratesCorrectSlotCount()
        {
            // Arrange — Mon-Fri, 9am-5pm = 8 slots/day x 5 days = 40 slots
            var doctor = TestHelpers.CreateDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);
            _slotRepoMock.Setup(r => r.BulkCreateAsync(It.IsAny<IEnumerable<Slot>>()))
                         .Returns(Task.CompletedTask);

            var request = new GenerateSlotsRequest
            {
                DoctorId = 1,
                FromDate = new DateOnly(2025, 7, 7),
                ToDate = new DateOnly(2025, 7, 11)
            };

            // Act
            var count = await _adminService.GenerateSlotsForDoctorAsync(request);

            // Assert
            count.Should().Be(40);
            _slotRepoMock.Verify(r =>
                r.BulkCreateAsync(It.Is<IEnumerable<Slot>>(s => s.Count() == 40)),
                Times.Once);
        }

        [Fact]
        public async Task GenerateSlotsForDoctorAsync_SkipsWeekends()
        {
            // Arrange — Mon to Sun = 7 days but only 5 working days = 40 slots
            var doctor = TestHelpers.CreateDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);
            _slotRepoMock.Setup(r => r.BulkCreateAsync(It.IsAny<IEnumerable<Slot>>()))
                         .Returns(Task.CompletedTask);

            var request = new GenerateSlotsRequest
            {
                DoctorId = 1,
                FromDate = new DateOnly(2025, 7, 7),
                ToDate = new DateOnly(2025, 7, 13)
            };

            // Act
            var count = await _adminService.GenerateSlotsForDoctorAsync(request);

            // Assert
            count.Should().Be(40);
        }

        [Fact]
        public async Task GenerateSlotsForDoctorAsync_WithNoWorkingHours_ThrowsInvalidOperationException()
        {
            // Arrange
            var doctor = TestHelpers.CreateDoctor();
            doctor.StartTime = null;
            doctor.EndTime = null;

            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            var request = new GenerateSlotsRequest
            {
                DoctorId = 1,
                FromDate = new DateOnly(2025, 7, 7),
                ToDate = new DateOnly(2025, 7, 11)
            };

            // Act
            var act = async () => await _adminService.GenerateSlotsForDoctorAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*working hours*");
        }

        [Fact]
        public async Task GenerateSlotsForDoctorAsync_WithNonExistentDoctor_ThrowsKeyNotFoundException()
        {
            // Arrange
            _doctorRepoMock.Setup(r => r.GetByIdAsync(999))
                           .ReturnsAsync((Doctor?)null);

            var request = new GenerateSlotsRequest
            {
                DoctorId = 999,
                FromDate = new DateOnly(2025, 7, 7),
                ToDate = new DateOnly(2025, 7, 11)
            };

            // Act
            var act = async () => await _adminService.GenerateSlotsForDoctorAsync(request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*Doctor not found*");
        }
    }
}
