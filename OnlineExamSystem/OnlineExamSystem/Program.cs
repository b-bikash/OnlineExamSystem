using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Helpers;
using OnlineExamSystem.Models;
using OnlineExamSystem.Services.AdminCleanup;
using OnlineExamSystem.Services.DemoData;
using OnlineExamSystem.Services.ImportExport;


var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AdminAuthorizeFilter>();
builder.Services.AddScoped<SessionValidationFilter>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IAdminCleanupService, AdminCleanupService>();
builder.Services.AddScoped<IDemoDataSeederService, DemoDataSeederService>();
builder.Services.AddScoped<IQuestionImportService, QuestionImportService>();
builder.Services.AddScoped<OnlineExamSystem.Services.Email.IEmailService, OnlineExamSystem.Services.Email.EmailService>();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ADD AUTHENTICATION (Cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";   // placeholder for now
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

// Authorization
builder.Services.AddAuthorization();

// Session
builder.Services.AddSession();

var app = builder.Build();

// =====================================================
// 🔐 ADMIN SEEDING (HASH-AWARE, SAFE)
// =====================================================

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed Admin only if none exists
        if (!context.Users.Any(u => u.Role == "Admin"))
        {
            var adminPassword = "";

            context.Users.Add(new User
            {
                Username = "admin",
                Email = "",
                PasswordHash = PasswordHelper.HashPassword(adminPassword),
                Role = "Admin",
                IsActive = true
            });

            context.SaveChanges();
        }
    }
}


// ---------------- PIPELINE ----------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files to serve unknown file types (e.g., face-api model shards)
var provider = new FileExtensionContentTypeProvider();
var staticFileOptions = new StaticFileOptions
{
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
};
app.UseStaticFiles(staticFileOptions);

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
