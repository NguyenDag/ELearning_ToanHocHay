FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 5001

# --timeout 120: a Gemini call (esp. with an image) can exceed gunicorn's 30s default.
CMD ["gunicorn", "-w", "2", "--timeout", "120", "-b", "0.0.0.0:5001", "Main_AI_Service:app"]
