using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

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
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure DB exists (dev-safe)
    //context.Database.EnsureCreated();

    // Seed Admin only if none exists
    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var adminPassword = "Admin@123";

        context.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@exam.com",
            PasswordHash = PasswordHelper.HashPassword(adminPassword),
            Role = "Admin",
            IsActive = true
        });

        context.SaveChanges();
    }
}

// ---------------- PIPELINE ----------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
