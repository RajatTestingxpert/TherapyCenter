using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TherapyCenter.Data;
using TherapyCenter.Repositories.Implementations;
using TherapyCenter.Repositories.Interfaces;
using TherapyCenter.Services.Interfaces;
using TherapyCenter.Services.Implementations;
//using Microsoft.OpenApi.Models;

namespace TherapyCenter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // ── 1. MySQL via Pomelo ───────────────────────────────────────────────────────
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

                // ── 2. JWT Authentication ─────────────────────────────────────────────────────
                var jwtSettings = builder.Configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"]!;

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

                // ── 3. Authorization Policies ─────────────────────────────────────────────────
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                    options.AddPolicy("StaffOnly", p => p.RequireRole("Admin", "Receptionist"));
                    options.AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"));
                    options.AddPolicy("PatientAccess", p => p.RequireRole("Patient", "Guardian"));
                    options.AddPolicy("AllStaff", p => p.RequireRole("Admin", "Receptionist", "Doctor"));
                });

                // ── 4. Repositories ───────────────────────────────────────────────────────────
                builder.Services.AddScoped<IUserRepository, UserRepository>();
                builder.Services.AddScoped<IPatientRepository, PatientRepository>();
                builder.Services.AddScoped<ITherapyRepository, TherapyRepository>();
                builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
                builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
                builder.Services.AddScoped<IFindingRepository, FindingRepository>();
                builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
                builder.Services.AddScoped<ISlotRepository, SlotRepository>();

                // ── 5. Services ───────────────────────────────────────────────────────────────
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<IAdminService, AdminService>();
                builder.Services.AddScoped<IAppointmentService, AppointmentService>();
                builder.Services.AddScoped<IPatientService, PatientService>();
                builder.Services.AddScoped<IDoctorService, DoctorService>();
                builder.Services.AddScoped<ISlotService, SlotService>();
                builder.Services.AddScoped<IPaymentService, PaymentService>();

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();


                // ── 6. Middleware pipeline ────────────────────────────────────────────────────
                var app = builder.Build();


                app.UseHttpsRedirection();
                app.UseAuthentication();   // must come before UseAuthorization
                app.UseAuthorization();
                app.MapControllers();
                app.Run();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                throw;
            }
        }
    }
}
