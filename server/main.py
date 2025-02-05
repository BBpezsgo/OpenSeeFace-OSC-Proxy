import mediapipe as mp
import cv2
import socket
import os
import struct
import time
import csv

import gaze
import expression
from face_mesh.face_mesh import calc_landmarks, calc_around_eye_bbox
from iris_landmark.iris_landmark import IrisLandmark
from iris import detect_iris, calc_min_enc_losingCircle

VISUALIZE = False
SOCK_PATH = "/tmp/CoreFxPipe_mySocket"
IRIS = False
GAZE = False
EXPRESSION = False

print('Loading iris ...')
iris_detector = None if IRIS else IrisLandmark()

print('Loading face mesh ...')
face_mesh = mp.solutions.face_mesh.FaceMesh(
        max_num_faces=1,
        refine_landmarks=True,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5)

keypoint_classifier = expression.KeyPointClassifier('/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier.tflite', 1)

# Read labels
with open('/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier_label.csv',
            encoding='utf-8-sig') as f:
    keypoint_classifier_labels = csv.reader(f)
    keypoint_classifier_labels = [
        row[0] for row in keypoint_classifier_labels
    ]

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
                    results = face_mesh.process(image)
                    if VISUALIZE: image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

                    if results.multi_face_landmarks is None or len(results.multi_face_landmarks) != 1: continue

                    packet = bytearray()
                    packet.extend(bytearray(struct.pack("d", time.time())))
                    packet.extend(bytearray(struct.pack("i", width)))
                    packet.extend(bytearray(struct.pack("i", height)))

                    for i in range(0, 468):
                        packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].x)))
                        packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].y)))
                        packet.extend(bytearray(struct.pack("f", results.multi_face_landmarks[0].landmark[i].z)))
                        if VISUALIZE:
                            x = int(results.multi_face_landmarks[0].landmark[i].x * width)
                            y = int(results.multi_face_landmarks[0].landmark[i].y * height)
                            cv2.circle(image, (x, y), 2, (100, 100, 0), -1)

                    if GAZE:
                        gaze_result = gaze.gaze(image, results.multi_face_landmarks[0])
                        if gaze_result:
                            packet.extend(bytearray(struct.pack("f", gaze_result[0][0])))
                            packet.extend(bytearray(struct.pack("f", gaze_result[0][1])))
                            packet.extend(bytearray(struct.pack("f", gaze_result[1][0])))
                            packet.extend(bytearray(struct.pack("f", gaze_result[1][1])))
                        else:
                            packet.extend(bytearray(struct.pack("f", 0)))
                            packet.extend(bytearray(struct.pack("f", 0)))
                            packet.extend(bytearray(struct.pack("f", 0)))
                            packet.extend(bytearray(struct.pack("f", 0)))
                    else:
                        packet.extend(bytearray(struct.pack("f", 0)))
                        packet.extend(bytearray(struct.pack("f", 0)))

                        packet.extend(bytearray(struct.pack("f", 0)))
                        packet.extend(bytearray(struct.pack("f", 0)))

                    if IRIS:
                        face_result = calc_landmarks(image, results.multi_face_landmarks[0].landmark)
                        # Calculate bounding box around eyes
                        left_eye, right_eye = calc_around_eye_bbox(face_result)

                        # Iris detection
                        left_iris, right_iris = detect_iris(image, iris_detector, left_eye, right_eye)

                        # Calculate the circumcircle of the iris
                        left_center, left_radius = calc_min_enc_losingCircle(left_iris)
                        right_center, right_radius = calc_min_enc_losingCircle(right_iris)

                        packet.extend(bytearray(struct.pack('f', float(left_center[0]) / float(width))))
                        packet.extend(bytearray(struct.pack('f', float(left_center[1]) / float(height))))
                        packet.extend(bytearray(struct.pack('f', float(left_radius))))

                        packet.extend(bytearray(struct.pack('f', float(right_center[0]) / float(width))))
                        packet.extend(bytearray(struct.pack('f', float(right_center[1]) / float(height))))
                        packet.extend(bytearray(struct.pack('f', float(right_radius))))
                    else:
                        packet.extend(bytearray(struct.pack('f', 0)))
                        packet.extend(bytearray(struct.pack('f', 0)))
                        packet.extend(bytearray(struct.pack('f', 0)))

                        packet.extend(bytearray(struct.pack('f', 0)))
                        packet.extend(bytearray(struct.pack('f', 0)))
                        packet.extend(bytearray(struct.pack('f', 0)))
                        
                    if EXPRESSION:
                        brect = expression.calc_bounding_rect(image, results.multi_face_landmarks[0])
                        landmark_list = expression.calc_landmark_list(image, results.multi_face_landmarks[0])
                        pre_processed_landmark_list = expression.pre_process_landmark(landmark_list)
                        facial_emotion_id = keypoint_classifier(pre_processed_landmark_list)
                        packet.extend(bytearray(struct.pack('i', facial_emotion_id)))
                    else:
                        packet.extend(bytearray(struct.pack('i', 42)))

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
