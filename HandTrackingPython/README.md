# Hand Tracking (ฝั่ง Python)

โฟลเดอร์นี้คือ**สำเนา**ของโค้ดฝั่ง Python ที่ใช้ตรวจจับมือ (hand tracking) แล้วส่งข้อมูลไปให้ฝั่ง Unity
เก็บไว้ในนี้ (อยู่ใน git repo เดียวกับ Unity project) เพื่อให้ใครก็ตามที่ clone repo นี้ไปได้โค้ดฝั่ง Python
ติดไปด้วยเลย ไม่ต้องไปขอไฟล์แยกกันอีกที

> ⚠️ ที่ทำงานจริงประจำวันของทีมอยู่คนละที่ (นอก repo นี้) โฟลเดอร์นี้เป็นสำเนาที่จะอัปเดตให้ตรงกัน
> ทุกครั้งที่โค้ดฝั่ง Python มีการแก้ไข ถ้าเห็นว่าไฟล์ในนี้เก่ากว่าที่ทีมใช้งานจริง ให้ทักไว้เพื่ออัปเดต

## ติดตั้ง

```bash
python -m venv venv
# Windows
venv\Scripts\activate
# macOS/Linux
source venv/bin/activate

pip install -r requirements.txt
```

ต้องมีกล้องเว็บแคมต่ออยู่ และไฟล์โมเดล `hand_landmarker.task` (แนบมาในโฟลเดอร์นี้แล้ว) ใช้โดย MediaPipe/cvzone
สำหรับตรวจจับ landmark ของมือ

## ไฟล์ในโฟลเดอร์นี้

- **`hand_detector_z.py`** — ตัวตรวจจับมือ (patch จาก `cvzone.HandTrackingModule.HandDetector` ให้ดึงค่า z
  จริงจาก MediaPipe แทนที่จะ hardcode เป็น 0) ทั้ง `Main.py` และ `main_jigsaw.py` เรียกใช้คลาสนี้ร่วมกัน
- **`udp_sender.py`** — ส่งข้อมูลจุด landmark ของมือ (21 จุด) เป็น JSON ผ่าน UDP ไปยัง Unity
- **`video_streamer.py`** — เปิด TCP server ไว้สตรีมภาพสดจากกล้องไปให้ Unity แสดงผล (ใช้เฉพาะซีนที่ต้องโชว์
  ภาพกล้องใน Unity ด้วย เช่นซีน jigsaw puzzle เพราะกล้องตัวเดียวกันเปิดพร้อมกันสองโปรเซสไม่ได้)
- **`Main.py`** — สคริปต์หลักสำหรับทดสอบ/เล่นซีนที่ใช้แค่ตำแหน่งมือควบคุม (เช่น SampleScene, Flappy Bird)
  ส่งเฉพาะ landmark ผ่าน UDP พอร์ต **5052** ไม่ได้สตรีมภาพ
- **`main_jigsaw.py`** — ใช้กับซีน jigsaw puzzle (tah) โดยเฉพาะ ทำทั้งสองอย่างพร้อมกัน: ส่ง landmark ผ่าน UDP
  พอร์ต 5052 **และ** สตรีมภาพสด (JPEG ย่อขนาด) ผ่าน TCP พอร์ต **5053** ให้ `WebcamStreamReceiver.cs`
  ฝั่ง Unity รับไปแสดงผล
- **`hand_landmarker.task`** — ไฟล์โมเดลของ MediaPipe ที่ `HandDetectorZ` ต้องใช้
- **`requirements.txt`** — รายชื่อ Python package ที่ต้องติดตั้ง

## วิธีรัน

เลือกสคริปต์ตามซีนที่จะเล่น (รันได้ทีละสคริปต์ เพราะแย่งกล้องตัวเดียวกันไม่ได้):

```bash
# เล่น/ทดสอบ SampleScene หรือ Flappy Bird (ใช้แค่ตำแหน่งมือ)
python Main.py

# เล่นซีน jigsaw puzzle (tah) — ต้องมีทั้ง landmark และภาพสด
python main_jigsaw.py
```

รันสคริปต์ Python ทิ้งไว้ก่อน (หรือระหว่างก็ได้) แล้วค่อยกด Play ในซีน Unity ที่เกี่ยวข้อง ฝั่ง Unity จะพยายาม
เชื่อมต่อ/reconnect ให้เองอัตโนมัติถ้ายังไม่เจอ ไม่ต้องเรียงลำดับการเปิดให้ตรงเป๊ะ

## พอร์ตที่ใช้

| พอร์ต | โปรโตคอล | ใช้ทำอะไร | ใช้ในสคริปต์ |
|---|---|---|---|
| 5052 | UDP | ส่งจุด landmark ของมือ (21 จุด) | `Main.py`, `main_jigsaw.py` |
| 5053 | TCP | สตรีมภาพสดจากกล้อง | `main_jigsaw.py` เท่านั้น |
