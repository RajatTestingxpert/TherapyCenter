using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TherapyCenter.Data;
using TherapyCenter.Entities;

namespace TherapyCenter.Tests
{
    public static class TestHelpers
    {
        // Creates a fresh in-memory database for each test (unique name = no shared state)
        public static AppDbContext CreateInMemoryContext(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
                dbName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        // Fake IConfiguration with JWT values so AuthService can generate tokens
        public static IConfiguration CreateJwtConfig()
        {
            var settings = new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey",     "TestSuperSecretKeyThatIsAtLeast32Chars!" },
                { "JwtSettings:Issuer",        "TestIssuer" },
                { "JwtSettings:Audience",      "TestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        // Entity factory methods — used by all test classes
        public static User CreateAdminUser(int id = 1) => new User
        {
            UserId = id,
            FirstName = "Super",
            LastName = "Admin",
            Email = $"admin{id}@therapy.com",
            PasswordHash = "hashed",
            Role = "Admin",
            PhoneNumber = "9999900000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        public static User CreateDoctorUser(int id = 2) => new User
        {
            UserId = id,
            FirstName = "Dr. Priya",
            LastName = "Sharma",
            Email = $"doctor{id}@therapy.com",
            PasswordHash = "hashed",
            Role = "Doctor",
            PhoneNumber = "9876543210",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        public static User CreateReceptionistUser(int id = 3) => new User
        {
            UserId = id,
            FirstName = "Anita",
            LastName = "Verma",
            Email = $"receptionist{id}@therapy.com",
            PasswordHash = "hashed",
            Role = "Receptionist",
            PhoneNumber = "9812345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        public static User CreateGuardianUser(int id = 4) => new User
        {
            UserId = id,
            FirstName = "Ramesh",
            LastName = "Kumar",
            Email = $"guardian{id}@therapy.com",
            PasswordHash = "hashed",
            Role = "Guardian",
            PhoneNumber = "9123456789",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        public static Doctor CreateDoctor(int doctorId = 1, int userId = 2) => new Doctor
        {
            DoctorId = doctorId,
            UserId = userId,
            Specialization = "Speech-Language Pathology",
            Bio = "10 years experience",
            AvailableDays = "Mon,Tue,Wed,Thu,Fri",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };

        public static Patient CreatePatient(int patientId = 1, int? guardianId = 4) => new Patient
        {
            PatientId = patientId,
            GuardianId = guardianId,
            FirstName = "Aarav",
            LastName = "Kumar",
            DateOfBirth = new DateTime(2018, 3, 15),
            Gender = "Male",
            MedicalHistory = "Delayed speech development",
            CreatedAt = DateTime.UtcNow
        };

        public static Therapy CreateTherapy(int therapyId = 1) => new Therapy
        {
            TherapyId = therapyId,
            Name = "Speech Therapy",
            Description = "Helps children with speech disorders",
            DurationMinutes = 60,
            Cost = 1500.00m
        };

        public static Slot CreateSlot(int slotId = 1, int doctorId = 1, bool isBooked = false) => new Slot
        {
            SlotId = slotId,
            DoctorId = doctorId,
            Date = new DateOnly(2025, 7, 7),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            IsBooked = isBooked
        };

        public static Appointment CreateAppointment(
            int appointmentId = 1,
            int patientId = 1,
            int doctorId = 1,
            int therapyId = 1,
            int? receptionistId = 3,
            string status = "Scheduled") => new Appointment
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                DoctorId = doctorId,
                TherapyId = therapyId,
                ReceptionistId = receptionistId,
                AppointmentDate = new DateOnly(2025, 7, 7),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Status = status,
                Notes = "Test appointment",
                CreatedAt = DateTime.UtcNow
            };
    }
}