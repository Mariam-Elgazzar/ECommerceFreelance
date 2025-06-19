#region Usings
using ECommerce.BL.Services.AuthenticationService;
using ECommerce.BL.Settings;
using ECommerce.BL.UnitOfWork;
using ECommerce.DAL.Data;
using ECommerce.DAL.Extend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
#endregion


#region Configure services

#region Create builder
var builder = WebApplication.CreateBuilder(args);
#endregion


#region Logging

//builder.Host.UseSerilog((context, configuration) =>
//{
//    configuration
//        .MinimumLevel.Information()
//        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
//        .WriteTo.Console(
//            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
//        );
//    //.WriteTo.File(
//    //    path: "logs/app-log-.txt",
//    //    rollingInterval: RollingInterval.Day,
//    //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
//    //);
//});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

#endregion


#region Connection String Configuration
var connectionString = builder.Configuration.GetConnectionString(name: "DefaultConnection") ??
                throw new InvalidOperationException(message: "No connection string was found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

#endregion


#region Endpoints API Explorer Configuration

builder.Services.AddEndpointsApiExplorer();

#endregion


#region API Configuration
// Add services to the container.
builder.Services.AddControllers();
#endregion


#region OpenAPI Configuration
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
#endregion


#region Swagger Configration
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ECommerce API",
        Description = "API documentation for ECommerce",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Khaled Ayman",
            Email = "khaled654ayman0@gmail.com"
        }
        //License = new OpenApiLicense
        //{
        //    Name = "Not Found any license",
        //    Url = new Uri("https://example.com/license")
        //}
    });

    // JWT Authentication setup
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Enable XML comments if available
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
#endregion


#region identity Configration

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters = null;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider);

#endregion


#region Authentication Configuration

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        ClockSkew = TimeSpan.Zero
    };
});
#endregion


#region CORS Configration

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyMethod()
               .AllowCredentials()
               .AllowAnyHeader();
    });
});

#endregion


#region Get Section Configration

#region JWT Configration

builder.Services.Configure<JWT>(builder.Configuration.GetSection(nameof(JWT)));

#endregion

#region Admin Configration

builder.Services.Configure<AdminLogin>(builder.Configuration.GetSection(nameof(AdminLogin)));

#endregion

#region Email Configration

builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection(nameof(EmailConfiguration)));

#endregion

#region Cloudinary Configration
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(nameof(CloudinarySettings)));
#endregion

#region Order Configration
builder.Services.Configure<OrderSettings>(builder.Configuration.GetSection(nameof(OrderSettings)));
#endregion

#region Twilio Configration
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection(nameof(TwilioSettings)));
#endregion

#endregion


#region Dependency Injection Configuration

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

#endregion


#region Build the application
var app = builder.Build();
#endregion


#endregion


#region Chech environment
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
#endregion


#region Use middleware

#region Static Files Meddleware

app.UseStaticFiles();

#endregion

#region Swagger Meddleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var prefix = string.IsNullOrEmpty(options.RoutePrefix) ? "." : "..";
    options.SwaggerEndpoint($"{prefix}/swagger/v1/swagger.json", "HR System API v1");
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DisplayRequestDuration();
});
#endregion

#region Cors Meddelwere
app.UseCors();
#endregion

#region HTTPS Meddleware
app.UseHttpsRedirection();
#endregion

#region Authentication and Authorization Meddelwere
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Endpoints
app.MapControllers();

app.Run();

#endregion

#endregion

