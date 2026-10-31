using Microsoft.EntityFrameworkCore;
using NhaTro.Web.Models;

namespace NhaTro.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<LoaiPhong> LoaiPhongs => Set<LoaiPhong>();
    public DbSet<Phong> Phongs => Set<Phong>();
    public DbSet<NguoiThue> NguoiThues => Set<NguoiThue>();
    public DbSet<HopDong> HopDongs => Set<HopDong>();
    public DbSet<HoaDon> HoaDons => Set<HoaDon>();
    public DbSet<TepDinhKem> TepDinhKems => Set<TepDinhKem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<NguoiDung>().ToTable("nguoi_dung");
        b.Entity<LoaiPhong>().ToTable("loai_phong", t => t.HasCheckConstraint("ck_loai_phong_gia", "gia_niem_yet >= 0"));
        b.Entity<Phong>().ToTable("phong");
        b.Entity<NguoiThue>().ToTable("nguoi_thue");
        b.Entity<HopDong>().ToTable("hop_dong", t =>
        {
            t.HasCheckConstraint("ck_hop_dong_ngay", "ngay_ket_thuc > ngay_bat_dau");
            t.HasCheckConstraint("ck_hop_dong_tien", "gia_thue >= 0 AND tien_coc >= 0");
        });
        b.Entity<HoaDon>().ToTable("hoa_don", t =>
        {
            t.HasCheckConstraint("ck_hoa_don_dien", "chi_so_dien_moi >= chi_so_dien_cu");
            t.HasCheckConstraint("ck_hoa_don_nuoc", "chi_so_nuoc_moi >= chi_so_nuoc_cu");
        });
        b.Entity<TepDinhKem>().ToTable("tep_dinh_kem");

        b.Entity<NguoiDung>().HasIndex(x => x.TenDangNhap).IsUnique();
        b.Entity<LoaiPhong>().HasIndex(x => x.TenLoai).IsUnique();
        b.Entity<Phong>().HasIndex(x => x.MaPhong).IsUnique();
        b.Entity<NguoiThue>().HasIndex(x => x.Cccd).IsUnique();
        b.Entity<NguoiThue>().HasIndex(x => x.HoTen);
        b.Entity<NguoiThue>().HasIndex(x => x.SoDienThoai);
        b.Entity<HopDong>().HasIndex(x => x.SoHopDong).IsUnique();
        b.Entity<HoaDon>().HasIndex(x => new { x.HopDongId, x.Thang }).IsUnique();
        b.Entity<TepDinhKem>().HasIndex(x => x.ObjectKey).IsUnique();
        b.Entity<TepDinhKem>().HasIndex(x => new { x.LoaiThucThe, x.ThucTheId });

        b.Entity<Phong>().HasOne(x => x.LoaiPhong).WithMany().HasForeignKey(x => x.LoaiPhongId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HopDong>().HasOne(x => x.Phong).WithMany().HasForeignKey(x => x.PhongId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HopDong>().HasOne(x => x.NguoiThue).WithMany().HasForeignKey(x => x.NguoiThueId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HoaDon>().HasOne(x => x.HopDong).WithMany().HasForeignKey(x => x.HopDongId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HopDong>().HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiTaoId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<HoaDon>().HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiTaoId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<TepDinhKem>().HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.NguoiUploadId).OnDelete(DeleteBehavior.SetNull);
    }
}
