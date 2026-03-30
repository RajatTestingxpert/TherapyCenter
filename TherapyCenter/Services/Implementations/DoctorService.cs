using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Interfaces;

namespace TherapyCenter.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _doctorRepo;
        private readonly ISlotRepository _slotRepo;

        public DoctorService(IDoctorRepository doctorRepo, ISlotRepository slotRepo)
        {
            _doctorRepo = doctorRepo;
            _slotRepo = slotRepo;
        }

        public async Task<IEnumerable<Doctor>> GetAllAsync()
            => await _doctorRepo.GetAllAsync();

        public async Task<Doctor?> GetByIdAsync(int doctorId)
            => await _doctorRepo.GetByIdAsync(doctorId);

        public async Task<Doctor?> GetByUserIdAsync(int userId)
            => await _doctorRepo.GetByUserIdAsync(userId);

        public async Task<IEnumerable<Slot>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
            => await _slotRepo.GetAvailableSlotsByDoctorAsync(doctorId, date);
    }
}
