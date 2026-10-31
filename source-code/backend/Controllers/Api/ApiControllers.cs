using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTro.Web.Data;
using NhaTro.Web.Helpers;
using NhaTro.Web.Models;
using NhaTro.Web.Services;

namespace NhaTro.Web.Controllers.Api;

/// <summary>Lớp cơ sở cho các REST API: chuyển lỗi nghiệp vụ thành ProblemDetails với mã HTTP phù hợp.</summary>
[ApiController, Authorize]
public abstract class ApiBase : ControllerBase
{
    protected IActionResult Loi(Kq kq) => Problem(
        detail: kq.Loi,
        statusCode: kq.Status,
        title: kq.Status == 404 ? "Không tìm thấy dữ liệu" : kq.Status == 409 ? "Xung đột nghiệp vụ" : "Dữ liệu không hợp lệ");
}

[Route("api/auth")]
public class AuthApiController : ApiBase
{
    private readonly AppDbContext _db;
    public AuthApiController(AppDbContext db) => _db = db;

    /// <summary>Đăng nhập (trả về cookie phiên làm việc).</summary>
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var u = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.TenDangNhap == dto.TenDangNhap);
        if (u == null || !u.DangHoatDong || !BCrypt.Net.BCrypt.Verify(dto.MatKhau, u.MatKhauHash))
            return Problem(detail: "Sai tên đăng nhập hoặc mật khẩu, hoặc tài khoản đã bị khóa.", statusCode: 401, title: "Chưa đăng nhập");
        await HttpContext.SignInAsync(Auth.TaoPrincipal(u));
        return Ok(new { tenDangNhap = u.TenDangNhap, hoTen = u.HoTen, vaiTro = u.VaiTro });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}

[Route("api/phong")]
public class PhongApiController : ApiBase
{
    private readonly QuanLyService _s;
    public PhongApiController(QuanLyService s) => _s = s;

    /// <summary>Danh sách phòng, tìm theo mã/tên và lọc theo trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string keyword, [FromQuery] string trangThai) => Ok(await _s.TimPhongAsync(keyword, trangThai));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _s.LayPhongAsync(id);
        return p == null ? Loi(Kq.That("Không tìm thấy phòng.", 404)) : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Phong m)
    {
        var kq = await _s.TaoPhongAsync(m);
        return kq.Ok ? CreatedAtAction(nameof(GetById), new { id = m.Id }, m) : Loi(kq);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Phong m)
    {
        var kq = await _s.CapNhatPhongAsync(id, m);
        return kq.Ok ? NoContent() : Loi(kq);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var kq = await _s.XoaPhongAsync(id);
        return kq.Ok ? NoContent() : Loi(kq);
    }
}

[Route("api/nguoi-thue")]
public class NguoiThueApiController : ApiBase
{
    private readonly QuanLyService _s;
    public NguoiThueApiController(QuanLyService s) => _s = s;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string keyword) => Ok(await _s.TimNguoiThueAsync(keyword));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var n = await _s.LayNguoiThueAsync(id);
        return n == null ? Loi(Kq.That("Không tìm thấy người thuê.", 404)) : Ok(n);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NguoiThue m)
    {
        var kq = await _s.TaoNguoiThueAsync(m);
        return kq.Ok ? CreatedAtAction(nameof(GetById), new { id = m.Id }, m) : Loi(kq);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] NguoiThue m)
    {
        var kq = await _s.CapNhatNguoiThueAsync(id, m);
        return kq.Ok ? NoContent() : Loi(kq);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var kq = await _s.XoaNguoiThueAsync(id);
        return kq.Ok ? NoContent() : Loi(kq);
    }
}

[Route("api/hop-dong")]
public class HopDongApiController : ApiBase
{
    private readonly QuanLyService _s;
    public HopDongApiController(QuanLyService s) => _s = s;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string keyword) => Ok(await _s.TimHopDongAsync(keyword));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var h = await _s.LayHopDongAsync(id);
        return h == null ? Loi(Kq.That("Không tìm thấy hợp đồng.", 404)) : Ok(h);
    }

    /// <summary>Lập hợp đồng; phòng tự chuyển sang "Đang thuê".</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HopDong m)
    {
        var kq = await _s.TaoHopDongAsync(m, User.UserId());
        return kq.Ok ? CreatedAtAction(nameof(GetById), new { id = m.Id }, m) : Loi(kq);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] HopDong m)
    {
        var kq = await _s.CapNhatHopDongAsync(id, m);
        return kq.Ok ? NoContent() : Loi(kq);
    }

    /// <summary>Kết thúc hợp đồng; phòng trở về "Trống".</summary>
    [HttpPost("{id:int}/ket-thuc")]
    public async Task<IActionResult> KetThuc(int id)
    {
        var kq = await _s.KetThucHopDongAsync(id);
        return kq.Ok ? NoContent() : Loi(kq);
    }
}

[Route("api/hoa-don")]
public class HoaDonApiController : ApiBase
{
    private readonly QuanLyService _s;
    public HoaDonApiController(QuanLyService s) => _s = s;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? thang, [FromQuery] bool? daThanhToan) => Ok(await _s.TimHoaDonAsync(thang, daThanhToan));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var d = await _s.LayHoaDonAsync(id);
        return d == null ? Loi(Kq.That("Không tìm thấy hóa đơn.", 404)) : Ok(d);
    }

    /// <summary>Lập hóa đơn tháng; tổng tiền do hệ thống tự tính.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HoaDon m)
    {
        var kq = await _s.TaoHoaDonAsync(m, User.UserId());
        return kq.Ok ? CreatedAtAction(nameof(GetById), new { id = m.Id }, m) : Loi(kq);
    }

    [HttpPut("{id:int}/thanh-toan")]
    public async Task<IActionResult> ThanhToan(int id)
    {
        var kq = await _s.DanhDauThanhToanAsync(id);
        return kq.Ok ? NoContent() : Loi(kq);
    }
}

[Route("api/tep")]
public class TepApiController : ApiBase
{
    private readonly FileService _files;
    public TepApiController(FileService files) => _files = files;

    /// <summary>Upload tệp (multipart/form-data, trường "file") cho Phong hoặc HopDong.</summary>
    [HttpPost, Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromQuery] string loaiThucThe, [FromQuery] int thucTheId, IFormFile file)
    {
        var (kq, tep) = await _files.UploadAsync(file, loaiThucThe, thucTheId, User.UserId());
        if (!kq.Ok) return Loi(kq);
        return Created("/api/tep/" + tep.Id + "/download", tep);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string loaiThucThe, [FromQuery] int thucTheId) => Ok(await _files.DanhSachAsync(loaiThucThe, thucTheId));

    /// <summary>Tải xuống tệp; thêm ?inline=true để xem trực tiếp (ảnh, PDF).</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] bool inline = false)
    {
        var (tep, data) = await _files.TaiAsync(id);
        if (tep == null || data == null) return Loi(Kq.That("Không tìm thấy tệp.", 404));
        return inline ? File(data, tep.LoaiTep) : File(data, tep.LoaiTep, tep.TenGoc);
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = VaiTro.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var kq = await _files.XoaAsync(id);
        return kq.Ok ? NoContent() : Loi(kq);
    }
}
