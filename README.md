# 🌌 OlapAnalytics: AI-Powered Multi-Dimensional Intelligence

[![Build Status](https://img.shields.io/badge/Build-Success-brightgreen.svg)]()
[![Backend](https://img.shields.io/badge/.NET-8.0-blue.svg)]()
[![Frontend](https://img.shields.io/badge/React-18.0-61dafb.svg)]()
[![AI](https://img.shields.io/badge/Google-Gemini_Pro-8b5cf6.svg)]()
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)]()

**OlapAnalytics** là nền tảng Business Intelligence (BI) thế hệ mới, kết hợp sức mạnh của **Generative AI (Gemini)** với kiến trúc **OLAP (SSAS)** truyền thống. Hệ thống cho phép biến dữ liệu thô từ file Excel/CSV thành một kho dữ liệu (Data Warehouse) hoàn chỉnh và khối lập phương đa chiều (SSAS Cube) chỉ trong vài giây thông qua quy trình tự động hóa hoàn toàn.

![Dashboard Preview](C:\Users\xpaga\.gemini\antigravity\brain\e952bee2-3a7d-44f0-95b0-56eda628c031\olap_analytics_dashboard_mockup_1776954216289.png)

---

## 📖 Mục lục (Table of Contents)
- [Giới thiệu](#-giới-thiệu)
- [Tính năng chính](#-tính- năng-chính)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Hướng dẫn Cài đặt](#-hướng-dẫn-cài-đặt)
- [Hướng dẫn Sử dụng](#-hướng-dẫn-sử-dụng)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Đóng góp](#-đóng-góp)
- [Giấy phép](#-giấy-phép)

---

## 📝 Giới thiệu
Dự án giải quyết vấn đề phức tạp trong việc xây dựng hệ thống báo cáo đa chiều. Thông thường, việc thiết kế Star Schema và triển khai SSAS Cube mất nhiều ngày làm việc của Data Engineer. Với **OlapAnalytics**, mọi thứ được tự động hóa bởi AI, giúp doanh nghiệp tiếp cận dữ liệu phân tích ngay lập tức.

---

## ✨ Tính năng chính
- **🤖 AI-Driven Star Schema**: Tự động phân tích và thiết kế mô hình dữ liệu từ file thô.
- **🔍 Auto-Discovery**: Tự động nhận diện cấu trúc SQL Server và SSAS.
- **📊 Interactive Dashboard**: Biểu đồ Glassmorphism với khả năng Drill-down sâu.
- **⚡ Smart Caching**: Cơ chế cache metadata tách biệt theo từng tenant/kết nối.
- **🌐 Multi-dimensional Filtering**: Lọc dữ liệu linh hoạt theo nhiều chiều thời gian và thuộc tính.

---

## 🏗️ Kiến trúc Hệ thống
Dự án tuân thủ mô hình **Clean Architecture** với 4 lớp chính ở Backend và kiến trúc **Component-based** ở Frontend, đảm bảo tính tách biệt và dễ bảo trì.

---

## 🛠️ Hướng dẫn Cài đặt (Installation)

### 1. Yêu cầu hệ thống
- **Windows 10/11** (Bắt buộc để chạy SSAS).
- **.NET SDK 8.0** & **Node.js 18+**.
- **SQL Server 2022** & **SSAS (Multidimensional Mode)**.

### 2. Các bước thiết lập
1. **Clone project**:
   ```bash
   git clone https://github.com/username/olapanalytics.git
   cd olapanalytics
   ```
2. **Cấu hình Database**: Tạo một Database trống trong SQL Server.
3. **Cấu hình Gemini**: Lấy API Key tại [Google AI Studio](https://aistudio.google.com/).
4. **Khởi động**: Chạy script tự động (bypass security blocks):
   ```powershell
   node run.js
   ```

---

## 📖 Hướng dẫn Sử dụng (Usage)

### Bước 1: Cấu hình kết nối
Vào trang **Settings**, điền thông tin máy chủ SQL và SSAS. Hệ thống sẽ tự động tìm kiếm các Database/Catalog khả dụng. Lưu ý dán API Key của Gemini để kích hoạt tính năng AI.

### Bước 2: Tải dữ liệu & Phân tích
Tải lên file dữ liệu (.csv, .xlsx) tại trang **Upload**. Nhấn "Bắt đầu phân tích" để AI thực hiện quy trình: *Phân tích -> Thiết kế DW -> Nạp dữ liệu -> Deploy Cube*.

### Bước 3: Khám phá Dashboard
Sử dụng các thẻ KPI và biểu đồ để theo dõi xu hướng. Click trực tiếp vào dữ liệu để **Drill-down** chi tiết.

---

## 💻 Công nghệ sử dụng (Tech Stack)
- **Frontend**: React, Vite, TypeScript, Vanilla CSS (Glassmorphism).
- **Backend**: .NET 8, Dapper, Adomd.NET, SQL Client.
- **AI**: Google Gemini Pro (Generative AI).
- **Storage/OLAP**: SQL Server, Analysis Services.

---

## 🤝 Đóng góp (Contributing)
Chúng tôi luôn hoan nghênh mọi sự đóng góp để dự án hoàn thiện hơn:
1. Fork dự án.
2. Tạo nhánh tính năng (`git checkout -b feature/AmazingFeature`).
3. Commit thay đổi (`git commit -m 'Add some AmazingFeature'`).
4. Push lên nhánh (`git push origin feature/AmazingFeature`).
5. Mở một Pull Request.

---

## 📄 Giấy phép (License)
Phân phối dưới giấy phép **MIT**. Xem `LICENSE` để biết thêm thông tin.

---
🚀 **OlapAnalytics** - *Powering your data, elevating your business.*
