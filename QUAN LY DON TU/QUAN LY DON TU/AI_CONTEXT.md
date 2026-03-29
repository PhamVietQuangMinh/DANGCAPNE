# PROJECT CONTEXT: QUAN LY DON TU (Employee Request Management)

## 1. Tech Stack
- **Framework:** ASP.NET Core MVC (.NET 8.0)
- **Database:** PostgreSQL (Hosted on Supabase)
- **ORM:** Entity Framework Core (EF Core)
- **Frontend / UI:** 
  - Vanilla HTML/CSS/JS with Razor Views (`.cshtml`)
  - **Style:** Custom **Glassmorphism** theme, smooth animations (350ms).
  - **Icons:** Bootstrap Icons (`bi-icon`)
- **Real-Time features:** SignalR (for instant notifications)
- **AI Integration:** Google Gemini API (used for Smart Form filling and general Chat)

## 2. Core Entities & Database Schema
- **User / Employee:** `Users`, `Roles`, `Departments`, `UserManagers`.
  - Fixed Shifts: Default is `08:00 - 18:00`.
- **Form Templates (`FormTemplates`):** 10 predefined templates:
  1. Đơn xin nghỉ phép (Leave)
  2. Đơn làm thêm giờ (OT)
  3. Đơn đi công tác (Travel)
  4. Đơn tạm ứng chi phí (Expense) - *Requires Financial Approval (PIN)*
  5. Đơn yêu cầu cấp phát thiết bị (Equipment)
  6. Đơn xin nghỉ việc (Leave)
  7. Đơn cập nhật thông tin nhân sự (Other)
  8. Đơn khiếu nại công ca (Attendance)
  9. Đơn đăng ký tài sản/văn phòng phẩm (Equipment)
  10. Đơn đề xuất/Đổi ca làm việc (Other)
- **Requests (`Requests`):** Primary tracking of what an employee submits. Connected to `FormData` for dynamic field inputs.
- **Approvals (`RequestApprovals`):** Tracks the multi-step approval workflow.
- **System:** `Notifications`, `AuditLogs`, `DraftRequests`.

## 3. Workflows (Luồng duyệt)
1. **Luồng duyệt cơ bản (Basic):** Trưởng phòng (Role 3) -> HR (Role 2). Used by most daily forms.
2. **Luồng duyệt tài chính (Financial):** Kế toán (SpecificUser:6) -> Giám đốc (SpecificUser:1). Requires PIN (`1234`).
3. **Luồng duyệt vượt cấp (Escalation):** Trưởng phòng -> HR -> Giám đốc. Used for important updates (e.g. Updating user info).

## 4. Special Custom Logic (Do NOT overwrite)
- **Admin Super-Approval:** In `RequestsController.Detail` and `ApprovalsController`, Admins have the ability to explicitly override and "Approve" any pending step in any workflow, even if they aren't the designated approver.
- **Global CC Notifications:** Admins receive CC notifications whenever a new request is created, or whenever another user (HR/Manager) approves or rejects a request.
- **Edit & Resubmit Flow:** When a request is marked as "Yêu cầu sửa" (RequestEdit), the Requester can use the `Edit` action (`RequestsController`) to modify and resubmit the form. This resets all `RequestApprovals` to `Pending` and restarts the workflow.
- **Glassmorphism CSS:** Core styles are in `wwwroot/css/site.css` (using `backdrop-filter`, `rgba` colors, gradient backgrounds, and `.glass-card`). Animations are intentionally fast (`150ms-350ms`) for a snappy feel.
- **AI Form Filling:** Uses `AnalyzeIntent` in `RequestsController.cs` and `gemini.js` to parse natural language into form fields using sessionStorage (`ai_formData_[id]`).
- **Profile Synchronization:** When form template 7 (Update Info) is approved, it extracts `new_fullname`, `new_phone`, `new_email` and automatically updates the User's profile in the database.

## 5. Roles
- **Role 1:** Admin (Giám đốc / Super Admin) - Can see everything, approve everything.
- **Role 2:** HR (Nhân sự) - Standard step 2 approver.
- **Role 3:** Manager (Trưởng phòng) - Standard step 1 approver.
- **Role 4:** Employee (Nhân viên) - Submits requests.

---
*Note to AI System:* Parse this file initially to understand the project architecture before suggesting sweeping database/controller changes. Avoid breaking the Glassmorphism UI or modifying the existing 10 Form Templates unless explicitly requested by the user.
