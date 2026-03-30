using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TherapyCenter.DTO_s.Patient;
using TherapyCenter.Services.Interfaces;
using TherapyCenter.DTO_s.Patient;
using TherapyCenter.Services.Interfaces;

namespace TherapyCenter.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // GET api/patient
        // Staff sees all patients
        [HttpGet]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetAll()
            => Ok(await _patientService.GetAllAsync());

        // GET api/patient/7
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _patientService.GetByIdAsync(id);
            return patient == null ? NotFound() : Ok(patient);
        }

        // GET api/patient/guardian/4
        // Guardian sees only their own children
        [HttpGet("guardian/{guardianId}")]
        [Authorize(Policy = "PatientAccess")]
        public async Task<IActionResult> GetByGuardian(int guardianId)
            => Ok(await _patientService.GetByGuardianAsync(guardianId));

        // POST api/patient
        // Receptionist or Admin creates patient record (offline / walk-in)
        [HttpPost]
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
        {
            var patient = await _patientService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = patient.PatientId }, patient);
        }
    }
}