using Microsoft.EntityFrameworkCore;
using TherapyCenter.Data;
using TherapyCenter.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TherapyCenter.Repositories.Interfaces;

namespace TherapyCenter.Repositories.Implementations
{
    public class PatientRepository : IPatientRepository
    {
        private readonly AppDbContext _context;

        public PatientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Patient?> GetByIdAsync(int patientId)
            => await _context.Patients
                             .Include(p => p.Guardian)
                             .FirstOrDefaultAsync(p => p.PatientId == patientId);

        public async Task<IEnumerable<Patient>> GetAllAsync()
            => await _context.Patients
                             .Include(p => p.Guardian)
                             .ToListAsync();

        public async Task<IEnumerable<Patient>> GetByGuardianIdAsync(int guardianId)
            => await _context.Patients
                             .Where(p => p.GuardianId == guardianId)
                             .ToListAsync();

        public async Task<Patient> CreateAsync(Patient patient)
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }
    }
}