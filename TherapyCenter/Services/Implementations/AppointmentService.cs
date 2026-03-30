using TherapyCenter.DTO_s.Appointment;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Interfaces;
using AppointmentEntity = TherapyCenter.Entities.Appointment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TherapyCenter.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly ITherapyRepository _therapyRepo;

        public AppointmentService(
            IAppointmentRepository appointmentRepo,
            ISlotRepository slotRepo,
            ITherapyRepository therapyRepo)
        {
            _appointmentRepo = appointmentRepo;
            _slotRepo = slotRepo;
            _therapyRepo = therapyRepo;
        }

        public async Task<AppointmentEntity> BookAsync(BookAppointmentRequest request)
        {
            var slot = await _slotRepo.GetByIdAsync(request.SlotId)
                       ?? throw new KeyNotFoundException("Slot not found.");

            if (slot.IsBooked)
                throw new InvalidOperationException("This slot is already booked.");

            var therapy = await _therapyRepo.GetByIdAsync(request.TherapyId)
                          ?? throw new KeyNotFoundException("Therapy not found.");

            slot.IsBooked = true;
            await _slotRepo.UpdateAsync(slot);

            var appointment = new AppointmentEntity
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                TherapyId = request.TherapyId,
                ReceptionistId = request.ReceptionistId,
                AppointmentDate = slot.Date,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Status = "Scheduled",
                Notes = request.Notes
            };

            return await _appointmentRepo.CreateAsync(appointment);
        }

        public async Task<AppointmentEntity> UpdateStatusAsync(int appointmentId, UpdateAppointmentStatusRequest request)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId)
                              ?? throw new KeyNotFoundException("Appointment not found.");

            appointment.Status = request.Status;
            if (!string.IsNullOrEmpty(request.Notes))
                appointment.Notes = request.Notes;

            return await _appointmentRepo.UpdateAsync(appointment);
        }

        public async Task<IEnumerable<AppointmentEntity>> GetByPatientAsync(int patientId)
            => await _appointmentRepo.GetByPatientIdAsync(patientId);

        public async Task<IEnumerable<AppointmentEntity>> GetByDoctorAsync(int doctorId)
            => await _appointmentRepo.GetByDoctorIdAsync(doctorId);

        public async Task<IEnumerable<AppointmentEntity>> GetByDateAsync(DateOnly date)
            => await _appointmentRepo.GetByDateAsync(date);

        public async Task<AppointmentEntity?> GetByIdAsync(int appointmentId)
            => await _appointmentRepo.GetByIdAsync(appointmentId);
    }
}