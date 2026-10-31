using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using NhaTro.Web.Data;
using NhaTro.Web.Models;

namespace NhaTro.Web.Services;

/// <summary>Lưu tệp trong MinIO, lưu metadata trong PostgreSQL.</summary>
public class FileService
{
    public const long MaxAnh = 5 * 1024 * 1024;
    public const long MaxPdf = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> ChoPhep = new()
    {
        ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
        ["image/png"] = new[] { ".png" },
        ["application/pdf"] = new[] { ".pdf" }
    };

    private readonly AppDbContext _db;
    private readonly IMinioClient _minio;
    private readonly ILogger<FileService> _log;
    private readonly string _bucket;

    public FileService(AppDbContext db, IMinioClient minio, IConfiguration cfg, ILogger<FileService> log)
    {
        _db = db;
        _minio = minio;
        _log = log;
        _bucket = cfg["Minio:Bucket"] ?? "project-files";
    }

    public async Task EnsureBucketAsync()
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
        if (!exists) await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
    }

    public Task<List<TepDinhKem>> DanhSachAsync(string loaiThucThe, int thucTheId) =>
        _db.TepDinhKems.Where(t => t.LoaiThucThe == loaiThucThe && t.ThucTheId == thucTheId)
            .OrderByDescending(t => t.ThoiDiemUpload).ToListAsync();

    public async Task<(Kq kq, TepDinhKem tep)> UploadAsync(IFormFile file, string loaiThucThe, int thucTheId, int? userId)
    {
        if (file == null || file.Length == 0) return (Kq.That("Vui lòng chọn tệp để tải lên."), null);

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        var tenGoc = Path.GetFileName(file.FileName ?? "");
        var ext = Path.GetExtension(tenGoc).ToLowerInvariant();
        if (!ChoPhep.TryGetValue(contentType, out var exts) || !exts.Contains(ext))
            return (Kq.That("Chỉ chấp nhận ảnh JPG, PNG hoặc tệp PDF."), null);

        var gioiHan = contentType == "application/pdf" ? MaxPdf : MaxAnh;
        if (file.Length > gioiHan)
            return (Kq.That($"Tệp vượt quá dung lượng cho phép ({gioiHan / 1024 / 1024} MB)."), null);
        if (tenGoc.Length > 255) tenGoc = tenGoc.Substring(tenGoc.Length - 255);

        bool tonTai;
        if (loaiThucThe == "Phong") tonTai = await _db.Phongs.AnyAsync(p => p.Id == thucTheId);
        else if (loaiThucThe == "HopDong") tonTai = await _db.HopDongs.AnyAsync(h => h.Id == thucTheId);
        else return (Kq.That("Loại thực thể không hợp lệ (Phong hoặc HopDong)."), null);
        if (!tonTai) return (Kq.That("Không tìm thấy phòng hoặc hợp đồng cần đính kèm tệp.", 404), null);

        await EnsureBucketAsync();

        var id = Guid.NewGuid();
        var now = DateTime.Now;
        var key = "uploads/" + now.ToString("yyyy") + "/" + now.ToString("MM") + "/" + id + ext;

        using (var stream = file.OpenReadStream())
        {
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(contentType));
        }

        var tep = new TepDinhKem
        {
            Id = id, TenGoc = tenGoc, ObjectKey = key, Bucket = _bucket, LoaiTep = contentType,
            DungLuong = file.Length, ThoiDiemUpload = now, LoaiThucThe = loaiThucThe,
            ThucTheId = thucTheId, NguoiUploadId = userId
        };
        _db.TepDinhKems.Add(tep);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            await XoaObjectAsync(_bucket, key);
            throw;
        }
        return (Kq.Thanh(), tep);
    }

    public async Task<(TepDinhKem tep, byte[] data)> TaiAsync(Guid id)
    {
        var tep = await _db.TepDinhKems.FindAsync(id);
        if (tep == null) return (null, null);
        try
        {
            var ms = new MemoryStream();
            await _minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(tep.Bucket)
                .WithObject(tep.ObjectKey)
                .WithCallbackStream(s => s.CopyTo(ms)));
            return (tep, ms.ToArray());
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return (tep, null);
        }
    }

    public async Task<Kq> XoaAsync(Guid id)
    {
        var tep = await _db.TepDinhKems.FindAsync(id);
        if (tep == null) return Kq.That("Không tìm thấy tệp.", 404);
        await XoaObjectAsync(tep.Bucket, tep.ObjectKey);
        _db.TepDinhKems.Remove(tep);
        await _db.SaveChangesAsync();
        return Kq.Thanh();
    }

    /// <summary>Xóa toàn bộ tệp của một phòng/hợp đồng (gọi khi xóa thực thể đó).</summary>
    public async Task XoaTheoThucTheAsync(string loaiThucThe, int thucTheId)
    {
        var teps = await _db.TepDinhKems.Where(t => t.LoaiThucThe == loaiThucThe && t.ThucTheId == thucTheId).ToListAsync();
        foreach (var t in teps) await XoaObjectAsync(t.Bucket, t.ObjectKey);
        _db.TepDinhKems.RemoveRange(teps);
        await _db.SaveChangesAsync();
    }

    private async Task XoaObjectAsync(string bucket, string key)
    {
        try
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(key));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Không xóa được object {Key}", key);
        }
    }
}
