# Hệ thống quản lý phòng trọ (NhaTro Manager)

**Đồ án cuối kỳ – Điện toán đám mây** · Đề tài **DA05 – Quản lý phòng trọ** · Ứng dụng web đóng gói và triển khai bằng Docker Compose.

## 1. Thành viên – Nhóm 5

| STT | Họ và tên | MSSV | Vai trò |
|---|---|---|---|
| 1 | Trịnh Lâm Huy | 24210501016 | Nhóm trưởng, DevOps (Docker, Compose, kiểm chứng dữ liệu bền vững) |
| 2 | Mã Khánh Tường | 24210501041 | Backend (database, API Phòng, Loại phòng, Người thuê) |
| 3 | Nguyễn Ca Gu | 24210501053 | Backend / Storage (MinIO, API Hợp đồng, Hóa đơn, phân quyền) |
| 4 | Ngô Chí Quyển | 24210501034 | Frontend (giao diện Razor + Bootstrap) |
| 5 | Lê Nhựt Duy | 24210501006 | Kiểm thử, tài liệu, Postman, slide, demo |

## 2. Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend + giao diện | ASP.NET Core 8 (MVC + Web API), Razor View, Bootstrap 5 |
| Database | PostgreSQL 16 (Entity Framework Core) |
| Lưu trữ tệp | MinIO (Object Storage, giao thức S3), bucket `project-files` |
| Bảo mật | Cookie Authentication, phân quyền theo vai trò, mật khẩu băm BCrypt |
| Tài liệu API | Swagger / OpenAPI, Postman Collection |
| Đóng gói | Docker, Docker Compose |

Giao diện tích hợp trong backend (**phương án 1**), nên hệ thống gồm 3 container: `application`, `database`, `minio`.

### Giao diện phiên bản mới

- Bố cục quản trị với header, sidebar điều hướng và vùng nội dung thống nhất.
- Làm mới màu sắc, typography, thẻ thống kê, bảng dữ liệu, biểu mẫu, nút thao tác và trang đăng nhập.
- Bổ sung lớp CSS `backend/wwwroot/css/theme-polish.css`; không thay đổi các route MVC, API, database hoặc cấu hình Docker.
- Có breakpoint CSS cho màn hình nhỏ; cần kiểm thử trực tiếp trên điện thoại trước khi ghi nhận là đã nghiệm thu responsive.

## 3. Yêu cầu môi trường

- Docker Desktop (Windows/macOS) hoặc Docker Engine + Docker Compose plugin (Linux).
- Máy có kết nối Internet ở lần chạy đầu để tải image.
- Các cổng trống: **8080** (web), **9000** và **9001** (MinIO).

## 4. Tạo tệp `.env` từ `.env.example`

```bash
# Linux / macOS / Git Bash
cp .env.example .env

# Windows PowerShell
Copy-Item .env.example .env
```

Mở `.env` và đổi các giá trị `change_this_password`. Không đưa tệp `.env` lên GitHub.

## 5. Chạy hệ thống

```bash
docker compose up -d --build
```

Lần đầu mất vài phút để build image. Ứng dụng tự tạo bảng, nạp dữ liệu mẫu và tạo bucket `project-files`.

## 6. URL giao diện web

http://localhost:8080

## 7. URL API / Swagger

- Swagger UI: http://localhost:8080/swagger
- Cách dùng: gọi `POST /api/auth/login` trong Swagger (hoặc đăng nhập ở giao diện web trong cùng trình duyệt) rồi gọi các API khác.
- Postman: nhập tệp `postman/NhaTro.postman_collection.json`, biến `baseUrl` mặc định `http://localhost:8080`; chạy request **Auth → Login** trước.

## 8. URL MinIO Console

http://localhost:9001 – đăng nhập bằng `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` trong `.env`. Tệp upload nằm trong bucket `project-files`, thư mục `uploads/năm/tháng/`.

## 9. Tài khoản demo

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Administrator | `admin` | `Admin@123` |
| Staff | `staff` | `Staff@123` |

Staff không được xóa dữ liệu, không quản lý loại phòng và tài khoản. Đổi mật khẩu demo trước khi dùng thật.

## 10. Dừng hệ thống

```bash
docker compose down
```

Dữ liệu vẫn được giữ trong named volume `database-data` và `minio-data`. Chạy lại bằng `docker compose up -d`.

## 11. Xóa toàn bộ dữ liệu demo

```bash
docker compose down -v
```

> ⚠️ Lệnh này xóa cả volume. **Không dùng** khi kiểm chứng dữ liệu bền vững.

## Kiểm chứng dữ liệu bền vững

1. `docker compose up -d --build`
2. Tạo một phòng/hợp đồng và upload một ảnh hoặc PDF.
3. `docker compose down`
4. `docker compose up -d`
5. Đăng nhập lại: dữ liệu vẫn còn, tệp vẫn xem/tải được, MinIO Console vẫn thấy object.

## Quy tắc nghiệp vụ chính

- Mỗi phòng có mã duy nhất; trạng thái: Trống, Đang thuê, Bảo trì.
- Một phòng chỉ có tối đa một hợp đồng hiệu lực. Lập hợp đồng → phòng "Đang thuê"; kết thúc hợp đồng → phòng "Trống".
- Mỗi hợp đồng có tối đa một hóa đơn mỗi tháng; chỉ số điện, nước mới không nhỏ hơn chỉ số cũ.
- Tổng hóa đơn = tiền phòng + (điện mới − cũ) × đơn giá điện + (nước mới − cũ) × đơn giá nước + phí dịch vụ.
- Tệp: JPG/PNG tối đa 5 MB, PDF tối đa 10 MB; lưu trong MinIO, metadata (tên gốc, object key, loại, dung lượng, thời điểm, ID thực thể) lưu trong bảng `tep_dinh_kem`.

## Cấu trúc thư mục

```text
source-code/
├── backend/        # ASP.NET Core: Controllers, Services, Models, Views (Razor), wwwroot
├── frontend/       # Ghi chú: giao diện tích hợp trong backend/Views
├── postman/        # Postman Collection
├── Dockerfile
├── compose.yaml
├── .env.example
├── .gitignore
└── README.md
```

## Xử lý sự cố

- **Không pull được image MinIO:** MinIO không còn phát hành image community `minio/minio` trên Docker Hub; dự án dùng `cgr.dev/chainguard/minio`. Nếu máy không pull được, đổi dòng `image:` của service `minio` trong `compose.yaml` sang một object storage/image MinIO tương đương mà máy pull được.
- **Cổng bị chiếm:** đổi cổng bên trái trong mục `ports` của `compose.yaml` (ví dụ `"8081:8080"`).
- **Ứng dụng khởi động chậm:** xem log bằng `docker compose logs -f application`.
