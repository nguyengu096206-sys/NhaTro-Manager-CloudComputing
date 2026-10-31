# NhaTro Manager – Cloud Computing

## Giới thiệu

Đồ án cuối kỳ môn Điện toán đám mây (Cloud Computing) – Nhóm 5.

NhaTro Manager là hệ thống website hỗ trợ quản lý phòng trọ, người thuê, hợp đồng, hóa đơn và tài khoản.

## Công nghệ sử dụng

- ASP.NET Core 8 – Xây dựng ứng dụng web
- PostgreSQL 16 – Quản lý cơ sở dữ liệu
- MinIO – Lưu trữ đối tượng
- Docker & Docker Compose – Triển khai hệ thống
- HTML, CSS, JavaScript – Giao diện người dùng

## Chức năng chính

- Quản lý phòng trọ và loại phòng
- Quản lý thông tin người thuê
- Quản lý hợp đồng thuê phòng
- Quản lý hóa đơn và thanh toán
- Quản lý tài khoản người dùng
- Thống kê tình trạng phòng trọ

## Hướng dẫn chạy

Yêu cầu: Máy tính đã cài Docker Desktop.

**Bước 1:** Tải source code từ GitHub.

**Bước 2:** Mở Terminal tại thư mục `source-code`.

**Bước 3:** Sao chép `.env.example` thành `.env` và cấu hình các biến môi trường.

**Bước 4:** Khởi chạy hệ thống:

```bash
docker compose up -d --build
```

**Bước 5:** Truy cập ứng dụng:

- Website: http://localhost:8080
- MinIO Console: http://localhost:9001

## Cấu trúc đồ án

- `source-code/` – Mã nguồn và Docker
- `documents/` – Báo cáo, slide và sơ đồ
- `ảnh minh chứng

## Nhóm thực hiện

Nhóm 5 – Đồ án cuối kỳ Điện toán đám mây.
