import math

# index ของ landmark ตามลำดับของ MediaPipe Hands: (MCP, PIP, TIP) ต่อนิ้ว ไม่รวมนิ้วโป้ง
# (นิ้วโป้งงอ/เหยียดในแนวข้าง ตรวจด้วยวิธีเดียวกันไม่แม่น เลยตัดออกตามที่คุยกันไว้)
_FINGERS = {
    "index": (5, 6, 8),
    "middle": (9, 10, 12),
    "ring": (13, 14, 16),
    "pinky": (17, 18, 20),
}


def _dist2d(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def classify_gesture(lm_list):
    """lm_list: list ของ [x, y, z] 21 จุด ตามลำดับ landmark ของ MediaPipe Hands

    คืนค่า:
        "fist"  ถ้าทั้ง 4 นิ้ว (ชี้ กลาง นาง ก้อย) งอครบ
        "open"  ถ้าทั้ง 4 นิ้วเหยียดครบ (แบมือ)
        "other" กรณีอื่นๆ (งอบางนิ้ว)

    วิธีเช็คนิ้วงอ: เทียบระยะ 2D จากข้อมือ (landmark 0) ไปยังปลายนิ้ว (TIP) กับระยะจากข้อมือ
    ไปยังข้อกลางนิ้ว (PIP) — ถ้าปลายนิ้วอยู่ใกล้ข้อมือกว่าข้อกลางนิ้ว แปลว่านิ้วนั้นงอ
    วิธีนี้ทนต่อการหมุน/เอียงมือได้ดีกว่าการเทียบแค่ค่า y ตรงๆ
    """
    if not lm_list or len(lm_list) < 21:
        return "other"

    wrist = lm_list[0]
    curled_count = 0

    for _mcp_idx, pip_idx, tip_idx in _FINGERS.values():
        pip = lm_list[pip_idx]
        tip = lm_list[tip_idx]
        if _dist2d(wrist, tip) < _dist2d(wrist, pip):
            curled_count += 1

    if curled_count == 4:
        return "fist"
    if curled_count == 0:
        return "open"
    return "other"


class GestureStabilizer:
    """กันอาการกระพริบสถานะ (fist/open/other) จากความไม่นิ่งของการตรวจจับแต่ละเฟรม

    ต้องเจอค่าดิบ (raw) เดิมซ้ำกันติดต่อกันครบ `required_frames` เฟรม ถึงจะเปลี่ยนสถานะ
    ที่รายงานออกไปจริง (ค่าที่ยังไม่ครบเงื่อนไข จะยังคงรายงานสถานะเดิมต่อไปก่อน)
    """

    def __init__(self, required_frames=3, initial="none"):
        self.required_frames = required_frames
        self.stable = initial
        self._candidate = initial
        self._count = 0

    def update(self, raw_value):
        if raw_value == self._candidate:
            self._count += 1
        else:
            self._candidate = raw_value
            self._count = 1

        if self._count >= self.required_frames:
            self.stable = raw_value

        return self.stable
