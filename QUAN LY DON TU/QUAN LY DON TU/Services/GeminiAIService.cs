using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace DANGCAPNE.Services
{
    public class GeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private const string _geminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=";
        public GeminiAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<string> AnalyzeDocumentAsync(string filePath, string contentType)
        {
            if (string.IsNullOrEmpty(_apiKey)) return "Chưa cấu hình Gemini API Key.";
            
            try
            {
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var base64File = Convert.ToBase64String(fileBytes);

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Hãy phân tích chứng từ này. Trích xuất các thông tin quan trọng. Tính toàn vẹn của tài liệu có đáng tin cậy không, hay có dấu hiệu chỉnh sửa/sao chép/làm giả/photoshop không? Trả lời bằng tiếng Việt ngắn gọn, trình bày theo cấu trúc rõ ràng." },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = contentType,
                                        data = base64File
                                    }
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_geminiEndpoint}{_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseString)!;
                    return $"🤖 **AI Assistant:**\n\n{result.candidates[0].content.parts[0].text}";
                }
                
                return $"🤖 **AI Assistant:** Lỗi khi gọi API AI ({(int)response.StatusCode}). Vui lòng kiểm tra lại cấu hình.";
            }
            catch (Exception ex)
            {
                return $"🤖 **AI Assistant:** Lỗi: {ex.Message}";
            }
        }

        public async Task<string> ParseRequestIntentAsync(string prompt, string templatesJson)
        {
             if (string.IsNullOrEmpty(_apiKey)) return "{}";
            
             var currentDate = DateTime.Now.ToString("dd/MM/yyyy");
             var systemPrompt = $@"Bạn là trợ lý ảo giúp nhân viên tạo đơn. Hôm nay là ngày {currentDate} (định dạng dd/MM/yyyy).
Dưới đây là danh sách các biểu mẫu (Form Templates) hiện có trong hệ thống dưới dạng JSON (mỗi field có FieldName, Label và FieldType):
{templatesJson}

Hãy đọc yêu cầu của nhân viên: '{prompt}'

Lưu ý: Mọi đơn đều luôn có thêm 2 trường mặc định là:
- FieldName: ""Title"" (Tiêu đề đơn, bạn tự sinh ra một chuyên nghiệp dựa theo yêu cầu. VD: ""Đơn xin nghỉ phép - Ốm 2 ngày"")
- FieldName: ""Priority"" (Mức độ ưu tiên, giá trị hợp lệ: ""Normal"", ""Low"", ""High"", ""Urgent"")

Quy tắc Xử lý Ngày tháng (QUAN TRỌNG):
- Đầu ra của tất cả các FieldType là ""Date"" BẮT BUỘC phải theo định dạng chuẩn HTML5: yyyy-MM-dd. Ví dụ: Nếu hôm nay là 12/03/2026, thì ghi là ""2026-03-12"".
- Nếu người dùng nói ""nghỉ 2 ngày từ hôm nay"": ""Từ ngày"" là hôm nay, ""Đến ngày"" tính bằng cách cộng (Số ngày - 1) vào ""Từ ngày"". Ví dụ nghỉ 2 ngày từ 12/03 thì ""Đến ngày"" là 13/03 (2026-03-13). Nghỉ 1 ngày thì Từ và Đến giống nhau.
- FieldName: ""Priority"" (Mức độ ưu tiên, giá trị hợp lệ: ""Normal"", ""Low"", ""High"", ""Urgent"")

Nhiệm vụ:
1. Xác định TemplateId (ID của biểu mẫu) phù hợp nhất.
2. Trích xuất thông tin để điền vào trường ""Title"" và ""Priority"".
3. Khớp thông tin từ yêu cầu vào càng nhiều FieldName của biểu mẫu càng tốt (bao gồm các loại nghỉ phép, ngày tháng, lý do, số tiền). Đảm bảo key trong FormData PHẢI TRÙNG KHỚP VỚI FieldName.
Trả về KẾT QUẢ DUY NHẤT LÀ MỘT CHUỖI JSON.
Cấu trúc JSON mẫu:
{{
  ""TemplateId"": 1,
  ""FormData"": {{
    ""Title"": ""Đơn xin nghỉ ốm 2 ngày"",
    ""Priority"": ""Normal"",
    ""LeaveType"": ""Ốm đau"",
    ""StartDate"": ""2023-10-25"",
    ""Reason"": ""Sốt siêu vi""
  }}
}}";
             
             try
             {
                 var payload = new
                 {
                     contents = new[]
                     {
                         new { parts = new[] { new { text = systemPrompt } } }
                     }
                 };

                 var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                 
                 int maxRetries = 2;
                 for (int i = 0; i < maxRetries; i++)
                 {
                     var response = await _httpClient.PostAsync($"{_geminiEndpoint}{_apiKey}", content);

                     if (response.IsSuccessStatusCode)
                     {
                         var responseString = await response.Content.ReadAsStringAsync();
                         dynamic result = JsonConvert.DeserializeObject(responseString)!;
                         string aiText = result.candidates[0].content.parts[0].text.ToString();
                         
                         // Clean up markdown fences
                         aiText = aiText.Replace("```json", "").Replace("```", "").Trim();
                         return aiText;
                     }
                     else if ((int)response.StatusCode == 429) 
                     {
                         await Task.Delay(2000 * (i + 1)); 
                     }
                     else
                     {
                         var errorResponse = await response.Content.ReadAsStringAsync();
                         Console.WriteLine($"[Gemini API Error] {response.StatusCode} - {errorResponse}");
                         return $"{{\"error\": \"API Error: {response.StatusCode}\"}}";
                     }
                 }
                 return "{}"; 
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[Gemini Exception] {ex.ToString()}");
                 return "{}";
             }
        }

        public async Task<string> GeneralChatAsync(List<ChatMessage> history)
        {
             if (string.IsNullOrEmpty(_apiKey)) return "Xin lỗi, hệ thống chưa được cấu hình API Key cho A.I.";

             var systemPrompt = @"Bạn là trợ lý ảo siêu thông minh, thân thiện của DANGCAPNE - Hệ thống Quản lý Đơn từ & Phê duyệt.
Nhiệm vụ của bạn là giải đáp thắc mắc, hướng dẫn sử dụng hệ thống, và hỗ trợ nhân viên các vấn đề chung một cách chuyên nghiệp. Mọi câu trả lời cần ngắn gọn, đi thẳng vào trọng tâm.";

             try
             {
                 var contentsList = new List<object>();
                 
                 // Thêm system prompt như một system instruction hoặc chèn vào đầu
                 contentsList.Add(new { role = "user", parts = new[] { new { text = systemPrompt } } });
                 contentsList.Add(new { role = "model", parts = new[] { new { text = "Vâng, tôi đã hiểu nhiệm vụ của mình." } } });

                 foreach (var msg in history)
                 {
                     string role = msg.Role == "ai" ? "model" : "user";
                     contentsList.Add(new { role = role, parts = new[] { new { text = msg.Text } } });
                 }

                 var payload = new
                 {
                     contents = contentsList.ToArray()
                 };

                 var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                 var response = await _httpClient.PostAsync($"{_geminiEndpoint}{_apiKey}", content);

                 if (response.IsSuccessStatusCode)
                 {
                     var responseString = await response.Content.ReadAsStringAsync();
                     dynamic result = JsonConvert.DeserializeObject(responseString)!;
                     string reply = result.candidates[0].content.parts[0].text;
                     return reply.Trim();
                 }
                 else
                 {
                     var err = await response.Content.ReadAsStringAsync();
                     Console.WriteLine($"[Gemini Chat Error] {err}");
                     return "Có lúi húi chút lỗi xíu rồi, bạn thử lại nha.";
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[Gemini Chat Exception] {ex.ToString()}");
                 return "Xin lỗi, hiện tại tôi không thể xử lý, vui lòng thử lại sau.";
             }
        }

        public async Task<MedicalCertAnalysisResult> AnalyzeMedicalCertificateAsync(string filePath, string contentType)
        {
            var failResult = new MedicalCertAnalysisResult
            {
                Verdict = "FAIL",
                Score = 0,
                Summary = "Không thể phân tích tài liệu.",
                Checks = new List<MedicalCertCheck>()
            };

            if (string.IsNullOrEmpty(_apiKey))
            {
                failResult.Summary = "Chưa cấu hình Gemini API Key.";
                return failResult;
            }

            try
            {
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var base64File = Convert.ToBase64String(fileBytes);

                var prompt = @"Bạn là chuyên gia kiểm tra chứng từ y tế của Việt Nam. Hãy phân tích hình ảnh được cung cấp và xác định đây có phải là chứng từ y tế hợp lệ dùng để xin nghỉ phép hay không (bao gồm các loại hợp lệ: GIẤY RA VIỆN, GIẤY CHỨNG NHẬN NGHỈ VIỆC HƯỞNG BHXH, GIẤY KHÁM BỆNH có chỉ định cho nghỉ).

Hãy kiểm tra các tiêu chí sau và trả về KẾT QUẢ DUY NHẤT LÀ JSON (không có markdown, không có text thêm):

{
  ""verdict"": ""PASS"" hoặc ""FAIL"" hoặc ""SUSPICIOUS"",
  ""score"": <số từ 0-100 đánh giá độ tin cậy>,
  ""summary"": ""<tóm tắt ngắn gọn kết quả bằng tiếng Việt>"",
  ""missing_fields"": [""<trường thiếu>"", ...],
  ""checks"": [
    {
      ""id"": ""template"",
      ""label"": ""Loại biểu mẫu hợp pháp"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn>""
    },
    {
      ""id"": ""seal_signature"",
      ""label"": ""Dấu và chữ ký"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn>""
    },
    {
      ""id"": ""image_integrity"",
      ""label"": ""Tính toàn vẹn ảnh (cắt ghép/chỉnh sửa)"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn>""
    },
    {
      ""id"": ""required_fields"",
      ""label"": ""Thông tin bắt buộc đầy đủ"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn — liệt kê trường thiếu nếu có>""
    },
    {
      ""id"": ""icd_diagnosis"",
      ""label"": ""Chẩn đoán & Mã ICD"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn>""
    },
    {
      ""id"": ""date_logic"",
      ""label"": ""Tính hợp lý ngày tháng & thời gian nghỉ"",
      ""status"": ""PASS"" hoặc ""FAIL"" hoặc ""WARN"",
      ""detail"": ""<giải thích ngắn>""
    }
  ]
}

Quy tắc đánh giá:
- verdict = PASS: Các tiêu chí quan trọng đều ổn. Nếu là 'Giấy ra viện' hợp lệ thì vẫn duyệt PASS dù không ghi rõ số ngày nghỉ ngơi nhưng có lời dặn của bác sĩ.
- verdict = SUSPICIOUS: Có dấu hiệu mờ, tẩy xóa sơ sài nhưng chưa chắc là giả.
- verdict = FAIL: Không phải tài liệu y tế, hoặc phát hiện dấu hiệu chỉnh sửa Photoshop/giả mạo chữ ký, làm giả con dấu rõ ràng.
- Giấy y tế hợp lệ thường có: tên bệnh viện, họ tên bệnh nhân, chẩn đoán, ngày vào/ra viện/khám, chữ ký và con dấu đỏ. Mấu chốt là tính xác thực mộc đỏ và chữ ký.
- Trả lời NGAY JSON THUẦN TÚY, không thêm bất kỳ text nào khác.";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = contentType,
                                        data = base64File
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        responseMimeType = "application/json"
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_geminiEndpoint}{_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseString)!;
                    string aiText = result.candidates[0].content.parts[0].text.ToString();
                    aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

                    try
                    {
                        var analysisResult = JsonConvert.DeserializeObject<MedicalCertAnalysisResult>(aiText);
                        if (analysisResult != null) return analysisResult;
                    }
                    catch
                    {
                        failResult.Summary = "AI trả về kết quả không đúng định dạng.";
                        return failResult;
                    }
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                failResult.Summary = $"Lỗi khi gọi AI ({(int)response.StatusCode}): {errorMsg}";
                return failResult;

            }
            catch (Exception ex)
            {
                failResult.Summary = $"Lỗi hệ thống: {ex.Message}";
                return failResult;
            }
        }
    }

    public class MedicalCertAnalysisResult
    {
        [JsonProperty("verdict")]
        public string Verdict { get; set; } = "FAIL"; // PASS, FAIL, SUSPICIOUS

        [JsonProperty("score")]
        public int Score { get; set; } = 0;

        [JsonProperty("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonProperty("missing_fields")]
        public List<string> MissingFields { get; set; } = new();

        [JsonProperty("checks")]
        public List<MedicalCertCheck> Checks { get; set; } = new();
    }

    public class MedicalCertCheck
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("label")]
        public string Label { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = "FAIL"; // PASS, FAIL, WARN

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}