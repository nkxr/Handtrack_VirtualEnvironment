import socket
import struct
import threading


class VideoStreamer:
    """เปิด TCP server ไว้ให้ฝั่ง Unity ต่อเข้ามารับภาพสดจากกล้อง (ใช้กับซีน jigsaw/tah
    ที่ต้องโชว์ภาพสดใน Unity แต่กล้องถูก Python ยึดไว้ทำ hand tracking อยู่แล้ว)

    โปรโตคอล: ส่งความยาวเฟรมเป็น 4 byte big-endian (unsigned int) ตามด้วยข้อมูล JPEG
    ความยาวเท่านั้น ต่อกันไปเรื่อยๆ ไม่มี handshake อื่น

    รองรับ client เดียว ณ ขณะหนึ่ง ถ้า Unity ยังไม่เปิด/ยังไม่ต่อเข้ามา send_frame จะข้ามไปเฉยๆ
    (ไม่เสียเวลา encode/ส่งเปล่าๆ) ถ้า client หลุดกลางคัน จะรอ client ใหม่มาต่อให้อัตโนมัติ
    """

    def __init__(self, host="127.0.0.1", port=5053):
        self.host = host
        self.port = port
        self._server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._server_socket.bind((host, port))
        self._server_socket.listen(1)
        self._server_socket.settimeout(0.5)

        self._client_socket = None
        self._lock = threading.Lock()
        self._running = True

        self._accept_thread = threading.Thread(target=self._accept_loop, daemon=True)
        self._accept_thread.start()

    def _accept_loop(self):
        while self._running:
            try:
                client, _addr = self._server_socket.accept()
                with self._lock:
                    if self._client_socket is not None:
                        try:
                            self._client_socket.close()
                        except OSError:
                            pass
                    self._client_socket = client
                    print("[VideoStreamer] Unity เชื่อมต่อสำเร็จ")
            except socket.timeout:
                continue
            except OSError:
                break

    def send_frame(self, jpg_bytes):
        """ส่งเฟรมภาพ (bytes ของ JPEG ที่ encode แล้ว) ไปยัง client ที่ต่ออยู่ (ถ้ามี)"""
        with self._lock:
            client = self._client_socket

        if client is None:
            return  # ยังไม่มี Unity ต่อเข้ามา ไม่ต้องเสียเวลา

        header = struct.pack(">I", len(jpg_bytes))
        try:
            client.sendall(header + jpg_bytes)
        except (BrokenPipeError, ConnectionResetError, OSError):
            with self._lock:
                if self._client_socket is client:
                    self._client_socket = None
            try:
                client.close()
            except OSError:
                pass

    def close(self):
        self._running = False
        with self._lock:
            if self._client_socket is not None:
                try:
                    self._client_socket.close()
                except OSError:
                    pass
                self._client_socket = None
        try:
            self._server_socket.close()
        except OSError:
            pass
