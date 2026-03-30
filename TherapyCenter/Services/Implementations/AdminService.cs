using TherapyCenter.DTO_s.Admin;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Interfaces;

namespace TherapyCenter.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly ITherapyRepository _therapyRepo;
        private readonly IDoctorRepository _doctorRepo;
        private readonly IUserRepository _userRepo;
        private readonly ISlotRepository _slotRepo;

        public AdminService(
            ITherapyRepository therapyRepo,
            IDoctorRepository doctorRepo,
            IUserRepository userRepo,
            ISlotRepository slotRepo)
        {
            _therapyRepo = therapyRepo;
            _doctorRepo = doctorRepo;
            _userRepo = userRepo;
            _slotRepo = slotRepo;
        }

        // ── Therapy CRUD ───────────────────────────────────────────────────────

        public async Task<Therapy> CreateTherapyAsync(CreateTherapyRequest request)
        {
            var therapy = new Therapy
            {
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                Cost = request.Cost
            };
            return await _therapyRepo.CreateAsync(therapy);
        }

        public async Task<Therapy> UpdateTherapyAsync(int therapyId, UpdateTherapyRequest request)
        {
            var therapy = await _therapyRepo.GetByIdAsync(therapyId)
                          ?? throw new KeyNotFoundException($"Therapy {therapyId} not found.");

            therapy.Name = request.Name;
            therapy.Description = request.Description;
            therapy.DurationMinutes = request.DurationMinutes;
            therapy.Cost = request.Cost;

            return await _therapyRepo.UpdateAsync(therapy);
        }

        public async Task DeleteTherapyAsync(int therapyId)
            => await _therapyRepo.DeleteAsync(therapyId);

        public async Task<IEnumerable<Therapy>> GetAllTherapiesAsync()
            => await _therapyRepo.GetAllAsync();

        // ── Doctor profile ─────────────────────────────────────────────────────

        public async Task<Doctor> CreateDoctorProfileAsync(CreateDoctorProfileRequest request)
        {
            var user = await _userRepo.GetByIdAsync(request.UserId)
                       ?? throw new KeyNotFoundException("User not found.");

            if (user.Role != "Doctor")
                throw new InvalidOperationException("User must have the Doctor role.");

            var doctor = new Doctor
            {
                UserId = request.UserId,
                Specialization = request.Specialization,
                Bio = request.Bio,
                AvailableDays = request.AvailableDays,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            return await _doctorRepo.CreateAsync(doctor);
        }

        public async Task<IEnumerable<User>> GetAllReceptionistsAsync()
            => await _userRepo.GetByRoleAsync("Receptionist");

        // ── Bulk slot generation ───────────────────────────────────────────────

        public async Task<int> GenerateSlotsForDoctorAsync(GenerateSlotsRequest request)
        {
            var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId)
                         ?? throw new KeyNotFoundException("Doctor not found.");

            if (doctor.StartTime == null || doctor.EndTime == null)
                throw new InvalidOperationException("Doctor has no working hours configured.");

            var availableDays = (doctor.AvailableDays ?? "Mon,Tue,Wed,Thu,Fri")
                                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var slots = new List<Slot>();
            var current = request.FromDate;

            while (current <= request.ToDate)
            {
                var dayAbbr = current.DayOfWeek.ToString()[..3];

                if (availableDays.Contains(dayAbbr, StringComparer.OrdinalIgnoreCase))
                {
                    var slotStart = doctor.StartTime.Value;

                    while (slotStart.AddHours(1) <= doctor.EndTime.Value)
                    {
                        slots.Add(new Slot
                        {
                            DoctorId = request.DoctorId,
                            Date = current,
                            StartTime = slotStart,
                            EndTime = slotStart.AddHours(1),
                            IsBooked = false
                        });

                        slotStart = slotStart.AddHours(1);
                    }
                }

                current = current.AddDays(1);
            }

            await _slotRepo.BulkCreateAsync(slots);
            return slots.Count;
        }
    }
    }
