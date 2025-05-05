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

SOCK_PATH = "/tmp/CoreFxPipe_mySocket"
IRIS = False
GAZE = False
EXPRESSION = False
FACE = False
HANDS = False
HOLISTIC = True

print('Loading iris ...')
iris_detector = None if not IRIS else IrisLandmark()

print('Loading hands ...')
hands = None if not HANDS else mp.solutions.hands.Hands()

print('Loading face mesh ...')
face_mesh = None if not FACE else mp.solutions.face_mesh.FaceMesh(
        max_num_faces=1,
        refine_landmarks=True,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5)

keypoint_classifier = None if not EXPRESSION else expression.KeyPointClassifier('/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier.tflite', 1)

if EXPRESSION:
    # Read labels
    with open('/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier_label.csv',
                encoding='utf-8-sig') as f:
        keypoint_classifier_labels = csv.reader(f)
        keypoint_classifier_labels = [
            row[0] for row in keypoint_classifier_labels
        ]

holistic = mp.solutions.holistic.Holistic(min_detection_confidence=0.5, min_tracking_confidence=0.5)

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

                    if FACE:
                        face_result = face_mesh.process(image)
                        if (face_result.multi_face_landmarks is not None and len(face_result.multi_face_landmarks) == 1):
                            packet = bytearray()
                            packet.extend(bytearray(struct.pack("d", time.time())))
                            packet.extend(bytearray(struct.pack("i", width)))
                            packet.extend(bytearray(struct.pack("i", height)))
                            packet.extend(bytearray(struct.pack("i", 1)))

                            for i in range(0, 468):
                                packet.extend(bytearray(struct.pack("f", face_result.multi_face_landmarks[0].landmark[i].x)))
                                packet.extend(bytearray(struct.pack("f", face_result.multi_face_landmarks[0].landmark[i].y)))
                                packet.extend(bytearray(struct.pack("f", face_result.multi_face_landmarks[0].landmark[i].z)))

                            if GAZE:
                                gaze_result = gaze.gaze(image, face_result.multi_face_landmarks[0])
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
                                face_result = calc_landmarks(image, face_result.multi_face_landmarks[0].landmark)
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
                                brect = expression.calc_bounding_rect(image, face_result.multi_face_landmarks[0])
                                landmark_list = expression.calc_landmark_list(image, face_result.multi_face_landmarks[0])
                                pre_processed_landmark_list = expression.pre_process_landmark(landmark_list)
                                facial_emotion_id = keypoint_classifier(pre_processed_landmark_list)
                                packet.extend(bytearray(struct.pack('i', facial_emotion_id)))
                            else:
                                packet.extend(bytearray(struct.pack('i', 42)))

                            conn.sendall(packet)

                    if HANDS:
                        hand_result = hands.process(image)

                        if (hand_result.multi_handedness and hand_result.multi_hand_landmarks and len(hand_result.multi_handedness) > 0):
                            packet = bytearray()
                            packet.extend(bytearray(struct.pack("d", time.time())))
                            packet.extend(bytearray(struct.pack("i", width)))
                            packet.extend(bytearray(struct.pack("i", height)))
                            packet.extend(bytearray(struct.pack("i", 2)))

                            numHands = len(hand_result.multi_handedness)
                            packet.extend(bytearray(struct.pack("i", numHands)))

                            for i in range(0, numHands):
                                packet.extend(bytearray(struct.pack("i", hand_result.multi_handedness[i].classification[0].index)))
                                packet.extend(bytearray(struct.pack("f", hand_result.multi_handedness[i].classification[0].score)))

                                for j in range(0, 21):
                                    packet.extend(bytearray(struct.pack("f", hand_result.multi_hand_landmarks[i].landmark[j].x)))
                                    packet.extend(bytearray(struct.pack("f", hand_result.multi_hand_landmarks[i].landmark[j].y)))
                                    packet.extend(bytearray(struct.pack("f", hand_result.multi_hand_landmarks[i].landmark[j].z)))

                            conn.sendall(packet)

                    if HOLISTIC:
                        holistic_results = holistic.process(image)

                        if (holistic_results.pose_landmarks):
                            packet = bytearray()
                            packet.extend(bytearray(struct.pack("d", time.time())))
                            packet.extend(bytearray(struct.pack("i", width)))
                            packet.extend(bytearray(struct.pack("i", height)))
                            packet.extend(bytearray(struct.pack("i", 3)))

                            for i in range(0, 33):
                                packet.extend(bytearray(struct.pack("f", holistic_results.pose_landmarks.landmark[i].x)))
                                packet.extend(bytearray(struct.pack("f", holistic_results.pose_landmarks.landmark[i].y)))
                                packet.extend(bytearray(struct.pack("f", holistic_results.pose_landmarks.landmark[i].z)))
                                packet.extend(bytearray(struct.pack("f", holistic_results.pose_landmarks.landmark[i].visibility)))

                            conn.sendall(packet)

                        if (holistic_results.face_landmarks):
                            packet = bytearray()
                            packet.extend(bytearray(struct.pack("d", time.time())))
                            packet.extend(bytearray(struct.pack("i", width)))
                            packet.extend(bytearray(struct.pack("i", height)))
                            packet.extend(bytearray(struct.pack("i", 1)))

                            for i in range(0, 468):
                                packet.extend(bytearray(struct.pack("f", holistic_results.face_landmarks.landmark[i].x)))
                                packet.extend(bytearray(struct.pack("f", holistic_results.face_landmarks.landmark[i].y)))
                                packet.extend(bytearray(struct.pack("f", holistic_results.face_landmarks.landmark[i].z)))

                            packet.extend(bytearray(struct.pack("f", 0)))
                            packet.extend(bytearray(struct.pack("f", 0)))

                            packet.extend(bytearray(struct.pack("f", 0)))
                            packet.extend(bytearray(struct.pack("f", 0)))

                            packet.extend(bytearray(struct.pack('f', 0)))
                            packet.extend(bytearray(struct.pack('f', 0)))
                            packet.extend(bytearray(struct.pack('f', 0)))

                            packet.extend(bytearray(struct.pack('f', 0)))
                            packet.extend(bytearray(struct.pack('f', 0)))
                            packet.extend(bytearray(struct.pack('f', 0)))
                            
                            packet.extend(bytearray(struct.pack('i', 42)))

                            conn.sendall(packet)

                        if (holistic_results.left_hand_landmarks or holistic_results.right_hand_landmarks):

                            packet = bytearray()
                            packet.extend(bytearray(struct.pack("d", time.time())))
                            packet.extend(bytearray(struct.pack("i", width)))
                            packet.extend(bytearray(struct.pack("i", height)))
                            packet.extend(bytearray(struct.pack("i", 2)))

                            numHands = 0
                            if (holistic_results.left_hand_landmarks): numHands += 1
                            if (holistic_results.right_hand_landmarks): numHands += 1

                            packet.extend(bytearray(struct.pack("i", numHands)))

                            if (holistic_results.left_hand_landmarks):
                                packet.extend(bytearray(struct.pack("i", 1)))
                                packet.extend(bytearray(struct.pack("f", 1)))

                                for j in range(0, 21):
                                    packet.extend(bytearray(struct.pack("f", holistic_results.left_hand_landmarks.landmark[j].x)))
                                    packet.extend(bytearray(struct.pack("f", holistic_results.left_hand_landmarks.landmark[j].y)))
                                    packet.extend(bytearray(struct.pack("f", holistic_results.left_hand_landmarks.landmark[j].z)))

                            if (holistic_results.right_hand_landmarks):
                                packet.extend(bytearray(struct.pack("i", 0)))
                                packet.extend(bytearray(struct.pack("f", 1)))

                                for j in range(0, 21):
                                    packet.extend(bytearray(struct.pack("f", holistic_results.right_hand_landmarks.landmark[j].x)))
                                    packet.extend(bytearray(struct.pack("f", holistic_results.right_hand_landmarks.landmark[j].y)))
                                    packet.extend(bytearray(struct.pack("f", holistic_results.right_hand_landmarks.landmark[j].z)))

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
