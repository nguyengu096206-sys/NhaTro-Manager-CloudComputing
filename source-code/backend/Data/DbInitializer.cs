using Microsoft.EntityFrameworkCore;
using NhaTro.Web.Models;
using NhaTro.Web.Services;

namespace NhaTro.Web.Data;

public static class DbInitializer
{
    public static async Task InitAsync(AppDbContext db, ILogger logger)
    {
        for (var i = 1; ; i++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception ex) when (i < 30)
            {
                logger.LogWarning("Chưa kết nối được database (lần {Lan}): {Msg}", i, ex.Message);
                await Task.Delay(2000);
            }
        }

        if (await db.NguoiDungs.AnyAsync()) return;

        var admin = new NguoiDung { TenDangNhap = "admin", HoTen = "Chủ trọ", VaiTro = VaiTro.Admin, MatKhauHash = BCrypt.Net.BCrypt.HashPassword("Admin@123") };
        var staff = new NguoiDung { TenDangNhap = "staff", HoTen = "Nhân viên quản lý", VaiTro = VaiTro.Staff, MatKhauHash = BCrypt.Net.BCrypt.HashPassword("Staff@123") };
        db.NguoiDungs.AddRange(admin, staff);

        var don = new LoaiPhong { TenLoai = "Phòng đơn", GiaNiemYet = 1800000, DienTich = 18, MoTa = "Phòng đơn, có gác lửng, WC riêng" };
        var doi = new LoaiPhong { TenLoai = "Phòng đôi", GiaNiemYet = 2500000, DienTich = 25, MoTa = "Phòng rộng cho 2 người, ban công" };
        var studio = new LoaiPhong { TenLoai = "Studio", GiaNiemYet = 3200000, DienTich = 32, MoTa = "Có bếp, máy lạnh, nội thất cơ bản" };
        db.LoaiPhongs.AddRange(don, doi, studio);
        await db.SaveChangesAsync();

        var phongs = new[]
        {
            new Phong { MaPhong = "P101", TenPhong = "Phòng 101", Tang = 1, LoaiPhongId = don.Id },
            new Phong { MaPhong = "P102", TenPhong = "Phòng 102", Tang = 1, LoaiPhongId = don.Id },
            new Phong { MaPhong = "P103", TenPhong = "Phòng 103", Tang = 1, LoaiPhongId = don.Id },
            new Phong { MaPhong = "P104", TenPhong = "Phòng 104", Tang = 1, LoaiPhongId = doi.Id },
            new Phong { MaPhong = "P105", TenPhong = "Phòng 105", Tang = 1, LoaiPhongId = doi.Id, TrangThai = TrangThaiPhong.BaoTri, GhiChu = "Đang sửa máy lạnh" },
            new Phong { MaPhong = "P201", TenPhong = "Phòng 201", Tang = 2, LoaiPhongId = doi.Id },
            new Phong { MaPhong = "P202", TenPhong = "Phòng 202", Tang = 2, LoaiPhongId = studio.Id },
            new Phong { MaPhong = "P203", TenPhong = "Phòng 203", Tang = 2, LoaiPhongId = studio.Id },
            new Phong { MaPhong = "P204", TenPhong = "Phòng 204", Tang = 2, LoaiPhongId = don.Id },
            new Phong { MaPhong = "P205", TenPhong = "Phòng 205", Tang = 2, LoaiPhongId = doi.Id },
        };
        db.Phongs.AddRange(phongs);

        var nguoiThues = new[]
        {
            new NguoiThue { HoTen = "Nguyễn Văn An", Cccd = "079201000001", SoDienThoai = "0901000001", QueQuan = "Cần Thơ", Email = "an@example.com" },
            new NguoiThue { HoTen = "Trần Thị Bình", Cccd = "079201000002", SoDienThoai = "0901000002", QueQuan = "An Giang" },
            new NguoiThue { HoTen = "Lê Minh Châu", Cccd = "079201000003", SoDienThoai = "0901000003", QueQuan = "Cà Mau" },
            new NguoiThue { HoTen = "Phạm Quốc Dũng", Cccd = "079201000004", SoDienThoai = "0901000004", QueQuan = "Bạc Liêu" },
            new NguoiThue { HoTen = "Võ Thị Em", Cccd = "079201000005", SoDienThoai = "0901000005", QueQuan = "Kiên Giang" },
        };
        db.NguoiThues.AddRange(nguoiThues);
        await db.SaveChangesAsync();

        var today = DateTime.Today;
        var hopDongs = new[]
        {
            new HopDong { SoHopDong = "HD-2026-0001", PhongId = phongs[0].Id, NguoiThueId = nguoiThues[0].Id, NgayBatDau = today.AddDays(-120), NgayKetThuc = today.AddDays(20), GiaThue = 1800000, TienCoc = 1800000, NguoiTaoId = admin.Id },
            new HopDong { SoHopDong = "HD-2026-0002", PhongId = phongs[1].Id, NguoiThueId = nguoiThues[1].Id, NgayBatDau = today.AddDays(-90), NgayKetThuc = today.AddDays(150), GiaThue = 1800000, TienCoc = 1800000, NguoiTaoId = admin.Id },
            new HopDong { SoHopDong = "HD-2026-0003", PhongId = phongs[5].Id, NguoiThueId = nguoiThues[2].Id, NgayBatDau = today.AddDays(-60), NgayKetThuc = today.AddDays(300), GiaThue = 2500000, TienCoc = 2500000, NguoiTaoId = staff.Id },
            new HopDong { SoHopDong = "HD-2026-0004", PhongId = phongs[6].Id, NguoiThueId = nguoiThues[3].Id, NgayBatDau = today.AddDays(-30), NgayKetThuc = today.AddDays(330), GiaThue = 3200000, TienCoc = 3200000, NguoiTaoId = staff.Id },
        };
        db.HopDongs.AddRange(hopDongs);
        foreach (var h in hopDongs) phongs.First(p => p.Id == h.PhongId).TrangThai = TrangThaiPhong.DangThue;
        await db.SaveChangesAsync();

        var thangTruoc = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var hoaDons = hopDongs.Select((h, i) =>
        {
            var d = new HoaDon
            {
                HopDongId = h.Id, Thang = thangTruoc,
                ChiSoDienCu = 100 + i * 20, ChiSoDienMoi = 180 + i * 35,
                ChiSoNuocCu = 10 + i, ChiSoNuocMoi = 16 + i * 2,
                DonGiaDien = 3500, DonGiaNuoc = 15000, PhiDichVu = 100000,
                DaThanhToan = i < 2, NgayThanhToan = i < 2 ? today.AddDays(-5) : null,
                NguoiTaoId = admin.Id
            };
            d.TongTien = QuanLyService.TinhTong(h.GiaThue, d);
            return d;
        }).ToList();
        db.HoaDons.AddRange(hoaDons);
        await db.SaveChangesAsync();
    }
}
