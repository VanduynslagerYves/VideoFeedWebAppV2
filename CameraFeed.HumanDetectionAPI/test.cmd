:: curl.exe -X POST "http://127.0.0.1:8000/detect-human/" -F "file=@test.jpeg"
curl.exe -X POST "http://127.0.0.1:8000/detect-human/" -F "file=@test.jpeg" --output result.jpeg
curl.exe -X POST "http://127.0.0.1:8000/detect-human/" -F "file=@test2.jpeg" --output result2.jpeg
