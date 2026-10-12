
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=acxiomcrm.db";
var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteConnection.DataSource))
    sqliteConnection.DataSource = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, sqliteConnection.DataSource));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(sqliteConnection.ToString()));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<SignInManager<ApplicationUser>, ActiveUserSignInManager>();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CrmAccessService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "Manager", "Sales Executive" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}

if (args.Any(a => a.Equals(
        "--provision-admin",
        StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var roleResult = await roleManager.CreateAsync(
            new IdentityRole("Admin"));

        if (!roleResult.Succeeded)
        {
            Console.WriteLine("Could not create Admin role:");
            foreach (var error in roleResult.Errors)
                Console.WriteLine(error.Description);

            return;
        }
    }

    Console.Write("Administrator email: ");
    var email = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(email))
    {
        Console.WriteLine("Email is required.");
        return;
    }

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        Console.WriteLine("No account exists with that email.");
        Console.Write("Create a new administrator account? (yes/no): ");

        if (!string.Equals(
                Console.ReadLine()?.Trim(),
                "yes",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No account created.");
            return;
        }

        Console.Write("New password: ");
        var password = ReadPassword();

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = email
        };

        var createResult = await userManager.CreateAsync(
            user,
            password);

        if (!createResult.Succeeded)
        {
            Console.WriteLine("Account creation failed:");
            foreach (var error in createResult.Errors)
                Console.WriteLine(error.Description);

            return;
        }
    }

    var currentRoles = await userManager.GetRolesAsync(user);
    if (currentRoles.Count != 1 || currentRoles[0] != "Admin")
    {
        if (currentRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                Console.WriteLine("Could not replace the user's current roles:");
                foreach (var error in removeResult.Errors)
                    Console.WriteLine(error.Description);
                return;
            }
        }
        var addResult = await userManager.AddToRoleAsync(
            user,
            "Admin");

        if (!addResult.Succeeded)
        {
            Console.WriteLine("Role assignment failed:");
            foreach (var error in addResult.Errors)
                Console.WriteLine(error.Description);

            return;
        }
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();
        await auditService.LogAsync("RoleChange", "User", details: $"Administrator role provisioned for user {user.Id}.", oldValue: string.Join(",", currentRoles), newValue: "Admin");
        await userManager.UpdateSecurityStampAsync(user);
    }

    Console.WriteLine($"Admin role assigned to {user.Email}.");
    Console.WriteLine("Run the application normally to start the website.");
    return;
}

app.Run();

static string ReadPassword()
{
    var password = new System.Text.StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Length--;

            continue;
        }

        if (!char.IsControl(key.KeyChar))
            password.Append(key.KeyChar);
    }

    return password.ToString();
}
