
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=acxiomcrm.db"));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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
}

if (args.Any(a => a.Equals(
        "--provision-admin",
        StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

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

        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
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

    if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
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
