# 🤖 Gemini AI Educational API Server

Flask server cung cấp API endpoints cho tạo gợi ý (hints) và phản hồi (feedback) từ Gemini AI.

---

## 🚀 **Cài đặt & Chạy**

### 1. **Cài đặt Dependencies**
```bash
pip install -r requirements.txt
```

### 2. **Cấu hình Environment**
```bash
# Copy file .env và cấu hình API keys
cp .env.example .env

# Chỉnh sửa .env với Gemini API keys của bạn
GEMINI_API_KEY_1 = "AIzaSy..."
GEMINI_API_KEY_2 = "AIzaSy..."

# Cấu hình port (mặc định: 5000)
FLASK_PORT = 5000
FLASK_DEBUG = False
```

### 3. **Chạy Server**
```bash
# Development mode
python AI_main.py

# Production mode (với gunicorn)
gunicorn -w 4 -b 0.0.0.0:5000 AI_main:app
```

Server sẽ chạy trên: `http://localhost:5000`

---

## 📊 **API Endpoints**

### ✅ **Health Check**
```bash
GET /api/health
```

**Response:**
```json
{
    "status": "healthy",
    "message": "Gemini AI API server is running"
}
```

---

### 📋 **Status**
```bash
GET /api/status
```

**Response:**
```json
{
    "service": "Gemini AI Educational API",
    "version": "1.0",
    "endpoints": {
        "hint": "/api/hint (POST)",
        "hint_batch": "/api/hint/batch (POST)",
        "feedback": "/api/feedback (POST)",
        "feedback_batch": "/api/feedback/batch (POST)"
    },
    "status": "operational"
}
```

---

## 💡 **HINT Endpoints**

### 1️⃣ **Tạo Gợi ý - Single**
```bash
POST /api/hint
```

**Request Body:**
```json
{
    "question_text": "Tính đạo hàm của f(x) = 2x³ + 3x²",
    "question_type": "FillBlank",
    "difficulty_level": "Medium",
    "student_answer": "f'(x) = 6x² + 6x",
    "hint_level": 1,
    "question_id": 5,
    "question_image_url": "https://example.com/formula.png",
    "options": [
        {
            "OptionId": 1,
            "OptionText": "Đáp án A"
        }
    ]
}
```

**Response:**
```json
{
    "HintText": "Gợi ý: Bạn cần xem lại quy tắc đạo hàm của lũy thừa...",
    "HintLevel": 1,
    "QuestionId": 5,
    "Status": "success"
}
```

---

### 2️⃣ **Tạo Gợi ý - Batch**
```bash
POST /api/hint/batch
```

**Request Body:**
```json
{
    "hints": [
        {
            "question_text": "Tính tích phân...",
            "question_type": "FillBlank",
            "difficulty_level": "Hard",
            "student_answer": "∫x dx = ...",
            "hint_level": 1,
            "question_id": 1
        },
        {
            "question_text": "Giải phương trình...",
            "question_type": "FillBlank",
            "difficulty_level": "Medium",
            "student_answer": "x = 5",
            "hint_level": 2,
            "question_id": 2
        }
    ]
}
```

**Response:**
```json
{
    "results": [
        {
            "index": 0,
            "HintText": "...",
            "HintLevel": 1,
            "Status": "success"
        },
        {
            "index": 1,
            "HintText": "...",
            "HintLevel": 2,
            "Status": "success"
        }
    ],
    "total": 2,
    "successful": 2,
    "failed": 0,
    "status": "success"
}
```

---

## 📝 **FEEDBACK Endpoints**

### 1️⃣ **Tạo Phản hồi - Single**
```bash
POST /api/feedback
```

**Request Body:**
```json
{
    "question_text": "Tính đạo hàm của f(x) = 2x³ + 3x²",
    "question_type": "FillBlank",
    "student_answer": "f'(x) = 6x² + 6x",
    "correct_answer": "f'(x) = 6x² + 6x",
    "is_correct": true,
    "explanation": "Sử dụng quy tắc lũy thừa",
    "attempt_id": 10,
    "question_image_url": "https://example.com/formula.png",
    "options": [
        {
            "OptionId": 1,
            "OptionText": "Đáp án A",
            "IsCorrect": false
        },
        {
            "OptionId": 2,
            "OptionText": "Đáp án B",
            "IsCorrect": true
        }
    ]
}
```

**Response:**
```json
{
    "FullSolution": "Lời giải hoàn chỉnh:\n1. Áp dụng quy tắc lũy thừa...",
    "MistakeAnalysis": "Phân tích lỗi:\nBạn đã làm đúng...",
    "ImprovementAdvice": "Lời khuyên cải thiện:\nHãy ôn lại...",
    "AttemptId": 10,
    "Status": "success"
}
```

---

### 2️⃣ **Tạo Phản hồi - Batch**
```bash
POST /api/feedback/batch
```

**Request Body:**
```json
{
    "feedbacks": [
        {
            "question_text": "...",
            "question_type": "FillBlank",
            "student_answer": "...",
            "correct_answer": "...",
            "is_correct": true,
            "attempt_id": 1
        },
        {
            "question_text": "...",
            "question_type": "MultipleChoice",
            "student_answer": "...",
            "correct_answer": "...",
            "is_correct": false,
            "attempt_id": 2
        }
    ]
}
```

**Response:**
```json
{
    "results": [
        {
            "index": 0,
            "FullSolution": "...",
            "Status": "success"
        },
        {
            "index": 1,
            "FullSolution": "...",
            "Status": "success"
        }
    ],
    "total": 2,
    "successful": 2,
    "failed": 0,
    "status": "success"
}
```

---

## 🔌 **Sử dụng từ C# Backend**

```csharp
// HttpClient để gọi Flask API
using (var httpClient = new HttpClient())
{
    httpClient.BaseAddress = new Uri("http://localhost:5000");
    
    // Tạo hint request
    var hintRequest = new
    {
        question_text = question.QuestionText,
        question_type = question.QuestionType.ToString(),
        difficulty_level = question.DifficultyLevel.ToString(),
        student_answer = studentAnswer.AnswerText,
        hint_level = 1,
        question_id = question.QuestionId,
        question_image_url = question.QuestionImageUrl,
        options = question.QuestionOptions?.Select(o => new
        {
            o.OptionId,
            o.OptionText,
            o.ImageUrl
        }).ToList()
    };
    
    // Gửi request
    var response = await httpClient.PostAsync(
        "/api/hint",
        new StringContent(
            JsonSerializer.Serialize(hintRequest),
            Encoding.UTF8,
            "application/json"
        )
    );
    
    // Parse response
    var hintResult = await response.Content.ReadAsAsync<HintResponse>();
    
    // Lưu vào database
    var aiHint = new AIHint
    {
        AttemptId = hintResult.AttemptId,
        QuestionId = hintResult.QuestionId,
        HintText = hintResult.HintText,
        HintLevel = hintResult.HintLevel
    };
    
    await hintRepository.CreateAsync(aiHint);
}
```

---

## ⚙️ **Configuration**

### Environment Variables
```bash
# Flask server
FLASK_PORT=5000              # Port để chạy server
FLASK_DEBUG=False            # Debug mode (True/False)

# Gemini API Keys (support multiple keys)
GEMINI_API_KEY_1="AIzaSy..."
GEMINI_API_KEY_2="AIzaSy..."
GEMINI_API_KEY_3="AIzaSy..."
```

---

## 🧪 **Testing**

### Với cURL:
```bash
# Health check
curl http://localhost:5000/api/health

# Tạo hint
curl -X POST http://localhost:5000/api/hint \
  -H "Content-Type: application/json" \
  -d '{
    "question_text": "Tính 2+2",
    "question_type": "FillBlank",
    "difficulty_level": "Easy",
    "student_answer": "4",
    "hint_level": 1
  }'

# Tạo feedback
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "question_text": "Tính 2+2",
    "question_type": "FillBlank",
    "student_answer": "4",
    "correct_answer": "4",
    "is_correct": true
  }'
```

### Với Python:
```python
import requests

# Tạo hint
response = requests.post(
    'http://localhost:5000/api/hint',
    json={
        'question_text': 'Tính 2+2',
        'question_type': 'FillBlank',
        'difficulty_level': 'Easy',
        'student_answer': '4',
        'hint_level': 1
    }
)

print(response.json())
```

---

## 📊 **Response Codes**

| Code | Meaning |
|------|---------|
| 200 | ✅ Success |
| 400 | ⚠️ Bad Request (validation error) |
| 404 | ❌ Endpoint not found |
| 405 | ❌ Method not allowed |
| 500 | 🔴 Server error |

---

## 🔄 **Xử lý Lỗi & Retry Logic**

Server tự động xoay vòng API keys nếu:
- Rate limit exceeded
- API key hết quota
- Network error

**Logs:**
```
[WARNING] Rotating to API key #2 of 3
[INFO] Loaded 3 Gemini API key(s)
[ERROR] API call attempt 1 failed: Rate limit
```

---

## 📚 **Cấu trúc Project**

```
AI/
├── AI_main.py                    # Flask server
├── AI_model/
│   ├── Gemini_api.py            # Gemini service
│   ├── Openai_api.py            # OpenAI service
│   └── __pycache__/
├── Prompts.py                   # AI prompts
├── .env                         # Environment config
├── requirements.txt             # Dependencies
└── README.md                    # Hướng dẫn
```

---

## 🎯 **Best Practices**

1. **Batch Processing:** Dùng `/batch` endpoints cho multiple requests
2. **Caching:** Cache responses để tránh duplicate calls
3. **Timeout:** Set timeout cho requests (10-30s)
4. **Error Handling:** Luôn check `status` field trong response
5. **Rate Limiting:** Implement rate limiting ở C# backend

---

## 🆘 **Troubleshooting**

**Q: ModuleNotFoundError: No module named 'Prompts'**
```bash
# Solution: Cài đặt dependencies
pip install -r requirements.txt
```

**Q: Connection refused to localhost:5000**
```bash
# Solution: Đảm bảo Flask server đang chạy
python AI_main.py
```

**Q: API key error**
```bash
# Solution: Kiểm tra .env file
# Đảm bảo GEMINI_API_KEY_1 có giá trị hợp lệ
```

---

Enjoy! 🚀
