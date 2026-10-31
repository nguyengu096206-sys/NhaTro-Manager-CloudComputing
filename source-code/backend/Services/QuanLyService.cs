using Microsoft.EntityFrameworkCore;
using NhaTro.Web.Data;
using NhaTro.Web.Models;

namespace NhaTro.Web.Services;

/// <summary>Toàn bộ nghiệp vụ ghi dữ liệu; dùng chung cho giao diện MVC và REST API.</summary>
public class QuanLyService
{
    private readonly AppDbContext _db;
    private readonly FileService _files;

    public QuanLyService(AppDbContext db, FileService files)
    {
        _db = db;
        _files = files;
    }

    private static string Like(string s) => s.Trim().ToLower();

    private async Task<Kq> LuuAsync(string loiRangBuoc)
    {
        try
        {
            await _db.SaveChangesAsync();
            return Kq.Thanh();
        }
        catch (DbUpdateException)
        {
            return Kq.That(loiRangBuoc, 409);
        }
    }

    // ======================= LOẠI PHÒNG =======================
    public Task<List<LoaiPhong>> DanhSachLoaiPhongAsync() => _db.LoaiPhongs.OrderBy(x => x.TenLoai).ToListAsync();
    public Task<LoaiPhong> LayLoaiPhongAsync(int id) => _db.LoaiPhongs.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Kq> TaoLoaiPhongAsync(LoaiPhong m)
    {
        m.TenLoai = m.TenLoai?.Trim();
        if (await _db.LoaiPhongs.AnyAsync(x => x.TenLoai == m.TenLoai)) return Kq.That("Tên loại phòng đã tồn tại.", 409);
        m.Id = 0;
        _db.LoaiPhongs.Add(m);
        return await LuuAsync("Không lưu được loại phòng.");
    }

    public async Task<Kq> CapNhatLoaiPhongAsync(int id, LoaiPhong m)
    {
        var e = await _db.LoaiPhongs.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy loại phòng.", 404);
        m.TenLoai = m.TenLoai?.Trim();
        if (await _db.LoaiPhongs.AnyAsync(x => x.TenLoai == m.TenLoai && x.Id != id)) return Kq.That("Tên loại phòng đã tồn tại.", 409);
        e.TenLoai = m.TenLoai; e.GiaNiemYet = m.GiaNiemYet; e.DienTich = m.DienTich; e.MoTa = m.MoTa;
        return await LuuAsync("Không lưu được loại phòng.");
    }

    public async Task<Kq> XoaLoaiPhongAsync(int id)
    {
        var e = await _db.LoaiPhongs.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy loại phòng.", 404);
        if (await _db.Phongs.AnyAsync(p => p.LoaiPhongId == id)) return Kq.That("Không thể xóa: vẫn còn phòng thuộc loại này.", 409);
        _db.LoaiPhongs.Remove(e);
        return await LuuAsync("Không xóa được loại phòng.");
    }

    // ======================= PHÒNG =======================
    public async Task<List<Phong>> TimPhongAsync(string keyword, string trangThai)
    {
        var q = _db.Phongs.Include(p => p.LoaiPhong).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Like(keyword);
            q = q.Where(p => p.MaPhong.ToLower().Contains(k) || p.TenPhong.ToLower().Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(p => p.TrangThai == trangThai);
        return await q.OrderBy(p => p.MaPhong).ToListAsync();
    }

    public Task<Phong> LayPhongAsync(int id) => _db.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.Id == id);

    private async Task<Kq> KiemTraPhongAsync(Phong p, int? id)
    {
        p.MaPhong = p.MaPhong?.Trim();
        p.TenPhong = p.TenPhong?.Trim();
        if (!TrangThaiPhong.All.Contains(p.TrangThai)) return Kq.That("Trạng thái phòng không hợp lệ.");
        if (!await _db.LoaiPhongs.AnyAsync(x => x.Id == p.LoaiPhongId)) return Kq.That("Loại phòng không tồn tại.");
        var idHienTai = id ?? 0;
        if (await _db.Phongs.AnyAsync(x => x.MaPhong == p.MaPhong && x.Id != idHienTai)) return Kq.That("Mã phòng " + p.MaPhong + " đã tồn tại.", 409);
        var coHopDong = id.HasValue && await _db.HopDongs.AnyAsync(h => h.PhongId == idHienTai && h.TrangThai == TrangThaiHopDong.HieuLuc);
        if (p.TrangThai == TrangThaiPhong.DangThue && !coHopDong) return Kq.That("Phòng chỉ chuyển sang \"Đang thuê\" khi lập hợp đồng.");
        if (coHopDong && p.TrangThai != TrangThaiPhong.DangThue) return Kq.That("Phòng đang có hợp đồng hiệu lực, không thể đổi trạng thái.", 409);
        return null;
    }

    public async Task<Kq> TaoPhongAsync(Phong p)
    {
        var loi = await KiemTraPhongAsync(p, null);
        if (loi != null) return loi;
        p.Id = 0;
        p.LoaiPhong = null;
        _db.Phongs.Add(p);
        return await LuuAsync("Không lưu được phòng.");
    }

    public async Task<Kq> CapNhatPhongAsync(int id, Phong m)
    {
        var e = await _db.Phongs.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy phòng.", 404);
        var loi = await KiemTraPhongAsync(m, id);
        if (loi != null) return loi;
        e.MaPhong = m.MaPhong; e.TenPhong = m.TenPhong; e.Tang = m.Tang;
        e.LoaiPhongId = m.LoaiPhongId; e.TrangThai = m.TrangThai; e.GhiChu = m.GhiChu;
        return await LuuAsync("Không lưu được phòng.");
    }

    public async Task<Kq> XoaPhongAsync(int id)
    {
        var e = await _db.Phongs.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy phòng.", 404);
        if (await _db.HopDongs.AnyAsync(h => h.PhongId == id && h.TrangThai == TrangThaiHopDong.HieuLuc))
            return Kq.That("Không thể xóa: phòng đang được thuê (có hợp đồng hiệu lực).", 409);
        if (await _db.HopDongs.AnyAsync(h => h.PhongId == id))
            return Kq.That("Không thể xóa: phòng đã có lịch sử hợp đồng.", 409);
        await _files.XoaTheoThucTheAsync("Phong", id);
        _db.Phongs.Remove(e);
        return await LuuAsync("Không xóa được phòng.");
    }

    // ======================= NGƯỜI THUÊ =======================
    public async Task<List<NguoiThue>> TimNguoiThueAsync(string keyword)
    {
        var q = _db.NguoiThues.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Like(keyword);
            q = q.Where(n => n.HoTen.ToLower().Contains(k) || n.Cccd.Contains(k) || n.SoDienThoai.Contains(k));
        }
        return await q.OrderBy(n => n.HoTen).ToListAsync();
    }

    public Task<NguoiThue> LayNguoiThueAsync(int id) => _db.NguoiThues.FirstOrDefaultAsync(n => n.Id == id);

    public async Task<Kq> TaoNguoiThueAsync(NguoiThue m)
    {
        m.HoTen = m.HoTen?.Trim();
        if (await _db.NguoiThues.AnyAsync(x => x.Cccd == m.Cccd)) return Kq.That("CCCD " + m.Cccd + " đã tồn tại.", 409);
        m.Id = 0;
        _db.NguoiThues.Add(m);
        return await LuuAsync("Không lưu được người thuê.");
    }

    public async Task<Kq> CapNhatNguoiThueAsync(int id, NguoiThue m)
    {
        var e = await _db.NguoiThues.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy người thuê.", 404);
        if (await _db.NguoiThues.AnyAsync(x => x.Cccd == m.Cccd && x.Id != id)) return Kq.That("CCCD " + m.Cccd + " đã tồn tại.", 409);
        e.HoTen = m.HoTen?.Trim(); e.Cccd = m.Cccd; e.SoDienThoai = m.SoDienThoai;
        e.QueQuan = m.QueQuan; e.Email = m.Email;
        return await LuuAsync("Không lưu được người thuê.");
    }

    public async Task<Kq> XoaNguoiThueAsync(int id)
    {
        var e = await _db.NguoiThues.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy người thuê.", 404);
        if (await _db.HopDongs.AnyAsync(h => h.NguoiThueId == id && h.TrangThai == TrangThaiHopDong.HieuLuc))
            return Kq.That("Không thể xóa: người thuê đang có hợp đồng hiệu lực.", 409);
        if (await _db.HopDongs.AnyAsync(h => h.NguoiThueId == id))
            return Kq.That("Không thể xóa: người thuê đã có lịch sử hợp đồng.", 409);
        _db.NguoiThues.Remove(e);
        return await LuuAsync("Không xóa được người thuê.");
    }

    // ======================= HỢP ĐỒNG =======================
    public async Task<List<HopDong>> TimHopDongAsync(string keyword)
    {
        var q = _db.HopDongs.Include(h => h.Phong).Include(h => h.NguoiThue).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Like(keyword);
            q = q.Where(h => h.SoHopDong.ToLower().Contains(k) || h.NguoiThue.HoTen.ToLower().Contains(k) || h.Phong.MaPhong.ToLower().Contains(k));
        }
        return await q.OrderByDescending(h => h.NgayBatDau).ThenByDescending(h => h.Id).ToListAsync();
    }

    public Task<HopDong> LayHopDongAsync(int id) =>
        _db.HopDongs.Include(h => h.Phong).Include(h => h.NguoiThue).FirstOrDefaultAsync(h => h.Id == id);

    private static Kq KiemTraNgayGia(HopDong h)
    {
        if (h.NgayBatDau.Year < 2000 || h.NgayKetThuc.Year < 2000) return Kq.That("Vui lòng nhập ngày bắt đầu và ngày kết thúc.");
        if (h.NgayKetThuc.Date <= h.NgayBatDau.Date) return Kq.That("Ngày kết thúc phải sau ngày bắt đầu.");
        if (h.GiaThue < 0 || h.TienCoc < 0) return Kq.That("Giá thuê và tiền cọc không được âm.");
        return null;
    }

    public async Task<Kq> TaoHopDongAsync(HopDong h, int? userId)
    {
        h.SoHopDong = h.SoHopDong?.Trim();
        var loi = KiemTraNgayGia(h);
        if (loi != null) return loi;

        var phong = await _db.Phongs.FindAsync(h.PhongId);
        if (phong == null) return Kq.That("Phòng không tồn tại.");
        if (!await _db.NguoiThues.AnyAsync(n => n.Id == h.NguoiThueId)) return Kq.That("Người thuê không tồn tại.");
        if (await _db.HopDongs.AnyAsync(x => x.SoHopDong == h.SoHopDong)) return Kq.That("Số hợp đồng " + h.SoHopDong + " đã tồn tại.", 409);
        if (phong.TrangThai == TrangThaiPhong.BaoTri) return Kq.That("Phòng đang bảo trì, không thể cho thuê.", 409);
        if (phong.TrangThai != TrangThaiPhong.Trong || await _db.HopDongs.AnyAsync(x => x.PhongId == h.PhongId && x.TrangThai == TrangThaiHopDong.HieuLuc))
            return Kq.That("Phòng đã có hợp đồng hiệu lực.", 409);

        h.Id = 0;
        h.NgayBatDau = h.NgayBatDau.Date;
        h.NgayKetThuc = h.NgayKetThuc.Date;
        h.TrangThai = TrangThaiHopDong.HieuLuc;
        h.NguoiTaoId = userId;
        h.Phong = null;
        h.NguoiThue = null;
        phong.TrangThai = TrangThaiPhong.DangThue;
        _db.HopDongs.Add(h);
        return await LuuAsync("Không lưu được hợp đồng.");
    }

    public async Task<Kq> CapNhatHopDongAsync(int id, HopDong m)
    {
        var e = await _db.HopDongs.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy hợp đồng.", 404);
        if (e.TrangThai != TrangThaiHopDong.HieuLuc) return Kq.That("Hợp đồng đã kết thúc, không thể cập nhật.", 409);
        var loi = KiemTraNgayGia(m);
        if (loi != null) return loi;
        e.NgayBatDau = m.NgayBatDau.Date; e.NgayKetThuc = m.NgayKetThuc.Date;
        e.GiaThue = m.GiaThue; e.TienCoc = m.TienCoc;
        return await LuuAsync("Không lưu được hợp đồng.");
    }

    public async Task<Kq> KetThucHopDongAsync(int id)
    {
        var e = await _db.HopDongs.Include(h => h.Phong).FirstOrDefaultAsync(h => h.Id == id);
        if (e == null) return Kq.That("Không tìm thấy hợp đồng.", 404);
        if (e.TrangThai != TrangThaiHopDong.HieuLuc) return Kq.That("Hợp đồng đã kết thúc trước đó.", 409);
        e.TrangThai = TrangThaiHopDong.DaKetThuc;
        e.Phong.TrangThai = TrangThaiPhong.Trong;
        return await LuuAsync("Không kết thúc được hợp đồng.");
    }

    // ======================= HÓA ĐƠN =======================
    public async Task<List<HoaDon>> TimHoaDonAsync(DateTime? thang, bool? daThanhToan)
    {
        var q = _db.HoaDons.Include(d => d.HopDong).ThenInclude(h => h.Phong)
            .Include(d => d.HopDong).ThenInclude(h => h.NguoiThue).AsQueryable();
        if (thang.HasValue)
        {
            var t = new DateTime(thang.Value.Year, thang.Value.Month, 1);
            q = q.Where(d => d.Thang == t);
        }
        if (daThanhToan.HasValue) q = q.Where(d => d.DaThanhToan == daThanhToan.Value);
        return await q.OrderByDescending(d => d.Thang).ThenBy(d => d.HopDong.Phong.MaPhong).ToListAsync();
    }

    public Task<HoaDon> LayHoaDonAsync(int id) =>
        _db.HoaDons.Include(d => d.HopDong).ThenInclude(h => h.Phong).FirstOrDefaultAsync(d => d.Id == id);

    public Task<List<HopDong>> HopDongHieuLucAsync() =>
        _db.HopDongs.Include(h => h.Phong).Include(h => h.NguoiThue)
            .Where(h => h.TrangThai == TrangThaiHopDong.HieuLuc).OrderBy(h => h.Phong.MaPhong).ToListAsync();

    /// <summary>Gợi ý chỉ số cũ cho hóa đơn mới = chỉ số mới của hóa đơn gần nhất.</summary>
    public async Task<HoaDon> GoiYHoaDonAsync(int hopDongId)
    {
        var d = new HoaDon { HopDongId = hopDongId, Thang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DonGiaDien = 3500, DonGiaNuoc = 15000, PhiDichVu = 100000 };
        var truoc = await _db.HoaDons.Where(x => x.HopDongId == hopDongId).OrderByDescending(x => x.Thang).FirstOrDefaultAsync();
        if (truoc != null)
        {
            d.ChiSoDienCu = truoc.ChiSoDienMoi; d.ChiSoDienMoi = truoc.ChiSoDienMoi;
            d.ChiSoNuocCu = truoc.ChiSoNuocMoi; d.ChiSoNuocMoi = truoc.ChiSoNuocMoi;
            d.DonGiaDien = truoc.DonGiaDien; d.DonGiaNuoc = truoc.DonGiaNuoc; d.PhiDichVu = truoc.PhiDichVu;
        }
        return d;
    }

    /// <summary>Tổng = tiền phòng + điện + nước + phí dịch vụ.</summary>
    public static decimal TinhTong(decimal giaThue, HoaDon d) =>
        giaThue + (d.ChiSoDienMoi - d.ChiSoDienCu) * d.DonGiaDien + (d.ChiSoNuocMoi - d.ChiSoNuocCu) * d.DonGiaNuoc + d.PhiDichVu;

    public async Task<Kq> TaoHoaDonAsync(HoaDon d, int? userId)
    {
        var hd = await _db.HopDongs.FirstOrDefaultAsync(x => x.Id == d.HopDongId);
        if (hd == null) return Kq.That("Hợp đồng không tồn tại.");
        if (hd.TrangThai != TrangThaiHopDong.HieuLuc) return Kq.That("Chỉ lập hóa đơn cho hợp đồng đang hiệu lực.", 409);
        if (d.Thang.Year < 2000) return Kq.That("Vui lòng chọn tháng của hóa đơn.");
        if (d.ChiSoDienMoi < d.ChiSoDienCu) return Kq.That("Chỉ số điện mới không được nhỏ hơn chỉ số cũ.");
        if (d.ChiSoNuocMoi < d.ChiSoNuocCu) return Kq.That("Chỉ số nước mới không được nhỏ hơn chỉ số cũ.");
        if (d.DonGiaDien < 0 || d.DonGiaNuoc < 0 || d.PhiDichVu < 0) return Kq.That("Đơn giá và phí dịch vụ không được âm.");

        d.Thang = new DateTime(d.Thang.Year, d.Thang.Month, 1);
        if (await _db.HoaDons.AnyAsync(x => x.HopDongId == d.HopDongId && x.Thang == d.Thang))
            return Kq.That("Hợp đồng này đã có hóa đơn cho tháng " + d.Thang.ToString("MM/yyyy") + ".", 409);

        d.Id = 0;
        d.TongTien = TinhTong(hd.GiaThue, d);
        d.DaThanhToan = false;
        d.NgayThanhToan = null;
        d.NguoiTaoId = userId;
        d.HopDong = null;
        _db.HoaDons.Add(d);
        return await LuuAsync("Không lưu được hóa đơn.");
    }

    public async Task<Kq> DanhDauThanhToanAsync(int id)
    {
        var e = await _db.HoaDons.FindAsync(id);
        if (e == null) return Kq.That("Không tìm thấy hóa đơn.", 404);
        if (e.DaThanhToan) return Kq.That("Hóa đơn đã được thanh toán trước đó.", 409);
        e.DaThanhToan = true;
        e.NgayThanhToan = DateTime.Today;
        return await LuuAsync("Không cập nhật được hóa đơn.");
    }
}
