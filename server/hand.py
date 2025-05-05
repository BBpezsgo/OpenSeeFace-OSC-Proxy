import cv2
import mediapipe as mp
import socket
import os
import struct
import time

SOCK_PATH = "/tmp/CoreFxPipe_mySocket"

print('Loading hands ...')
hands = mp.solutions.hands.Hands()

print('Capturing camera 0 ...')
cap = cv2.VideoCapture(0)

print('Creating socket ...')
with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as sock:
    try:
        os.remove(SOCK_PATH)
    except OSError:
        pass
    sock.bind(SOCK_PATH)

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

                    image = cv2.flip(image, 1)
                    image.flags.writeable = False
                    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

                    results = hands.process(image)

                    if not results.multi_handedness: continue
                    if not results.multi_hand_landmarks: continue

                    numHands = len(results.multi_handedness)
                    if numHands < 1: continue

                    packet = bytearray()
                    packet.extend(bytearray(struct.pack("d", time.time())))
                    packet.extend(bytearray(struct.pack("i", width)))
                    packet.extend(bytearray(struct.pack("i", height)))
                    packet.extend(bytearray(struct.pack("i", numHands)))

                    for i in range(0, numHands):
                        packet.extend(bytearray(struct.pack("i", results.multi_handedness[i].classification[0].index)))
                        packet.extend(bytearray(struct.pack("f", results.multi_handedness[i].classification[0].score)))

                        for j in range(0, 21):
                            packet.extend(bytearray(struct.pack("f", results.multi_hand_landmarks[i].landmark[j].x)))
                            packet.extend(bytearray(struct.pack("f", results.multi_hand_landmarks[i].landmark[j].y)))
                            packet.extend(bytearray(struct.pack("f", results.multi_hand_landmarks[i].landmark[j].z)))

                    conn.sendall(packet)
            print("Camera closed")
            break

        except BrokenPipeError:
            print("Client disconnected")
            pass

cap.release()
print("Camera released")

sock.close()
print("Socket closed")
