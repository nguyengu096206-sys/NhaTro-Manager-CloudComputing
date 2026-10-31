using System.Globalization;
using System.Security.Claims;
using NhaTro.Web.Models;

namespace NhaTro.Web.Helpers;

public static class Fmt
{
    private static readonly CultureInfo Vi = new("vi-VN");
    public static string Vnd(decimal v) => string.Format(Vi, "{0:N0} đ", v);
    public static string Ngay(DateTime d) => d.ToString("dd/MM/yyyy");
    public static string Ngay(DateTime? d) => d.HasValue ? d.Value.ToString("dd/MM/yyyy") : "";
    public static string Thang(DateTime d) => d.ToString("MM/yyyy");
    public static string Dung(long b) => b >= 1024 * 1024 ? $"{b / 1024.0 / 1024.0:0.0} MB" : $"{Math.Max(1, b / 1024)} KB";
}

public static class Auth
{
    public static ClaimsPrincipal TaoPrincipal(NguoiDung u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.TenDangNhap),
            new(ClaimTypes.GivenName, u.HoTen),
            new(ClaimTypes.Role, u.VaiTro)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }

    public static int? UserId(this ClaimsPrincipal p) => int.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
