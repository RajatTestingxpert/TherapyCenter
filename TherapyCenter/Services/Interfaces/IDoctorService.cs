using TherapyCenter.Entities;

namespace TherapyCenter.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<Doctor>> GetAllAsync();
        Task<Doctor?> GetByIdAsync(int doctorId);
        Task<Doctor?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Slot>> GetAvailableSlotsAsync(int doctorId, DateOnly date);
    }
}
