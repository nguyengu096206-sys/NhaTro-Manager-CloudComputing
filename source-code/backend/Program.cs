using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minio;
using NhaTro.Web.Data;
using NhaTro.Web.Services;

// Cho phép ghi DateTime thường vào cột timestamp/date của PostgreSQL.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IMinioClient>(_ =>
{
    var c = builder.Configuration.GetSection("Minio");
    return new MinioClient()
        .WithEndpoint(c["Endpoint"])
        .WithCredentials(c["AccessKey"], c["SecretKey"])
        .WithSSL(false)
        .Build();
});
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<QuanLyService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12 * 1024 * 1024);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "nhatro.auth";
        o.Cookie.HttpOnly = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Denied";
        // API trả 401/403 thay vì chuyển hướng sang trang đăng nhập.
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NhaTro Manager API", Version = "v1", Description = "REST API quản lý phòng trọ. Đăng nhập bằng POST /api/auth/login trước khi gọi các API khác." });
    c.DocInclusionPredicate((_, api) => api.RelativePath != null && api.RelativePath.StartsWith("api/"));
});

var app = builder.Build();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { type = "about:blank", title = "Lỗi hệ thống", status = 500, detail = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau." }));
    }
    else
    {
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync("<!DOCTYPE html><html lang=\"vi\"><meta charset=\"utf-8\"><body style=\"font-family:sans-serif;margin:3rem\"><h3>Đã xảy ra lỗi hệ thống</h3><p>Vui lòng thử lại sau.</p><a href=\"/\">Về trang chủ</a></body></html>");
    }
}));

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NhaTro Manager API v1"));

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.InitAsync(db, logger);
    try
    {
        await scope.ServiceProvider.GetRequiredService<FileService>().EnsureBucketAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning("Chưa tạo được bucket MinIO, sẽ thử lại khi upload: {Msg}", ex.Message);
    }
}

app.Run();
