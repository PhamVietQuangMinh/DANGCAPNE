---
name: gemini-ai-service
description: Cách dùng Gemini AIService trong project — gọi AI, prompt, xử lý response. Dùng khi thêm tính năng AI, chỉnh prompt, hoặc gọi GeminiService.
---

# Gemini AI Service — DANGCAPNE

## Vị trí

`Services/GeminiAIService.cs` — dùng API key từ `Gemini:ApiKey` trong `appsettings.json`.

## Gọi Gemini

```csharp
private readonly GeminiAIService _geminiService;

public XxxController(GeminiAIService geminiService)
{
    _geminiService = geminiService;
}

// Gọi đồng bộ
var response = await _geminiService.CallGeminiAsync("Prompt ở đây");

// Response trả về string — parse tùy nghiệp vụ
```

## Prompt mẫu

```csharp
var prompt = $@"
Bạn là trợ lý HR. Phân tích đơn xin nghỉ phép sau:
- Nhân viên: {employee.Name}
- Loại: {leaveType}
- Từ ngày: {startDate}
- Đến ngày: {endDate}
- Số ngày: {days}

Trả lời ngắn gọn: Đồng ý hay từ chối? Tại sao?
";

var advice = await _geminiService.CallGeminiAsync(prompt);
```

## Lưu ý

- **KHÔNG** commit API key thật vào `appsettings.json`.
- Dùng `dotnet user-secrets` để lưu key ở máy dev.
- Gemini trả về text thuần; cần parse/validate trước khi dùng cho nghiệp vụ tự động.
- Nếu gặp lỗi network, xử lý `try/catch` và fallback hợp lý.

