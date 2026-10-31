namespace NhaTro.Web.Services;

/// <summary>Kết quả của một thao tác nghiệp vụ (thành công hoặc lỗi kèm mã HTTP tương ứng).</summary>
public class Kq
{
    public bool Ok { get; set; }
    public int Status { get; set; } = 400;
    public string Loi { get; set; }

    public static Kq Thanh() => new Kq { Ok = true, Status = 200 };
    public static Kq That(string loi, int status = 400) => new Kq { Ok = false, Status = status, Loi = loi };
}
