import cv2

from hand_detector_z import HandDetectorZ
from udp_sender import UDPSender

cap = cv2.VideoCapture(0)
cap.set(3, 1280)
cap.set(4, 720)
success, img = cap.read()
h, w, _ = img.shape
detector = HandDetectorZ(detectionCon=0.8, maxHands=1)

sender = UDPSender(ip="127.0.0.1", port=5052)

while True:
    success, img = cap.read()
    hands, img = detector.findHands(img)

    if hands:
        hand = hands[0]
        lmList = hand["lmList"]
        # กลับแกน y: ภาพนับจากบนลงล่าง แต่ Unity นับแกน y ขึ้นบน
        flipped = [[lm[0], h - lm[1], lm[2]] for lm in lmList]
        sender.send_landmarks(flipped)
        print(flipped)
    else:
        sender.send_no_hand()

    cv2.imshow("Image", img)
    cv2.waitKey(1)
