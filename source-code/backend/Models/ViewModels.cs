using System.ComponentModel.DataAnnotations;

namespace NhaTro.Web.Models;

public class TepVm
{
    public string LoaiThucThe { get; set; }
    public int ThucTheId { get; set; }
    public List<TepDinhKem> Teps { get; set; } = new();
    public string ReturnUrl { get; set; }
}

public class PhongDetailsVm
{
    public Phong Phong { get; set; }
    public HopDong HopDongHienTai { get; set; }
    public TepVm Tep { get; set; }
}

public class HopDongDetailsVm
{
    public HopDong HopDong { get; set; }
    public List<HoaDon> HoaDons { get; set; } = new();
    public TepVm Tep { get; set; }
}

public class DashboardVm
{
    public int TongPhong { get; set; }
    public int Trong { get; set; }
    public int DangThue { get; set; }
    public int BaoTri { get; set; }
    public List<HopDong> SapHetHan { get; set; } = new();
    public int HoaDonChuaThanhToan { get; set; }
    public decimal TongChuaThanhToan { get; set; }
}

public class TaiKhoanForm
{
    [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Vui lòng nhập tên đăng nhập"), StringLength(50, MinimumLength = 3, ErrorMessage = "Từ 3 đến 50 ký tự")]
    public string TenDangNhap { get; set; }

    [Display(Name = "Họ và tên"), Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    public string HoTen { get; set; }

    [Display(Name = "Vai trò"), Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public string VaiTro { get; set; } = Models.VaiTro.Staff;

    [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 50 ký tự")]
    public string MatKhau { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] public string TenDangNhap { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] public string MatKhau { get; set; }
}
