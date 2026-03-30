using TherapyCenter.DTO_s.Patient;
using TherapyCenter.Entities;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Interfaces;

namespace TherapyCenter.Services.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepo;

        public PatientService(IPatientRepository patientRepo)
        {
            _patientRepo = patientRepo;
        }

        public async Task<Patient> CreateAsync(CreatePatientRequest request)
        {
            var patient = new Patient
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MedicalHistory = request.MedicalHistory,
                GuardianId = request.GuardianId
            };

            return await _patientRepo.CreateAsync(patient);
        }

        public async Task<Patient?> GetByIdAsync(int patientId)
            => await _patientRepo.GetByIdAsync(patientId);

        public async Task<IEnumerable<Patient>> GetAllAsync()
            => await _patientRepo.GetAllAsync();

        public async Task<IEnumerable<Patient>> GetByGuardianAsync(int guardianId)
            => await _patientRepo.GetByGuardianIdAsync(guardianId);
    }
}
