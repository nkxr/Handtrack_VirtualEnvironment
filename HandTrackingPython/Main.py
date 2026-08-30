import cv2

from hand_detector_z import HandDetectorZ
from udp_sender import UDPSender
from video_streamer import VideoStreamer
from gesture_classifier import classify_gesture, GestureStabilizer

# สคริปต์นี้ใช้ได้กับทุกซีนพร้อมกัน (SampleScene / Flappy Bird / jigsaw ฯลฯ)
# ไม่ต้องสลับสคริปต์ตามซีนที่เล่นใน Unity อีกต่อไป — เปิดตัวนี้ทิ้งไว้ตัวเดียวพอ
# แม้ตอน Unity ถูก build เป็นโปรแกรมจริงแล้วสลับซีนไปมา ก็ยังใช้ตัวนี้ตัวเดียวได้เหมือนเดิม

# ปรับได้ตามสเปคเครื่อง ถ้าลองแล้วภาพสตรีมยังกระตุก ลองลดขนาด/คุณภาพ/หรือข้ามเฟรม
STREAM_WIDTH = 480
STREAM_HEIGHT = 360
JPEG_QUALITY = 70  # 0-100 ยิ่งสูงยิ่งภาพคมแต่ไฟล์ใหญ่/ช้าลง

# ต้องเจอค่าท่ามือดิบเดิมซ้ำกันกี่เฟรม ถึงจะนับว่าเปลี่ยนสถานะจริง (กันกระพริบ)
GESTURE_STABLE_FRAMES = 3

cap = cv2.VideoCapture(0)
cap.set(3, 1280)
cap.set(4, 720)
success, img = cap.read()
h, w, _ = img.shape

detector = HandDetectorZ(detectionCon=0.8, maxHands=1)
landmark_sender = UDPSender(ip="127.0.0.1", port=5052)
video_streamer = VideoStreamer(host="127.0.0.1", port=5053)
gesture_stabilizer = GestureStabilizer(required_frames=GESTURE_STABLE_FRAMES, initial="none")

encode_params = [int(cv2.IMWRITE_JPEG_QUALITY), JPEG_QUALITY]

print("[Main] เริ่มทำงาน: ส่ง landmark+gesture ไปพอร์ต 5052 (UDP), สตรีมภาพไปพอร์ต 5053 (TCP)")
print("[Main] ใช้สคริปต์นี้ตัวเดียวได้ทุกซีน ไม่ต้องสลับสคริปต์ตามซีนที่เล่นใน Unity")

try:
    while True:
        success, img = cap.read()
        if not success:
            continue

        hands, img = detector.findHands(img)

        if hands:
            hand = hands[0]
            lmList = hand["lmList"]
            # กลับแกน y: ภาพนับจากบนลงล่าง แต่ Unity นับแกน y ขึ้นบน
            flipped = [[lm[0], h - lm[1], lm[2]] for lm in lmList]
            raw_gesture = classify_gesture(flipped)
            gesture = gesture_stabilizer.update(raw_gesture)
            landmark_sender.send_landmarks(flipped, gesture)
        else:
            gesture = gesture_stabilizer.update("none")
            landmark_sender.send_no_hand(gesture)

        # ส่งภาพสดไปให้ Unity แสดงผล (ย่อขนาดลงก่อนเพื่อลดภาระ) — ไม่มิเรอร์ ส่งภาพดิบตามที่กล้องเห็น
        small = cv2.resize(img, (STREAM_WIDTH, STREAM_HEIGHT))
        ok, jpg = cv2.imencode(".jpg", small, encode_params)
        if ok:
            video_streamer.send_frame(jpg.tobytes())

        cv2.imshow("Image", img)
        cv2.waitKey(1)
finally:
    video_streamer.close()
    cap.release()
