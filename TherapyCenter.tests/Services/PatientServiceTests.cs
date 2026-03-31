using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TherapyCenter.DTO_s.Patient;
using TherapyCenter.DTO_s.Payment;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Implementations;
using TherapyCenter.Tests;
using Xunit;
namespace TherapyCenter.tests.Services
{
    public class PatientServiceTests
    {
        private readonly Mock<IPatientRepository> _patientRepoMock;
        private readonly PatientService _patientService;

        public PatientServiceTests()
        {
            _patientRepoMock = new Mock<IPatientRepository>();
            _patientService = new PatientService(_patientRepoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ReturnsCreatedPatient()
        {
            // Arrange
            var request = new CreatePatientRequest
            {
                FirstName = "Aarav",
                LastName = "Kumar",
                DateOfBirth = new DateTime(2018, 3, 15),
                Gender = "Male",
                MedicalHistory = "Delayed speech development",
                GuardianId = 4
            };

            _patientRepoMock.Setup(r => r.CreateAsync(It.IsAny<Patient>()))
                            .ReturnsAsync((Patient p) => { p.PatientId = 1; return p; });

            // Act
            var result = await _patientService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be("Aarav");
            result.LastName.Should().Be("Kumar");
            result.GuardianId.Should().Be(4);
            result.PatientId.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_WithNullGuardianId_CreatesWalkInPatient()
        {
            // Arrange
            var request = new CreatePatientRequest
            {
                FirstName = "Rohan",
                LastName = "Patel",
                DateOfBirth = new DateTime(2017, 11, 5),
                Gender = "Male",
                MedicalHistory = "ADHD",
                GuardianId = null
            };

            Patient? saved = null;
            _patientRepoMock.Setup(r => r.CreateAsync(It.IsAny<Patient>()))
                            .Callback<Patient>(p => saved = p)
                            .ReturnsAsync((Patient p) => p);

            // Act
            await _patientService.CreateAsync(request);

            // Assert
            saved.Should().NotBeNull();
            saved!.GuardianId.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingPatient_ReturnsPatient()
        {
            // Arrange
            var patient = TestHelpers.CreatePatient(1);
            _patientRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(patient);

            // Act
            var result = await _patientService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(1);
            result.FirstName.Should().Be("Aarav");
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            _patientRepoMock.Setup(r => r.GetByIdAsync(999))
                            .ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByGuardianAsync_ReturnsOnlyPatientsForThatGuardian()
        {
            // Arrange
            var patients = new List<Patient>
            {
                TestHelpers.CreatePatient(1, guardianId: 4),
                TestHelpers.CreatePatient(2, guardianId: 4)
            };

            _patientRepoMock.Setup(r => r.GetByGuardianIdAsync(4)).ReturnsAsync(patients);

            // Act
            var result = await _patientService.GetByGuardianAsync(4);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.GuardianId == 4);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllPatients()
        {
            // Arrange
            var patients = new List<Patient>
            {
                TestHelpers.CreatePatient(1),
                TestHelpers.CreatePatient(2),
                TestHelpers.CreatePatient(3, guardianId: null)
            };

            _patientRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(patients);

            // Act
            var result = await _patientService.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
        }
    }

    public class DoctorServiceTests
    {
        private readonly Mock<IDoctorRepository> _doctorRepoMock;
        private readonly Mock<ISlotRepository> _slotRepoMock;
        private readonly DoctorService _doctorService;

        public DoctorServiceTests()
        {
            _doctorRepoMock = new Mock<IDoctorRepository>();
            _slotRepoMock = new Mock<ISlotRepository>();
            _doctorService = new DoctorService(_doctorRepoMock.Object, _slotRepoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllDoctors()
        {
            // Arrange
            var doctors = new List<Doctor>
            {
                TestHelpers.CreateDoctor(1, 2),
                TestHelpers.CreateDoctor(2, 6)
            };

            _doctorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(doctors);

            // Act
            var result = await _doctorService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingDoctor_ReturnsDoctor()
        {
            // Arrange
            var doctor = TestHelpers.CreateDoctor(1, 2);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            // Act
            var result = await _doctorService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.DoctorId.Should().Be(1);
            result.Specialization.Should().Be("Speech-Language Pathology");
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            _doctorRepoMock.Setup(r => r.GetByIdAsync(999))
                           .ReturnsAsync((Doctor?)null);

            // Act
            var result = await _doctorService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAvailableSlotsAsync_ReturnsOnlyFreeSlots()
        {
            // Arrange
            var date = new DateOnly(2025, 7, 7);
            var freeSlots = new List<Slot>
            {
                TestHelpers.CreateSlot(3, 1, isBooked: false),
                TestHelpers.CreateSlot(4, 1, isBooked: false),
                TestHelpers.CreateSlot(5, 1, isBooked: false)
            };

            _slotRepoMock.Setup(r => r.GetAvailableSlotsByDoctorAsync(1, date))
                         .ReturnsAsync(freeSlots);

            // Act
            var result = await _doctorService.GetAvailableSlotsAsync(1, date);

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(s => !s.IsBooked);
        }

        [Fact]
        public async Task GetAvailableSlotsAsync_WhenAllSlotsBooked_ReturnsEmptyList()
        {
            // Arrange
            var date = new DateOnly(2025, 7, 7);
            _slotRepoMock.Setup(r => r.GetAvailableSlotsByDoctorAsync(1, date))
                         .ReturnsAsync(new List<Slot>());

            // Act
            var result = await _doctorService.GetAvailableSlotsAsync(1, date);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsCorrectDoctor()
        {
            // Arrange
            var doctor = TestHelpers.CreateDoctor(1, userId: 2);
            _doctorRepoMock.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(doctor);

            // Act
            var result = await _doctorService.GetByUserIdAsync(2);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(2);
        }
    }
}
