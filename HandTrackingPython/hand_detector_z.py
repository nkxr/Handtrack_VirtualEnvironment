import cv2
import mediapipe as mp
from cvzone.HandTrackingModule import HandDetector


class HandDetectorZ(HandDetector):
    """
    Patch สำหรับ cvzone 2.0.0:
    HandTrackingModule.py ตัวเดิม hardcode z=0 ใน lmList -> lmList.append([x, y, 0])
    ฟังก์ชันนี้ copy logic เดิมของ cvzone มาทั้งหมด
    แต่เปลี่ยนจุดเดียวคือดึง lm.z จริงจาก MediaPipe มาใส่แทน 0

    (แยกออกมาเป็นไฟล์กลาง เพื่อให้ Main.py และ main_jigsaw.py เรียกใช้คลาสเดียวกันได้
    ไม่ต้อง copy โค้ดซ้ำ)
    """
    def findHands(self, img, draw=True, flipType=True):
        imgRGB = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=imgRGB)

        result = self.detector.detect_for_video(mp_image, self.timestamp)
        self.timestamp += 1

        allHands = []
        h, w, _ = img.shape

        if result.hand_landmarks:
            for i, hand_landmarks in enumerate(result.hand_landmarks):
                myHand = {}
                lmList = []
                xList = []
                yList = []

                for lm in hand_landmarks:
                    x, y, z = int(lm.x * w), int(lm.y * h), int(lm.z * w)  # <-- แก้จุดนี้
                    lmList.append([x, y, z])
                    xList.append(x)
                    yList.append(y)

                xmin, xmax = min(xList), max(xList)
                ymin, ymax = min(yList), max(yList)
                boxW, boxH = xmax - xmin, ymax - ymin
                bbox = (xmin, ymin, boxW, boxH)
                cx, cy = xmin + boxW // 2, ymin + boxH // 2

                myHand["lmList"] = lmList
                myHand["bbox"] = bbox
                myHand["center"] = (cx, cy)
                myHand["type"] = "Unknown"

                allHands.append(myHand)

                if draw:
                    cv2.rectangle(img, (xmin - 20, ymin - 20), (xmax + 20, ymax + 20), (255, 0, 255), 2)
                    cv2.circle(img, (cx, cy), 5, (0, 255, 0), -1)

        return allHands, img
