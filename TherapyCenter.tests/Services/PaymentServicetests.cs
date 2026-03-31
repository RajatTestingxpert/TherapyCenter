using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TherapyCenter.DTO_s.Payment;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Implementations;
using Xunit;

namespace TherapyCenter.tests.Services
{
    public class PaymentServiceTests
    {
        // These fields MUST be declared here — the errors said they didn't exist
        // because the constructor content was missing when copied
        private readonly Mock<IPaymentRepository> _paymentRepoMock;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _paymentService = new PaymentService(_paymentRepoMock.Object);
        }

        [Fact]
        public async Task RecordPaymentAsync_WithCash_CreatesPendingPayment()
        {
            // Arrange — cash has no TransactionId, so status = Pending
            var request = new RecordPaymentRequest
            {
                AppointmentId = 1,
                Amount = 1500.00m,
                PaymentMethod = "Cash",
                TransactionId = null
            };

            Payment? saved = null;
            _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>()))
                            .Callback<Payment>(p => saved = p)
                            .ReturnsAsync((Payment p) => p);

            // Act
            await _paymentService.RecordPaymentAsync(request);

            // Assert
            saved.Should().NotBeNull();
            saved!.Status.Should().Be("Pending");
            saved.PaidAt.Should().BeNull();
            saved.TransactionId.Should().BeNull();
            saved.PaymentMethod.Should().Be("Cash");
        }

        [Fact]
        public async Task RecordPaymentAsync_WithTransactionId_CreatesPaidPayment()
        {
            // Arrange — gateway sends a TransactionId, so mark Paid immediately
            var request = new RecordPaymentRequest
            {
                AppointmentId = 1,
                Amount = 1200.00m,
                PaymentMethod = "CreditCard",
                TransactionId = "TXN-2025-00892"
            };

            Payment? saved = null;
            _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>()))
                            .Callback<Payment>(p => saved = p)
                            .ReturnsAsync((Payment p) => p);

            // Act
            await _paymentService.RecordPaymentAsync(request);

            // Assert
            saved!.Status.Should().Be("Paid");
            saved.PaidAt.Should().NotBeNull();
            saved.TransactionId.Should().Be("TXN-2025-00892");
        }

        [Fact]
        public async Task MarkAsPaidAsync_UpdatesStatusAndPaidAt()
        {
            // Arrange
            var payment = new Payment
            {
                PaymentId = 1,
                AppointmentId = 1,
                Amount = 1800.00m,
                PaymentMethod = "Cash",
                Status = "Pending",
                PaidAt = null
            };

            _paymentRepoMock.Setup(r => r.GetByAppointmentIdAsync(1))
                            .ReturnsAsync(payment);

            _paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => p);

            // Act
            var result = await _paymentService.MarkAsPaidAsync(1, "TXN-9999");

            // Assert
            result.Status.Should().Be("Paid");
            result.PaidAt.Should().NotBeNull();
            result.TransactionId.Should().Be("TXN-9999");
        }

        [Fact]
        public async Task MarkAsPaidAsync_WithNonExistentPayment_ThrowsKeyNotFoundException()
        {
            // Arrange
            _paymentRepoMock.Setup(r => r.GetByAppointmentIdAsync(999))
                            .ReturnsAsync((Payment?)null);

            // Act
            var act = async () => await _paymentService.MarkAsPaidAsync(999, null);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*Payment record not found*");
        }

        [Fact]
        public async Task GetByAppointmentAsync_ReturnsCorrectPayment()
        {
            // Arrange
            var payment = new Payment
            {
                PaymentId = 1,
                AppointmentId = 1,
                Amount = 1500.00m,
                Status = "Paid"
            };

            _paymentRepoMock.Setup(r => r.GetByAppointmentIdAsync(1))
                            .ReturnsAsync(payment);

            // Act
            var result = await _paymentService.GetByAppointmentAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Amount.Should().Be(1500.00m);
            result.Status.Should().Be("Paid");
        }

        [Fact]
        public async Task GetByAppointmentAsync_WithNoPayment_ReturnsNull()
        {
            // Arrange
            _paymentRepoMock.Setup(r => r.GetByAppointmentIdAsync(99))
                            .ReturnsAsync((Payment?)null);

            // Act
            var result = await _paymentService.GetByAppointmentAsync(99);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("Cash", null, "Pending")]
        [InlineData("CreditCard", "TXN-001", "Paid")]
        [InlineData("Insurance", "INS-REF-99", "Paid")]
        public async Task RecordPaymentAsync_StatusDependsOnTransactionId(
            string method, string? txnId, string expectedStatus)
        {
            // Arrange
            var request = new RecordPaymentRequest
            {
                AppointmentId = 1,
                Amount = 1000m,
                PaymentMethod = method,
                TransactionId = txnId
            };

            Payment? saved = null;
            _paymentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>()))
                            .Callback<Payment>(p => saved = p)
                            .ReturnsAsync((Payment p) => p);

            // Act
            await _paymentService.RecordPaymentAsync(request);

            // Assert
            saved!.Status.Should().Be(expectedStatus);
        }
    }
}