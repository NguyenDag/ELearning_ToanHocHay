
import requests
import json

url = "http://localhost:5001/api/chatbot/message"
data = {
    "user_id": "user_123",
    "text": "Tôi là phụ huynh"
}

try:
    resp = requests.post(url, json=data)
    if resp.status_code == 200:
        print(json.dumps(resp.json(), ensure_ascii=False, indent=2))
    else:
        print(f"Error {resp.status_code}: {resp.text}")
except Exception as e:
    print(f"Error: {type(e).__name__}: {str(e)}")
