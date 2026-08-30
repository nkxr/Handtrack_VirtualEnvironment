import json
import socket


class UDPSender:
    """ส่งข้อมูลจุด landmark ของมือ พร้อมสถานะท่ามือ ไปยัง Unity ผ่าน UDP

    รูปแบบข้อความที่ส่ง (JSON):
        {"hand_detected": true/false, "landmarks": [x0, y0, z0, ...], "gesture": "fist"|"open"|"other"|"none"}

    ฝั่ง Unity มี HandTrackingHub.cs รับข้อมูลรูปแบบนี้อยู่แล้ว (ฟัง UDP พอร์ตเดียวกัน
    แล้ว parse ด้วย JsonUtility) เป็นจุดกลางให้ทุกซีนดึงข้อมูลไปใช้
    """

    def __init__(self, ip="127.0.0.1", port=5052):
        self.address = (ip, port)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send_landmarks(self, lm_list, gesture="other"):
        """lm_list: list ของ [x, y, z] ต่อจุด (จาก HandDetector) ปกติยาว 21 จุด
        gesture: สถานะท่ามือที่จำแนกไว้แล้ว ("fist" / "open" / "other")"""
        flat = []
        for lm in lm_list:
            flat.extend([int(lm[0]), int(lm[1]), int(lm[2])])

        payload = json.dumps({"hand_detected": True, "landmarks": flat, "gesture": gesture})
        self.sock.sendto(payload.encode("utf-8"), self.address)

    def send_no_hand(self, gesture="none"):
        """แจ้ง Unity ว่าตอนนี้ไม่เจอมือในเฟรม (ให้ฝั่ง Unity ซ่อน marker / โชว์สถานะ "ไม่พบมือ")"""
        payload = json.dumps({"hand_detected": False, "landmarks": [], "gesture": gesture})
        self.sock.sendto(payload.encode("utf-8"), self.address)

    def close(self):
        self.sock.close()
