using Microsoft.AspNetCore.Identity;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EmailContext>();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // 6 haneli kod ile e-posta doğrulanmadan giriş yapılamaz
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<EmailContext>()
    .AddDefaultTokenProviders();

// Şifre yenileme bağlantısı 1 saat geçerli olsun
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/SignIn";
    options.AccessDeniedPath = "/ErrorPages/Page403";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    // AJAX (fetch) isteklerinde login sayfasına yönlendirmek yerine 401 döndür
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            context.Response.StatusCode = 401;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<MailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 401 / 403 / 404 durumlarında ErrorPages altındaki sayfalar gösterilir
app.UseStatusCodePagesWithReExecute("/ErrorPages/Page{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();   // yüklenen profil fotoğrafları (wwwroot/images/profile) için
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
