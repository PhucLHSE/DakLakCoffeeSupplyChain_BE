## ☕ Feature: [Tên tính năng/API ngắn gọn]

### 📌 Objective
[Mô tả mục tiêu của PR: bạn đang thêm, sửa, hoặc cải tiến điều gì? Liên quan đến vai trò hoặc luồng nào?]

---

### ✅ Key Changes
- [Thay đổi chính 1: triển khai API, thêm service, sửa logic...]
- [Thay đổi chính 2: mapping, validation, role check, truy vấn DB...]
- [Thay đổi chính 3: xử lý bug, refactor, thêm helper…]

---

### 🧱 Affected Files
- `[TênFile1].cs`
- `[TênFile2].cs`
- `[TênInterface].cs`
- `[Tên DTO hoặc Mapper].cs`
- `[SQL hoặc Migration nếu có]`

---

### 📁 Added
- `[File/Component mới được thêm]`

---

### 🧪 How to Test
1. **Login** bằng role phù hợp (VD: `BusinessManager`) để lấy JWT token
2. Gửi request đến endpoint:
   - `POST /api/businessbuyers` (hoặc endpoint bạn thêm)
   - Header: `Authorization: Bearer {token}`
3. Body mẫu:
```json
{
  "CompanyName": "Công ty ABC",
  "TaxId": "1234567890",
  "Phone": "0901234567",
  "Email": "abc@company.com",
  "Address": "123 Đắk Lắk"
}
```
---

### 🧪 Test Cases
- [x] ✅ [Tên case test 1]: mô tả ngắn và kết quả mong đợi  
- [x] ❌ [Tên case test 2]: mô tả tình huống lỗi bị chặn đúng  
- [x] [Tên case test 3]: tạo thành công, đúng dữ liệu/trường liên quan  

---

### 🔍 Notes
- [Ghi chú kỹ thuật đặc biệt: EF limitation, logic token, datetime, role...]
- [Yêu cầu frontend nào? hoặc tác động đến role nào?]
- 💡 [Gợi ý phát triển thêm nếu có, ví dụ: thêm phân trang, export CSV...]

---

### 🔗 Related
- Modules: `[Tên domain liên quan: Product, Contract, etc.]`
- Roles: `[Admin / BusinessManager / Expert...]`

---
