import mediapipe as mp
import cv2
import socket
import os
import struct
import time

import gaze

VISUALIZE = False
SOCK_PATH = "/tmp/CoreFxPipe_mySocket"

with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as sock:
    try:
        os.remove(SOCK_PATH)
    except OSError:
        pass
    sock.bind(SOCK_PATH)

    mp_face_mesh = mp.solutions.face_mesh

    cap = cv2.VideoCapture(0)

    with mp_face_mesh.FaceMesh(
            max_num_faces=1,
            refine_landmarks=True,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5) as face_mesh:
        while True:
            try:
                print("Waiting for connection ...")
                sock.listen()
                conn, _ = sock.accept()

                with conn:
                    print("Client connected")

                    while cap.isOpened():
                        success, image = cap.read()
                        if not success:
                            print("Ignoring empty camera frame")
                            continue

                        height, width, _ = image.shape
                        image.flags.writeable = False
                        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                        results = face_mesh.process(image)
                        if VISUALIZE: image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

                        packet = bytearray()
                        packet.extend(bytearray(struct.pack("d", time.time())))
                        packet.extend(bytearray(struct.pack("i", width)))
                        packet.extend(bytearray(struct.pack("i", height)))

                        if results.multi_face_landmarks and len(results.multi_face_landmarks) == 1:
                            for i in range(0, 468):
                                packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].x)))
                                packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].y)))
                                packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].z)))
                                if VISUALIZE:
                                    x = int(results.multi_face_landmarks[0].landmark[i].x * width)
                                    y = int(results.multi_face_landmarks[0].landmark[i].y * height)
                                    cv2.circle(image, (x, y), 2, (100, 100, 0), -1)
                            # gaze_result = gaze.gaze(image, results.multi_face_landmarks[0])
                            # if gaze_result:
                            #     packet.extend(bytearray(struct.pack("f", gaze_result[0][0])))
                            #     packet.extend(bytearray(struct.pack("f", gaze_result[0][1])))
                            #     packet.extend(bytearray(struct.pack("f", gaze_result[1][0])))
                            #     packet.extend(bytearray(struct.pack("f", gaze_result[1][1])))
                            # else:
                                packet.extend(bytearray(struct.pack("f", 0)))
                                packet.extend(bytearray(struct.pack("f", 0)))
                                packet.extend(bytearray(struct.pack("f", 0)))
                                packet.extend(bytearray(struct.pack("f", 0)))

                        conn.sendall(packet)

                        if VISUALIZE: cv2.imshow('Visualize', image)
                print("Camera closed")
                break

            except BrokenPipeError:
                print("Client disconnected")
                pass

    cap.release()
    print("Camera released")

sock.close()
print("Socket closed")
