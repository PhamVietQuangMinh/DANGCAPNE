---
name: dangcapne-request-system
description: ASP.NET Core MVC app for internal request approval, attendance, HR dashboard, and Supabase/PostgreSQL via EF Core. Use when editing Controllers, Razor views, DbContext, migrations, session auth, or Vietnamese UI in this repository.
---

# DANGCAPNE — hệ thống đơn từ / phê duyệt

## Vị trí code chính

- Solution: `DANGCAPNE.sln` (repo root).
- Web app: `QUAN LY DON TU/QUAN LY DON TU/` (project `DANGCAPNE.csproj`).
- DbContext: `Data/ApplicationDbContext.cs`.
- Controllers: `Controllers/`.
- Views: `Views/`.

## Stack & mô hình

- ASP.NET Core MVC, Entity Framework Core, Razor.
- Đăng nhập / phân quyền qua **session** (`UserId`, `TenantId`, `Roles`, …) — kiểm tra pattern hiện có trong controller trước khi đổi.
- Đa tenant: lọc theo `TenantId` khi truy vấn dữ liệu nghiệp vụ.

## Khi chỉnh truy vấn EF

- Đọc/chỉ đọc: ưu tiên `.AsNoTracking()` cho action chỉ hiển thị dữ liệu.
- Giữ thay đổi tối thiểu: khớp style và naming hiện có trong file.

## Bảo mật & cấu hình

- Không đưa mật khẩu DB hoặc API key vào skill; dùng User Secrets / biến môi trường cho production.

## Tài liệu thêm (tùy chọn)

- Chi tiết cấu trúc module: xem `.cursor/rules/dangcapne-project.mdc`.
