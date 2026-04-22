# 🌌 OlapAnalytics: Next-Gen Business Intelligence Platform

[![Build Status](https://img.shields.io/badge/Build-Success-brightgreen.svg)]()
[![Backend](https://img.shields.io/badge/.NET-10.0-blue.svg)]()
[![Frontend](https://img.shields.io/badge/React-18.0-61dafb.svg)]()
[![Database](https://img.shields.io/badge/SQL_Server-2022-red.svg)]()

**OlapAnalytics** là một nền tảng phân tích dữ liệu chuyên sâu, được thiết kế để biến những tập dữ liệu thô từ SQL Server và SSAS thành những thông tin kinh doanh giá trị. Với giao diện **Glassmorphism** thời thượng và khả năng truy vấn đa chiều mạnh mẽ, OlapAnalytics đặt ra tiêu chuẩn mới cho các ứng dụng BI hiện đại.

---

## ✨ Điểm nhấn Công nghệ (Highlights)

### 📊 Trải nghiệm Phân tích Đỉnh cao
- **Dynamic MDX Engine**: Bộ máy xử lý tự động tạo truy vấn MDX phức tạp, loại bỏ sự cần thiết phải biết mã lệnh phía người dùng.
- **Deep Drill-Down Architecture**: Cho phép đi sâu vào từng tầng dữ liệu (Hierarchy) một cách mượt mà nhờ cơ chế phân cấp thông minh.
- **Neon Analytics Dashboard**: Hệ thống biểu đồ phản hồi tức thì với hiệu ứng ánh sáng Neon và độ trong suốt pha lê.

### 🔐 Bảo mật & Hiệu năng
- **Glass-Auth System**: Giao diện xác thực với hiệu ứng trượt (Sliding Animation) sử dụng `cubic-bezier` mang lại cảm giác cao cấp.
- **Smart Caching**: Tối ưu hóa tốc độ tải dữ liệu thông qua cơ chế lưu trữ đệm tại Backend.
- **Multi-Tenant Ready**: Kiến trúc sẵn sàng cho việc quản lý nhiều khách hàng với kết nối SQL/SSAS riêng biệt.

---

## 🏗️ Kiến trúc Hệ thống (Architecture)

Dự án được xây dựng theo mô hình **Clean Architecture**, đảm bảo khả năng mở rộng và bảo trì dễ dàng:

### 🔹 Backend (.NET Core 10)
- **API Layer**: Cung cấp các RESTful Endpoints bảo mật bằng JWT.
- **Application Layer**: Chứa logic xử lý nghiệp vụ, dịch vụ phân tích và chuyển đổi DTO.
- **Domain Layer**: Định nghĩa các thực thể cốt lõi và interface.
- **Infrastructure Layer**: Thực thi các truy vấn MDX thông qua Adomd.NET và quản lý kết nối SQL Server qua Dapper.

### 🔸 Frontend (React + TypeScript)
- **State Management**: Sử dụng React Hooks và Context API cho luồng dữ liệu đồng bộ.
- **Visualizations**: Tích hợp Chart.js với tùy biến sâu về giao diện Neon.
- **Styling**: Sử dụng Vanilla CSS với hệ thống biến (Variables) mạnh mẽ, hỗ trợ Dark Mode hoàn hảo.

---

## 🛠️ Hướng dẫn Triển khai (Deployment)

### 1️⃣ Chuẩn bị Môi trường
| Công cụ | Yêu cầu tối thiểu |
| :--- | :--- |
| **.NET SDK** | Version 8.0 or 10.0 |
| **Node.js** | Version 18.x (LTS) |
| **SQL Server** | Developer Edition hoặc cao hơn |
| **SSAS** | Multidimensional Mode |

### 2️⃣ Cài đặt Cơ sở dữ liệu
1. Mở SQL Server Management Studio (SSMS).
2. Chạy file script tại `database/AppDb.sql` để khởi tạo Warehouse.
3. Đảm bảo Cube SSAS đã được Deploy và có tên là `Travel DW` (hoặc cập nhật trong cấu hình).

### 3️⃣ Cấu hình Kết nối
Chỉnh sửa file `backend/OlapAnalytics.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AppAnalytics;Trusted_Connection=True;"
  }
}
```

### 4️⃣ Khởi động Hệ thống
Bạn chỉ cần thực hiện **01 câu lệnh duy nhất** tại thư mục gốc:

```powershell
./start-app.ps1
```
*Script sẽ tự động mở đồng thời Backend API và Frontend UI trong các cửa sổ riêng biệt.*

---


## 📸 Ảnh chụp Giao diện (Screenshots)
*(Dự án hỗ trợ hiển thị tuyệt vời trên mọi độ phân giải)*

- **Dashboard**: Biểu đồ Neon 3D, KPI Cards.
- **Pivot Table**: Heatmap tích hợp, lọc đa điều kiện.
- **Auth Page**: Hiệu ứng Sliding mượt mà 60fps.

---
🚀 **OlapAnalytics** - *Powering your data, elevating your business.*
