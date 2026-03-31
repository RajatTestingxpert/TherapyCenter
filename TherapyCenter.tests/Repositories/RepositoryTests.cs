using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TherapyCenter.Data;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Implementations;
using TherapyCenter.Tests;
using Xunit;

namespace TherapyCenter.Tests.Repositories
{
    public class RepositoryTests
    {
        [Fact]
        public async Task CreateAsync_SavesUserToDatabase()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);
            var user = TestHelpers.CreateAdminUser();

            var result = await repo.CreateAsync(user);

            result.UserId.Should().BeGreaterThan(0);
            result.Email.Should().Be(user.Email);

            var fromDb = await repo.GetByEmailAsync(user.Email);
            fromDb.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);
            var user = TestHelpers.CreateAdminUser();
            await repo.CreateAsync(user);

            var result = await repo.GetByEmailAsync(user.Email);

            result.Should().NotBeNull();
            result!.FirstName.Should().Be("Super");
            result.Role.Should().Be("Admin");
        }

        [Fact]
        public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);

            var result = await repo.GetByEmailAsync("nobody@therapy.com");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_InactiveUser_ReturnsNull()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);
            var user = TestHelpers.CreateAdminUser();
            user.IsActive = false;
            await repo.CreateAsync(user);

            var result = await repo.GetByEmailAsync(user.Email);

            result.Should().BeNull();
        }

        [Fact]
        public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);
            var user = TestHelpers.CreateAdminUser();
            await repo.CreateAsync(user);

            var exists = await repo.EmailExistsAsync(user.Email);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);

            var exists = await repo.EmailExistsAsync("ghost@therapy.com");

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task GetByRoleAsync_ReturnsOnlyUsersWithThatRole()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);

            var admin = TestHelpers.CreateAdminUser();
            var doctor = TestHelpers.CreateDoctorUser();
            var receptionist = TestHelpers.CreateReceptionistUser();

            await repo.CreateAsync(admin);
            await repo.CreateAsync(doctor);
            await repo.CreateAsync(receptionist);

            var doctors = await repo.GetByRoleAsync("Doctor");

            doctors.Should().HaveCount(1);
            doctors.First().Role.Should().Be("Doctor");
        }

        [Fact]
        public async Task UpdateAsync_ChangesUserFields()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new UserRepository(context);
            var user = TestHelpers.CreateAdminUser();
            var created = await repo.CreateAsync(user);

            created.PhoneNumber = "9999988888";
            var updated = await repo.UpdateAsync(created);

            updated.PhoneNumber.Should().Be("9999988888");
            var fromDb = await repo.GetByIdAsync(created.UserId);
            fromDb!.PhoneNumber.Should().Be("9999988888");
        }
    }

    public class SlotRepositoryTests
    {
        private async Task<Doctor> SeedDoctorAsync(AppDbContext context)
        {
            var doctorUser = TestHelpers.CreateDoctorUser();
            context.Users.Add(doctorUser);
            await context.SaveChangesAsync();

            var doctor = TestHelpers.CreateDoctor();
            doctor.UserId = doctorUser.UserId;
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            return doctor;
        }

        [Fact]
        public async Task BulkCreateAsync_SavesAllSlots()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var doctor = await SeedDoctorAsync(context);
            var repo = new SlotRepository(context);

            var slots = Enumerable.Range(1, 8).Select(i => new Slot
            {
                DoctorId = doctor.DoctorId,
                Date = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(8 + i, 0),
                EndTime = new TimeOnly(9 + i, 0),
                IsBooked = false
            }).ToList();

            await repo.BulkCreateAsync(slots);

            var saved = await repo.GetSlotsByDoctorAsync(doctor.DoctorId);
            saved.Should().HaveCount(8);
        }

        [Fact]
        public async Task GetAvailableSlotsByDoctorAsync_ReturnsOnlyFreeSlots()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var doctor = await SeedDoctorAsync(context);
            var repo = new SlotRepository(context);
            var date = DateOnly.FromDateTime(DateTime.Today);

            var slots = new List<Slot>
            {
                new() { DoctorId = doctor.DoctorId, Date = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsBooked = false },
                new() { DoctorId = doctor.DoctorId, Date = date, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), IsBooked = false },
                new() { DoctorId = doctor.DoctorId, Date = date, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(12, 0), IsBooked = false },
                new() { DoctorId = doctor.DoctorId, Date = date, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), IsBooked = true },
                new() { DoctorId = doctor.DoctorId, Date = date, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(14, 0), IsBooked = true },
            };

            await repo.BulkCreateAsync(slots);

            var available = await repo.GetAvailableSlotsByDoctorAsync(doctor.DoctorId, date);

            available.Should().HaveCount(3);
            available.Should().OnlyContain(s => !s.IsBooked);
        }

        [Fact]
        public async Task UpdateAsync_MarksSlotAsBooked()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var doctor = await SeedDoctorAsync(context);
            var repo = new SlotRepository(context);

            var slot = new Slot
            {
                DoctorId = doctor.DoctorId,
                Date = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                IsBooked = false
            };

            var created = await repo.CreateAsync(slot);

            created.IsBooked = true;
            await repo.UpdateAsync(created);

            var fromDb = await repo.GetByIdAsync(created.SlotId);
            fromDb!.IsBooked.Should().BeTrue();
        }
    }

    public class TherapyRepositoryTests
    {
        [Fact]
        public async Task CreateAsync_SavesTherapy()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new TherapyRepository(context);
            var therapy = TestHelpers.CreateTherapy();

            var result = await repo.CreateAsync(therapy);

            result.TherapyId.Should().BeGreaterThan(0);
            result.Name.Should().Be("Speech Therapy");
            result.Cost.Should().Be(1500.00m);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllTherapies()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new TherapyRepository(context);

            await repo.CreateAsync(new Therapy { Name = "Speech", DurationMinutes = 60, Cost = 1500 });
            await repo.CreateAsync(new Therapy { Name = "Occupational", DurationMinutes = 45, Cost = 1200 });
            await repo.CreateAsync(new Therapy { Name = "Behavioral", DurationMinutes = 60, Cost = 1800 });

            var all = await repo.GetAllAsync();

            all.Should().HaveCount(3);
        }

        [Fact]
        public async Task DeleteAsync_RemovesTherapy()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new TherapyRepository(context);
            var therapy = TestHelpers.CreateTherapy();
            var created = await repo.CreateAsync(therapy);

            await repo.DeleteAsync(created.TherapyId);

            var result = await repo.GetByIdAsync(created.TherapyId);
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ChangesTherapyFields()
        {
            await using var context = TestHelpers.CreateInMemoryContext();
            var repo = new TherapyRepository(context);
            var therapy = TestHelpers.CreateTherapy();
            var created = await repo.CreateAsync(therapy);

            created.Name = "Advanced Speech Therapy";
            created.Cost = 1800.00m;
            var updated = await repo.UpdateAsync(created);

            updated.Name.Should().Be("Advanced Speech Therapy");
            updated.Cost.Should().Be(1800.00m);
        }
    }
}