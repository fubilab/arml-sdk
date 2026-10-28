import socket
import json
import cv2
import _thread as thread
import time
import threading
import numpy as np

class UnityBridge:
    def __init__(self, address):
        self.address = address
        self.socket = None
        self.running = False
        self.image = None
        self.names = None
        self.objects = None
        self.configs = None
        self.data = None
        self.count = 0
        self._frame_lock = threading.Lock()
        self._frame_ready = threading.Event()

    def start(self):
        """ Start the networking thread. """
        self.running = True
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self.socket.bind(self.address) 
        print("Server Started!")
        self.socket.listen(10)
        thread.start_new_thread(self._run, ())

    def send(self, image, names, objects, configs):
        with self._frame_lock:
            self.image = image
            self.names = names
            self.objects = objects
            self.configs = configs
            frame_ready = (
                image is not None
                and names is not None
                and objects is not None
                and configs is not None
            )

        if frame_ready:
            self._frame_ready.set()

    def close(self):
        """ Close the socket connection. """
        self.running = False
        self._frame_ready.set()
        if self.socket:
            self.socket.close()

    def _serialize_objects(self, key_names, objects, configs):
        # Ensure that key_names, objects, and configs have the same length
        if not (len(key_names) == len(objects) == len(configs)):
            raise ValueError("Length of key_names, objects, and configs must be the same.")

        if len(key_names) != len(set(key_names)):
            raise ValueError("Key names must be unique.")

        # Initialize a dictionary to store the serialized data
        serialized_data = {}#{key: [] for key in key_names}

        for obj, config, key_name in zip(objects, configs, key_names):
            serialized_obj = {}
            for field in config:
                if hasattr(obj, field):
                    value = getattr(obj, field)
                    # Check if the value is a numpy.ndarray and convert it to a list
                    if isinstance(value, np.ndarray):
                        value = value.tolist()
                    # Add other type checks and conversions as necessary
                    serialized_obj[field] = value
            serialized_data[key_name] = serialized_obj

        return serialized_data


    def client(self,conn, addr):
        while self.running:

            # Wait for DATA command

            try:
                data = bytearray()
                while len(data) < 4:
                    packet = conn.recv(4)
                    if not packet:
                        break
                    if len(packet) == 0:
                        break

                    data.extend(packet)

            except OSError as exception:
                print(f"[UnityBridge] Error receiving request from {addr}: {exception}", flush=True)
                self._error(conn, addr)
                break

            if not data:
                break

            command = data.decode('utf-8', errors='replace')
            if command != "DATA":
                print(f"[UnityBridge] Ignoring unexpected request {bytes(data)!r} from {addr}.", flush=True)
                continue

            if self.count < 5 or self.count % 100 == 0:
                with self._frame_lock:
                    image_available = self.image is not None
                print(
                    f"[UnityBridge] Received request #{self.count + 1}: "
                    f"{bytes(data)!r}; image available={image_available}",
                    flush=True,
                )

            while self.running and not self._frame_ready.wait(timeout=0.1):
                pass
            if not self.running:
                break

            with self._frame_lock:
                image = self.image
                names = self.names
                objects = self.objects
                configs = self.configs

            try:
                self.data = self._serialize_objects(names, objects, configs)
            except (TypeError, ValueError) as exception:
                print(f"[UnityBridge] Could not serialize frame data: {exception}", flush=True)
                break

            if not self._send_data(conn, image, self.data):
                break

            time.sleep(0.05) 

        conn.close()
        print('Disconnected ', addr)

    def _error(self,conn, addr):
        try:
            print("Error.")
        except:
            print(addr, "Disconnected.")

    def _run(self):
        """ The main loop for the networking thread. """
        while True:
            print("Listening...")
            try:
                conn, addr = self.socket.accept()
                print('Connected with ', addr)
                thread.start_new_thread(self.client, (conn, addr))
            except:
                break


    def _send_data(self, conn, image, data):
        """ Send the image and serialized data over the socket. """
        if image is None:
            print("[UnityBridge] Cannot answer DATA request: no image is available yet.", flush=True)
            return False

        try:
            ret, encoded_image = cv2.imencode('.jpg', image)
        except cv2.error as e:
            print(f"[UnityBridge] Image encoding failed: {e}", flush=True)
            return False

        if not ret:
            print("[UnityBridge] Could not encode image.", flush=True)
            return False

        image_data = encoded_image.tobytes()
        json_data = json.dumps(data).encode('utf-8')

        try:
            frame_number = self.count + 1
            log_frame = frame_number <= 5 or frame_number % 100 == 0
            if log_frame:
                print(
                    f"[UnityBridge] Sending frame #{frame_number}: "
                    f"JSON={len(json_data)} bytes, image={len(image_data)} bytes.",
                    flush=True,
                )
            conn.sendall(json_data+b'<<END_OF_JSON>>')
            conn.sendall(image_data+b'<<END>>')
            self.count = self.count + 1
            if log_frame:
                print(f"[UnityBridge] Frame #{frame_number} sent.", flush=True)
        except socket.error as e:
            print(f"[UnityBridge] Error sending frame: {e}", flush=True)
            return False

        return True


class TestObject:
    def __init__(self, result):
        self.result = result
        self.field1 = None
        self.arr1 = []
