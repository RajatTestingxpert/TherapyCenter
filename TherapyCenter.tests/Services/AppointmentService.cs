using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TherapyCenter.DTO_s.Appointment;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Implementations;
using TherapyCenter.Tests;
using Xunit;

namespace TherapyCenter.tests.Services
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IAppointmentRepository> _appointmentRepoMock;
        private readonly Mock<ISlotRepository> _slotRepoMock;
        private readonly Mock<ITherapyRepository> _therapyRepoMock;
        private readonly AppointmentService _appointmentService;

        public AppointmentServiceTests()
        {
            _appointmentRepoMock = new Mock<IAppointmentRepository>();
            _slotRepoMock = new Mock<ISlotRepository>();
            _therapyRepoMock = new Mock<ITherapyRepository>();

            _appointmentService = new AppointmentService(
                _appointmentRepoMock.Object,
                _slotRepoMock.Object,
                _therapyRepoMock.Object);
        }

        // ── BookAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task BookAsync_WithFreeSlot_CreatesAppointmentAndMarksSlotBooked()
        {
            // Arrange
            var freeSlot = TestHelpers.CreateSlot(1, 1, isBooked: false);
            var therapy = TestHelpers.CreateTherapy(1);

            _slotRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(freeSlot);
            _therapyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(therapy);
            _slotRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Slot>()))
                         .ReturnsAsync((Slot s) => s);
            _appointmentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Appointment>()))
                                .ReturnsAsync((Appointment a) => { a.AppointmentId = 1; return a; });

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 1,
                SlotId = 1,
                ReceptionistId = 3,
                Notes = "First session"
            };

            // Act
            var result = await _appointmentService.BookAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.PatientId.Should().Be(1);
            result.DoctorId.Should().Be(1);
            result.Status.Should().Be("Scheduled");
            result.AppointmentDate.Should().Be(freeSlot.Date);
            result.StartTime.Should().Be(freeSlot.StartTime);

            // Slot must be marked booked
            _slotRepoMock.Verify(r =>
                r.UpdateAsync(It.Is<Slot>(s => s.IsBooked == true)), Times.Once);
        }

        [Fact]
        public async Task BookAsync_WithAlreadyBookedSlot_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookedSlot = TestHelpers.CreateSlot(1, 1, isBooked: true);
            _slotRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bookedSlot);

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 1,
                SlotId = 1,
                Notes = "Notes"
            };

            // Act
            var act = async () => await _appointmentService.BookAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*already booked*");

            // Appointment must NEVER be created
            _appointmentRepoMock.Verify(r =>
                r.CreateAsync(It.IsAny<Appointment>()), Times.Never);
        }

        [Fact]
        public async Task BookAsync_WithNonExistentSlot_ThrowsKeyNotFoundException()
        {
            // Arrange
            _slotRepoMock.Setup(r => r.GetByIdAsync(999))
                         .ReturnsAsync((Slot?)null);

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 1,
                SlotId = 999
            };

            // Act
            var act = async () => await _appointmentService.BookAsync(request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*Slot not found*");
        }

        [Fact]
        public async Task BookAsync_WithNonExistentTherapy_ThrowsKeyNotFoundException()
        {
            // Arrange
            var freeSlot = TestHelpers.CreateSlot(1, 1, isBooked: false);
            _slotRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(freeSlot);
            _therapyRepoMock.Setup(r => r.GetByIdAsync(999))
                            .ReturnsAsync((Therapy?)null);

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 999,
                SlotId = 1
            };

            // Act
            var act = async () => await _appointmentService.BookAsync(request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*Therapy not found*");
        }

        [Fact]
        public async Task BookAsync_OnlineBooking_ReceptionistIdIsNull()
        {
            // Arrange
            var freeSlot = TestHelpers.CreateSlot(1, 1, isBooked: false);
            var therapy = TestHelpers.CreateTherapy(1);

            _slotRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(freeSlot);
            _therapyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(therapy);
            _slotRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Slot>())).ReturnsAsync(freeSlot);

            Appointment? capturedAppointment = null;
            _appointmentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Appointment>()))
                                .Callback<Appointment>(a => capturedAppointment = a)
                                .ReturnsAsync((Appointment a) => a);

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 1,
                SlotId = 1,
                ReceptionistId = null,
                Notes = "Online booking"
            };

            // Act
            await _appointmentService.BookAsync(request);

            // Assert
            capturedAppointment.Should().NotBeNull();
            capturedAppointment!.ReceptionistId.Should().BeNull();
        }

        [Fact]
        public async Task BookAsync_AppointmentUsesSlotDateAndTime()
        {
            // Arrange — appointment times must come from the slot, not the caller
            var slot = new Slot
            {
                SlotId = 1,
                DoctorId = 1,
                Date = new DateOnly(2025, 7, 7),
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                IsBooked = false
            };

            _slotRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(slot);
            _therapyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestHelpers.CreateTherapy());
            _slotRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Slot>())).ReturnsAsync(slot);

            Appointment? saved = null;
            _appointmentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Appointment>()))
                                .Callback<Appointment>(a => saved = a)
                                .ReturnsAsync((Appointment a) => a);

            var request = new BookAppointmentRequest
            {
                PatientId = 1,
                DoctorId = 1,
                TherapyId = 1,
                SlotId = 1,
                Notes = "Notes"
            };

            // Act
            await _appointmentService.BookAsync(request);

            // Assert
            saved!.AppointmentDate.Should().Be(new DateOnly(2025, 7, 7));
            saved.StartTime.Should().Be(new TimeOnly(14, 0));
            saved.EndTime.Should().Be(new TimeOnly(15, 0));
        }

        // ── UpdateStatusAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_WithValidAppointment_UpdatesStatus()
        {
            // Arrange
            var appointment = TestHelpers.CreateAppointment(status: "Scheduled");

            _appointmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);
            _appointmentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
                                .ReturnsAsync((Appointment a) => a);

            var request = new UpdateAppointmentStatusRequest
            {
                Status = "Completed",
                Notes = "Session done"
            };

            // Act
            var result = await _appointmentService.UpdateStatusAsync(1, request);

            // Assert
            result.Status.Should().Be("Completed");
            result.Notes.Should().Be("Session done");
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentAppointment_ThrowsKeyNotFoundException()
        {
            // Arrange
            _appointmentRepoMock.Setup(r => r.GetByIdAsync(999))
                                .ReturnsAsync((Appointment?)null);

            var request = new UpdateAppointmentStatusRequest { Status = "Completed" };

            // Act
            var act = async () => await _appointmentService.UpdateStatusAsync(999, request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*Appointment not found*");
        }

        [Theory]
        [InlineData("Scheduled")]
        [InlineData("Completed")]
        [InlineData("Cancelled")]
        public async Task UpdateStatusAsync_WithAnyValidStatus_UpdatesCorrectly(string status)
        {
            // Arrange
            var appointment = TestHelpers.CreateAppointment();
            _appointmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appointment);
            _appointmentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
                                .ReturnsAsync((Appointment a) => a);

            var request = new UpdateAppointmentStatusRequest { Status = status };

            // Act
            var result = await _appointmentService.UpdateStatusAsync(1, request);

            // Assert
            result.Status.Should().Be(status);
        }

        // ── GetByPatientAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByPatientAsync_ReturnsAllAppointmentsForPatient()
        {
            // Arrange
            var appointments = new List<Appointment>
            {
                TestHelpers.CreateAppointment(1, patientId: 1),
                TestHelpers.CreateAppointment(2, patientId: 1)
            };

            _appointmentRepoMock.Setup(r => r.GetByPatientIdAsync(1))
                                .ReturnsAsync(appointments);

            // Act
            var result = await _appointmentService.GetByPatientAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(a => a.PatientId == 1);
        }

        [Fact]
        public async Task GetByPatientAsync_WithNoAppointments_ReturnsEmptyList()
        {
            // Arrange
            _appointmentRepoMock.Setup(r => r.GetByPatientIdAsync(99))
                                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _appointmentService.GetByPatientAsync(99);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
