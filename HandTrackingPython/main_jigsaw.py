import cv2

from hand_detector_z import HandDetectorZ
from udp_sender import UDPSender
from video_streamer import VideoStreamer

# ปรับได้ตามสเปคเครื่อง ถ้าลองแล้วภาพยังกระตุก ลองลดขนาด/คุณภาพ/หรือข้ามเฟรม (เช่นส่งทุกเฟรมที่ 2)
STREAM_WIDTH = 480
STREAM_HEIGHT = 360
JPEG_QUALITY = 70  # 0-100 ยิ่งสูงยิ่งภาพคมแต่ไฟล์ใหญ่/ช้าลง

cap = cv2.VideoCapture(0)
cap.set(3, 1280)
cap.set(4, 720)
success, img = cap.read()
h, w, _ = img.shape

detector = HandDetectorZ(detectionCon=0.8, maxHands=1)
landmark_sender = UDPSender(ip="127.0.0.1", port=5052)
video_streamer = VideoStreamer(host="127.0.0.1", port=5053)

encode_params = [int(cv2.IMWRITE_JPEG_QUALITY), JPEG_QUALITY]

print("[main_jigsaw] เริ่มทำงาน: ส่ง landmark ไปพอร์ต 5052, สตรีมภาพไปพอร์ต 5053")
print("[main_jigsaw] รอ Unity (WebcamStreamReceiver) มาเชื่อมต่อ...")

try:
    while True:
        success, img = cap.read()
        if not success:
            continue

        hands, img = detector.findHands(img)

        if hands:
            hand = hands[0]
            lmList = hand["lmList"]
            # กลับแกน y: ภาพนับจากบนลงล่าง แต่ Unity นับแกน y ขึ้นบน (เหมือน Main.py)
            flipped = [[lm[0], h - lm[1], lm[2]] for lm in lmList]
            landmark_sender.send_landmarks(flipped)
        else:
            landmark_sender.send_no_hand()

        # ส่งภาพสด (ไม่มิเรอร์ ให้ตรงกับพฤติกรรมเดิมของ WebCamManager ที่โชว์ภาพดิบจากกล้อง)
        small = cv2.resize(img, (STREAM_WIDTH, STREAM_HEIGHT))
        ok, jpg = cv2.imencode(".jpg", small, encode_params)
        if ok:
            video_streamer.send_frame(jpg.tobytes())

        cv2.imshow("Image", img)
        cv2.waitKey(1)
finally:
    video_streamer.close()
    cap.release()
