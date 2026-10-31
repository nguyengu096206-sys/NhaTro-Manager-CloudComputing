using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NhaTro.Web.Data;
using NhaTro.Web.Helpers;
using NhaTro.Web.Models;
using NhaTro.Web.Services;

namespace NhaTro.Web.Controllers;

[Authorize, AutoValidateAntiforgeryToken]
public abstract class WebController : Controller
{
    protected void ThanhCong(string m) => TempData["ok"] = m;
    protected void Loi(string m) => TempData["err"] = m;
    protected int? UserId => User.UserId();

    /// <summary>Chuyển lỗi nghiệp vụ thành thông báo trên form.</summary>
    protected void ThemLoi(Kq kq) => ModelState.AddModelError(string.Empty, kq.Loi);
}

// ------------------------------------------------------------ Tài khoản đăng nhập
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string tenDangNhap, string matKhau, string returnUrl = null)
    {
        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap);
        if (u == null || !u.DangHoatDong || !BCrypt.Net.BCrypt.Verify(matKhau ?? "", u.MatKhauHash))
        {
            ViewBag.Loi = "Sai tên đăng nhập hoặc mật khẩu, hoặc tài khoản đã bị khóa.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        await HttpContext.SignInAsync(Auth.TaoPrincipal(u));
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Denied() => View();
}

// ------------------------------------------------------------ Dashboard
public class HomeController : WebController
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var gioiHan = today.AddDays(30);
        var vm = new DashboardVm
        {
            TongPhong = await _db.Phongs.CountAsync(),
            Trong = await _db.Phongs.CountAsync(p => p.TrangThai == TrangThaiPhong.Trong),
            DangThue = await _db.Phongs.CountAsync(p => p.TrangThai == TrangThaiPhong.DangThue),
            BaoTri = await _db.Phongs.CountAsync(p => p.TrangThai == TrangThaiPhong.BaoTri),
            SapHetHan = await _db.HopDongs.Include(h => h.Phong).Include(h => h.NguoiThue)
                .Where(h => h.TrangThai == TrangThaiHopDong.HieuLuc && h.NgayKetThuc >= today && h.NgayKetThuc <= gioiHan)
                .OrderBy(h => h.NgayKetThuc).ToListAsync(),
            HoaDonChuaThanhToan = await _db.HoaDons.CountAsync(d => !d.DaThanhToan),
            TongChuaThanhToan = (await _db.HoaDons.Where(d => !d.DaThanhToan).Select(d => (decimal?)d.TongTien).SumAsync()) ?? 0
        };
        return View(vm);
    }
}

// ------------------------------------------------------------ Loại phòng
public class LoaiPhongController : WebController
{
    private readonly QuanLyService _s;
    public LoaiPhongController(QuanLyService s) => _s = s;

    public async Task<IActionResult> Index() => View(await _s.DanhSachLoaiPhongAsync());

    [Authorize(Roles = VaiTro.Admin)]
    public IActionResult Create() => View("Form", new LoaiPhong());

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Create(LoaiPhong m)
    {
        if (ModelState.IsValid)
        {
            var kq = await _s.TaoLoaiPhongAsync(m);
            if (kq.Ok) { ThanhCong("Đã thêm loại phòng " + m.TenLoai + "."); return RedirectToAction(nameof(Index)); }
            ThemLoi(kq);
        }
        return View("Form", m);
    }

    [Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var m = await _s.LayLoaiPhongAsync(id);
        return m == null ? NotFound() : View("Form", m);
    }

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Edit(int id, LoaiPhong m)
    {
        m.Id = id;
        if (ModelState.IsValid)
        {
            var kq = await _s.CapNhatLoaiPhongAsync(id, m);
            if (kq.Ok) { ThanhCong("Đã cập nhật loại phòng."); return RedirectToAction(nameof(Index)); }
            if (kq.Status == 404) return NotFound();
            ThemLoi(kq);
        }
        return View("Form", m);
    }

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var kq = await _s.XoaLoaiPhongAsync(id);
        if (kq.Ok) ThanhCong("Đã xóa loại phòng."); else Loi(kq.Loi);
        return RedirectToAction(nameof(Index));
    }
}

// ------------------------------------------------------------ Phòng
public class PhongController : WebController
{
    private readonly QuanLyService _s;
    private readonly FileService _files;
    public PhongController(QuanLyService s, FileService files) { _s = s; _files = files; }

    private async Task NapDanhMucAsync() => ViewBag.LoaiPhongs = new SelectList(await _s.DanhSachLoaiPhongAsync(), "Id", "TenLoai");

    public async Task<IActionResult> Index(string keyword, string trangThai)
    {
        ViewBag.Keyword = keyword;
        ViewBag.TrangThai = trangThai;
        return View(await _s.TimPhongAsync(keyword, trangThai));
    }

    public async Task<IActionResult> Create()
    {
        await NapDanhMucAsync();
        return View("Form", new Phong());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Phong m)
    {
        if (ModelState.IsValid)
        {
            var kq = await _s.TaoPhongAsync(m);
            if (kq.Ok) { ThanhCong("Đã thêm phòng " + m.MaPhong + "."); return RedirectToAction(nameof(Index)); }
            ThemLoi(kq);
        }
        await NapDanhMucAsync();
        return View("Form", m);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _s.LayPhongAsync(id);
        if (m == null) return NotFound();
        await NapDanhMucAsync();
        return View("Form", m);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Phong m)
    {
        m.Id = id;
        if (ModelState.IsValid)
        {
            var kq = await _s.CapNhatPhongAsync(id, m);
            if (kq.Ok) { ThanhCong("Đã cập nhật phòng " + m.MaPhong + "."); return RedirectToAction(nameof(Index)); }
            if (kq.Status == 404) return NotFound();
            ThemLoi(kq);
        }
        await NapDanhMucAsync();
        return View("Form", m);
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await _s.LayPhongAsync(id);
        if (p == null) return NotFound();
        var hd = (await _s.TimHopDongAsync(p.MaPhong)).FirstOrDefault(h => h.PhongId == id && h.TrangThai == TrangThaiHopDong.HieuLuc);
        var vm = new PhongDetailsVm
        {
            Phong = p,
            HopDongHienTai = hd,
            Tep = new TepVm { LoaiThucThe = "Phong", ThucTheId = id, Teps = await _files.DanhSachAsync("Phong", id), ReturnUrl = Url.Action("Details", "Phong", new { id }) }
        };
        return View(vm);
    }

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var kq = await _s.XoaPhongAsync(id);
        if (kq.Ok) ThanhCong("Đã xóa phòng."); else Loi(kq.Loi);
        return RedirectToAction(nameof(Index));
    }
}

// ------------------------------------------------------------ Người thuê
public class NguoiThueController : WebController
{
    private readonly QuanLyService _s;
    public NguoiThueController(QuanLyService s) => _s = s;

    public async Task<IActionResult> Index(string keyword)
    {
        ViewBag.Keyword = keyword;
        return View(await _s.TimNguoiThueAsync(keyword));
    }

    public IActionResult Create() => View("Form", new NguoiThue());

    [HttpPost]
    public async Task<IActionResult> Create(NguoiThue m)
    {
        if (ModelState.IsValid)
        {
            var kq = await _s.TaoNguoiThueAsync(m);
            if (kq.Ok) { ThanhCong("Đã thêm người thuê " + m.HoTen + "."); return RedirectToAction(nameof(Index)); }
            ThemLoi(kq);
        }
        return View("Form", m);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _s.LayNguoiThueAsync(id);
        return m == null ? NotFound() : View("Form", m);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, NguoiThue m)
    {
        m.Id = id;
        if (ModelState.IsValid)
        {
            var kq = await _s.CapNhatNguoiThueAsync(id, m);
            if (kq.Ok) { ThanhCong("Đã cập nhật người thuê."); return RedirectToAction(nameof(Index)); }
            if (kq.Status == 404) return NotFound();
            ThemLoi(kq);
        }
        return View("Form", m);
    }

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var kq = await _s.XoaNguoiThueAsync(id);
        if (kq.Ok) ThanhCong("Đã xóa người thuê."); else Loi(kq.Loi);
        return RedirectToAction(nameof(Index));
    }
}

// ------------------------------------------------------------ Hợp đồng
public class HopDongController : WebController
{
    private readonly QuanLyService _s;
    private readonly FileService _files;
    private readonly AppDbContext _db;
    public HopDongController(QuanLyService s, FileService files, AppDbContext db) { _s = s; _files = files; _db = db; }

    private async Task NapDanhMucAsync(int? phongId = null)
    {
        var phongTrong = await _db.Phongs.Where(p => p.TrangThai == TrangThaiPhong.Trong).OrderBy(p => p.MaPhong).ToListAsync();
        ViewBag.Phongs = new SelectList(phongTrong.Select(p => new { p.Id, Ten = p.MaPhong + " - " + p.TenPhong }), "Id", "Ten", phongId);
        var nguoiThues = await _db.NguoiThues.OrderBy(n => n.HoTen).ToListAsync();
        ViewBag.NguoiThues = new SelectList(nguoiThues.Select(n => new { n.Id, Ten = n.HoTen + " (" + n.Cccd + ")" }), "Id", "Ten");
    }

    public async Task<IActionResult> Index(string keyword)
    {
        ViewBag.Keyword = keyword;
        return View(await _s.TimHopDongAsync(keyword));
    }

    public async Task<IActionResult> Create(int? phongId)
    {
        await NapDanhMucAsync(phongId);
        var m = new HopDong { NgayBatDau = DateTime.Today, NgayKetThuc = DateTime.Today.AddMonths(6) };
        if (phongId.HasValue)
        {
            m.PhongId = phongId.Value;
            var p = await _s.LayPhongAsync(phongId.Value);
            if (p?.LoaiPhong != null) { m.GiaThue = p.LoaiPhong.GiaNiemYet; m.TienCoc = p.LoaiPhong.GiaNiemYet; }
        }
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Create(HopDong m)
    {
        if (ModelState.IsValid)
        {
            var kq = await _s.TaoHopDongAsync(m, UserId);
            if (kq.Ok) { ThanhCong("Đã lập hợp đồng " + m.SoHopDong + "."); return RedirectToAction(nameof(Details), new { id = m.Id }); }
            ThemLoi(kq);
        }
        await NapDanhMucAsync(m.PhongId);
        return View(m);
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await _s.LayHopDongAsync(id);
        if (h == null) return NotFound();
        var vm = new HopDongDetailsVm
        {
            HopDong = h,
            HoaDons = (await _s.TimHoaDonAsync(null, null)).Where(d => d.HopDongId == id).ToList(),
            Tep = new TepVm { LoaiThucThe = "HopDong", ThucTheId = id, Teps = await _files.DanhSachAsync("HopDong", id), ReturnUrl = Url.Action("Details", "HopDong", new { id }) }
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> KetThuc(int id)
    {
        var kq = await _s.KetThucHopDongAsync(id);
        if (kq.Ok) ThanhCong("Đã kết thúc hợp đồng, phòng chuyển về trạng thái Trống."); else Loi(kq.Loi);
        return RedirectToAction(nameof(Details), new { id });
    }
}

// ------------------------------------------------------------ Hóa đơn
public class HoaDonController : WebController
{
    private readonly QuanLyService _s;
    public HoaDonController(QuanLyService s) => _s = s;

    public async Task<IActionResult> Index(DateTime? thang, string trangThai)
    {
        bool? daTT = trangThai == "da" ? true : trangThai == "chua" ? false : (bool?)null;
        ViewBag.Thang = thang?.ToString("yyyy-MM-dd");
        ViewBag.TrangThai = trangThai;
        return View(await _s.TimHoaDonAsync(thang, daTT));
    }

    private async Task NapDanhMucAsync()
    {
        var hds = await _s.HopDongHieuLucAsync();
        ViewBag.HopDongs = new SelectList(hds.Select(h => new { h.Id, Ten = h.Phong.MaPhong + " - " + h.NguoiThue.HoTen + " (" + h.SoHopDong + ")" }), "Id", "Ten");
        ViewBag.GiaThue = hds.ToDictionary(h => h.Id, h => h.GiaThue);
    }

    public async Task<IActionResult> Create(int? hopDongId)
    {
        await NapDanhMucAsync();
        var m = hopDongId.HasValue ? await _s.GoiYHoaDonAsync(hopDongId.Value)
            : new HoaDon { Thang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DonGiaDien = 3500, DonGiaNuoc = 15000, PhiDichVu = 100000 };
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Create(HoaDon m)
    {
        if (ModelState.IsValid)
        {
            var kq = await _s.TaoHoaDonAsync(m, UserId);
            if (kq.Ok) { ThanhCong("Đã lập hóa đơn, tổng tiền " + Fmt.Vnd(m.TongTien) + "."); return RedirectToAction(nameof(Index)); }
            ThemLoi(kq);
        }
        await NapDanhMucAsync();
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> ThanhToan(int id, string returnUrl)
    {
        var kq = await _s.DanhDauThanhToanAsync(id);
        if (kq.Ok) ThanhCong("Đã đánh dấu hóa đơn đã thanh toán."); else Loi(kq.Loi);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}

// ------------------------------------------------------------ Tài khoản (chỉ Administrator)
[Authorize(Roles = VaiTro.Admin)]
public class TaiKhoanController : WebController
{
    private readonly AppDbContext _db;
    public TaiKhoanController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.NguoiDungs.OrderBy(u => u.TenDangNhap).ToListAsync());

    public IActionResult Create() => View(new TaiKhoanForm());

    [HttpPost]
    public async Task<IActionResult> Create(TaiKhoanForm f)
    {
        if (f.VaiTro != VaiTro.Admin && f.VaiTro != VaiTro.Staff) ModelState.AddModelError(nameof(f.VaiTro), "Vai trò không hợp lệ");
        if (ModelState.IsValid && await _db.NguoiDungs.AnyAsync(u => u.TenDangNhap == f.TenDangNhap))
            ModelState.AddModelError(nameof(f.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) return View(f);
        _db.NguoiDungs.Add(new NguoiDung { TenDangNhap = f.TenDangNhap.Trim(), HoTen = f.HoTen.Trim(), VaiTro = f.VaiTro, MatKhauHash = BCrypt.Net.BCrypt.HashPassword(f.MatKhau) });
        await _db.SaveChangesAsync();
        ThanhCong("Đã tạo tài khoản " + f.TenDangNhap + ".");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (u.Id == UserId) { Loi("Không thể khóa chính tài khoản đang đăng nhập."); return RedirectToAction(nameof(Index)); }
        u.DangHoatDong = !u.DangHoatDong;
        await _db.SaveChangesAsync();
        ThanhCong(u.DangHoatDong ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DatLaiMatKhau(int id, string matKhau)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (string.IsNullOrWhiteSpace(matKhau) || matKhau.Length < 6) { Loi("Mật khẩu mới phải có ít nhất 6 ký tự."); return RedirectToAction(nameof(Index)); }
        u.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(matKhau);
        await _db.SaveChangesAsync();
        ThanhCong("Đã đặt lại mật khẩu cho " + u.TenDangNhap + ".");
        return RedirectToAction(nameof(Index));
    }
}

// ------------------------------------------------------------ Tệp đính kèm
public class TepController : WebController
{
    private readonly FileService _files;
    public TepController(FileService files) => _files = files;

    private IActionResult QuayLai(string returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, string loaiThucThe, int thucTheId, string returnUrl)
    {
        var (kq, tep) = await _files.UploadAsync(file, loaiThucThe, thucTheId, UserId);
        if (kq.Ok) ThanhCong("Đã tải lên tệp " + tep.TenGoc + "."); else Loi(kq.Loi);
        return QuayLai(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, bool inline = false)
    {
        var (tep, data) = await _files.TaiAsync(id);
        if (tep == null || data == null) return NotFound();
        return inline ? File(data, tep.LoaiTep) : File(data, tep.LoaiTep, tep.TenGoc);
    }

    [HttpPost, Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(Guid id, string returnUrl)
    {
        var kq = await _files.XoaAsync(id);
        if (kq.Ok) ThanhCong("Đã xóa tệp."); else Loi(kq.Loi);
        return QuayLai(returnUrl);
    }
}
