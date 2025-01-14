using Backend.Controllers;
using Backend.Repositories.Document;
using Backend.Repositories.Graph;
using Backend.Repositories.Interfaces;
using Backend.Repositories.Relational;
using Backend.Services;
using Infrastructure.Persistance;
using Infrastructure.Persistance.Graph;
using Infrastructure.Persistance.Relational;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;
using System.Security.Claims;
using System.Text;

namespace Backend
{
    public class Program
    { 
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddHealthChecks();

            // Add services to the container
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // Add security definition for JWT Bearer
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            builder.Services.AddAuthorization();

            // Services
            builder.Services.AddScoped<ICardService, CardService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IDeckService, DeckService>();
            builder.Services.AddScoped<ICombatService, CombatService>();
            builder.Services.AddScoped<IEnemyService, EnemyService>();


            // Repositories (Relational)
            builder.Services.AddScoped<ICardRepository, CardRepository>();
            builder.Services.AddScoped<ICombatRepository, CombatRepository>();
            builder.Services.AddScoped<IDeckRepository, DeckRepository>();
            builder.Services.AddScoped<IEnemyRepository, EnemyRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();





            builder.Services.AddMemoryCache(); // Bruger vi til in-memory caching for blacklisting tokens


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()  // Specify the allowed origin (frontend)
                           .AllowAnyHeader()                      // Allow all headers (e.g., Authorization, Content-Type, etc.)
                           .AllowAnyMethod()
                           .SetIsOriginAllowedToAllowWildcardSubdomains();                      // Allow all HTTP methods (e.g., GET, POST, PUT, DELETE)
                                                                   //.AllowCredentials();                   // Allow cookies and Authorization headers to be sent with the request
                });
            });

            var hostingEnvironment = builder.Environment.EnvironmentName;
            PersistanceConfiguration.ConfigureServices(builder.Services, dbtype.DefaultConnection, hostingEnvironment);

            //builder.Services.AddAntiforgery(options =>
            //{
            //    // Customize the settings if needed (e.g., SameSite policy, cookie options)
            //    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Ensures cookie is sent over HTTPS
            //    options.Cookie.HttpOnly = true;  // Prevents JavaScript access to the cookie
            //    options.Cookie.SameSite = SameSiteMode.Unspecified;  // Protects against CSRF attacks
            //    options.Cookie.Expiration = TimeSpan.FromMinutes(60);
            //    options.Cookie.Path = "/";
            //});


            var app = builder.Build();


            // Map controllers
            app.MapCardEndpoint();
            app.MapAuthEndpoints();
            app.MapEnemyEndpoint();
            app.MapDeckEndpoint();
            app.MapHealthChecks("/health");

            app.MapCombatEndpoints();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/", () => "Hello World!");
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("AllowAll");
            app.Run();
        }
    }
}