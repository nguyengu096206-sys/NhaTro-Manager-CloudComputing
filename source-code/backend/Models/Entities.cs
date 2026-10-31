using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NhaTro.Web.Models;

public static class VaiTro
{
    public const string Admin = "Administrator";
    public const string Staff = "Staff";
}

public static class TrangThaiPhong
{
    public const string Trong = "Trống";
    public const string DangThue = "Đang thuê";
    public const string BaoTri = "Bảo trì";
    public static readonly string[] All = { Trong, DangThue, BaoTri };
}

public static class TrangThaiHopDong
{
    public const string HieuLuc = "Hiệu lực";
    public const string DaKetThuc = "Đã kết thúc";
}

public class NguoiDung
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string TenDangNhap { get; set; }
    [Required, StringLength(100)] public string MatKhauHash { get; set; }
    [Required, StringLength(100)] public string HoTen { get; set; }
    [Required, StringLength(20)] public string VaiTro { get; set; }
    public bool DangHoatDong { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class LoaiPhong
{
    public int Id { get; set; }

    [Display(Name = "Tên loại"), Required(ErrorMessage = "Vui lòng nhập tên loại phòng"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    public string TenLoai { get; set; }

    [Display(Name = "Giá niêm yết (VNĐ/tháng)"), Range(0d, 100000000000d, ErrorMessage = "Giá không hợp lệ"), Column(TypeName = "numeric(12,0)")]
    public decimal GiaNiemYet { get; set; }

    [Display(Name = "Diện tích (m²)"), Range(1d, 999d, ErrorMessage = "Diện tích phải từ 1 đến 999 m²"), Column(TypeName = "numeric(5,1)")]
    public decimal DienTich { get; set; }

    [Display(Name = "Mô tả")]
    public string MoTa { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Phong
{
    public int Id { get; set; }

    [Display(Name = "Mã phòng"), Required(ErrorMessage = "Vui lòng nhập mã phòng"), StringLength(20, ErrorMessage = "Tối đa 20 ký tự")]
    public string MaPhong { get; set; }

    [Display(Name = "Tên phòng"), Required(ErrorMessage = "Vui lòng nhập tên phòng"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    public string TenPhong { get; set; }

    [Display(Name = "Tầng"), Range(0, 50, ErrorMessage = "Tầng phải từ 0 đến 50")]
    public int Tang { get; set; }

    [Display(Name = "Loại phòng"), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại phòng")]
    public int LoaiPhongId { get; set; }
    public LoaiPhong LoaiPhong { get; set; }

    [Display(Name = "Trạng thái"), Required(ErrorMessage = "Vui lòng chọn trạng thái"), StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiPhong.Trong;

    [Display(Name = "Ghi chú")]
    public string GhiChu { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class NguoiThue
{
    public int Id { get; set; }

    [Display(Name = "Họ và tên"), Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    public string HoTen { get; set; }

    [Display(Name = "CCCD"), Required(ErrorMessage = "Vui lòng nhập số CCCD"), RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD gồm đúng 12 chữ số")]
    public string Cccd { get; set; }

    [Display(Name = "Số điện thoại"), Required(ErrorMessage = "Vui lòng nhập số điện thoại"), RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10–11 chữ số, bắt đầu bằng 0")]
    public string SoDienThoai { get; set; }

    [Display(Name = "Quê quán"), StringLength(200, ErrorMessage = "Tối đa 200 ký tự")]
    public string QueQuan { get; set; }

    [Display(Name = "Email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    public string Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class HopDong
{
    public int Id { get; set; }

    [Display(Name = "Số hợp đồng"), Required(ErrorMessage = "Vui lòng nhập số hợp đồng"), StringLength(30, ErrorMessage = "Tối đa 30 ký tự")]
    public string SoHopDong { get; set; }

    [Display(Name = "Phòng"), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng")]
    public int PhongId { get; set; }
    public Phong Phong { get; set; }

    [Display(Name = "Người thuê"), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn người thuê")]
    public int NguoiThueId { get; set; }
    public NguoiThue NguoiThue { get; set; }

    [Display(Name = "Ngày bắt đầu"), Column(TypeName = "date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime NgayBatDau { get; set; }

    [Display(Name = "Ngày kết thúc"), Column(TypeName = "date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime NgayKetThuc { get; set; }

    [Display(Name = "Giá thuê (VNĐ/tháng)"), Range(0d, 100000000000d, ErrorMessage = "Giá thuê không hợp lệ"), Column(TypeName = "numeric(12,0)")]
    public decimal GiaThue { get; set; }

    [Display(Name = "Tiền cọc (VNĐ)"), Range(0d, 100000000000d, ErrorMessage = "Tiền cọc không hợp lệ"), Column(TypeName = "numeric(12,0)")]
    public decimal TienCoc { get; set; }

    [Display(Name = "Trạng thái"), StringLength(20)]
    public string TrangThai { get; set; } = TrangThaiHopDong.HieuLuc;

    public int? NguoiTaoId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class HoaDon
{
    public int Id { get; set; }

    [Display(Name = "Hợp đồng"), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng")]
    public int HopDongId { get; set; }
    public HopDong HopDong { get; set; }

    [Display(Name = "Tháng"), Column(TypeName = "date"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime Thang { get; set; }

    [Display(Name = "Chỉ số điện cũ"), Range(0, int.MaxValue, ErrorMessage = "Chỉ số không hợp lệ")]
    public int ChiSoDienCu { get; set; }
    [Display(Name = "Chỉ số điện mới"), Range(0, int.MaxValue, ErrorMessage = "Chỉ số không hợp lệ")]
    public int ChiSoDienMoi { get; set; }
    [Display(Name = "Chỉ số nước cũ"), Range(0, int.MaxValue, ErrorMessage = "Chỉ số không hợp lệ")]
    public int ChiSoNuocCu { get; set; }
    [Display(Name = "Chỉ số nước mới"), Range(0, int.MaxValue, ErrorMessage = "Chỉ số không hợp lệ")]
    public int ChiSoNuocMoi { get; set; }

    [Display(Name = "Đơn giá điện (VNĐ/kWh)"), Range(0d, 100000000d, ErrorMessage = "Đơn giá không hợp lệ"), Column(TypeName = "numeric(10,0)")]
    public decimal DonGiaDien { get; set; }
    [Display(Name = "Đơn giá nước (VNĐ/m³)"), Range(0d, 100000000d, ErrorMessage = "Đơn giá không hợp lệ"), Column(TypeName = "numeric(10,0)")]
    public decimal DonGiaNuoc { get; set; }
    [Display(Name = "Phí dịch vụ (VNĐ)"), Range(0d, 100000000000d, ErrorMessage = "Phí dịch vụ không hợp lệ"), Column(TypeName = "numeric(12,0)")]
    public decimal PhiDichVu { get; set; }

    [Display(Name = "Tổng tiền"), Column(TypeName = "numeric(12,0)")]
    public decimal TongTien { get; set; }

    [Display(Name = "Đã thanh toán")]
    public bool DaThanhToan { get; set; }

    [Column(TypeName = "date")]
    public DateTime? NgayThanhToan { get; set; }

    public int? NguoiTaoId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class TepDinhKem
{
    public Guid Id { get; set; }
    [Required, StringLength(255)] public string TenGoc { get; set; }
    [Required, StringLength(300)] public string ObjectKey { get; set; }
    [Required, StringLength(63)] public string Bucket { get; set; }
    [Required, StringLength(100)] public string LoaiTep { get; set; }
    public long DungLuong { get; set; }
    public DateTime ThoiDiemUpload { get; set; } = DateTime.Now;
    [Required, StringLength(20)] public string LoaiThucThe { get; set; }
    public int ThucTheId { get; set; }
    public int? NguoiUploadId { get; set; }
}
