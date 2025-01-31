#!/usr/bin/env python
# -*- coding: utf-8 -*-
import cv2 as cv
import numpy as np
import mediapipe as mp

def get_eye_landmarks(landmarks: list[tuple[int, int, float]]):
    # 目の輪郭の座標列を取得

    left_eye_landmarks: list[tuple[int, int]] = []
    right_eye_landmarks: list[tuple[int, int]] = []

    if len(landmarks) > 0:
        # 参考：https://github.com/tensorflow/tfjs-models/blob/master/facemesh/mesh_map.jpg
        # 左目
        left_eye_landmarks.append((landmarks[133][0], landmarks[133][1]))
        left_eye_landmarks.append((landmarks[173][0], landmarks[173][1]))
        left_eye_landmarks.append((landmarks[157][0], landmarks[157][1]))
        left_eye_landmarks.append((landmarks[158][0], landmarks[158][1]))
        left_eye_landmarks.append((landmarks[159][0], landmarks[159][1]))
        left_eye_landmarks.append((landmarks[160][0], landmarks[160][1]))
        left_eye_landmarks.append((landmarks[161][0], landmarks[161][1]))
        left_eye_landmarks.append((landmarks[246][0], landmarks[246][1]))
        left_eye_landmarks.append((landmarks[163][0], landmarks[163][1]))
        left_eye_landmarks.append((landmarks[144][0], landmarks[144][1]))
        left_eye_landmarks.append((landmarks[145][0], landmarks[145][1]))
        left_eye_landmarks.append((landmarks[153][0], landmarks[153][1]))
        left_eye_landmarks.append((landmarks[154][0], landmarks[154][1]))
        left_eye_landmarks.append((landmarks[155][0], landmarks[155][1]))

        # 右目
        right_eye_landmarks.append((landmarks[362][0], landmarks[362][1]))
        right_eye_landmarks.append((landmarks[398][0], landmarks[398][1]))
        right_eye_landmarks.append((landmarks[384][0], landmarks[384][1]))
        right_eye_landmarks.append((landmarks[385][0], landmarks[385][1]))
        right_eye_landmarks.append((landmarks[386][0], landmarks[386][1]))
        right_eye_landmarks.append((landmarks[387][0], landmarks[387][1]))
        right_eye_landmarks.append((landmarks[388][0], landmarks[388][1]))
        right_eye_landmarks.append((landmarks[466][0], landmarks[466][1]))
        right_eye_landmarks.append((landmarks[390][0], landmarks[390][1]))
        right_eye_landmarks.append((landmarks[373][0], landmarks[373][1]))
        right_eye_landmarks.append((landmarks[374][0], landmarks[374][1]))
        right_eye_landmarks.append((landmarks[380][0], landmarks[380][1]))
        right_eye_landmarks.append((landmarks[381][0], landmarks[381][1]))
        right_eye_landmarks.append((landmarks[382][0], landmarks[382][1]))

    return left_eye_landmarks, right_eye_landmarks

def calc_bounding_rect(landmarks: list[tuple[int, int]]):
    landmark_array = np.empty((0, 2), int)

    for _, landmark in enumerate(landmarks):
        landmark_x = int(landmark[0])
        landmark_y = int(landmark[1])

        landmark_point = [np.array((landmark_x, landmark_y))]
        landmark_array = np.append(landmark_array, landmark_point, axis=0)

    x, y, w, h = cv.boundingRect(landmark_array)

    return [x, y, x + w, y + h]

def calc_around_eye(bbox: list[int], around_ratio: float = 0.5):
    x1, y1, x2, y2 = bbox
    x = x1
    y = y1
    w = x2 - x1
    h = y2 - y1

    cx = int(x + (w / 2))
    cy = int(y + (h / 2))
    square_length = max(w, h)
    x = int(cx - (square_length / 2))
    y = int(cy - (square_length / 2))
    w = square_length
    h = square_length

    around_ratio = 0.5
    x = int(x - (square_length * around_ratio))
    y = int(y - (square_length * around_ratio))
    w = int(square_length * (1 + (around_ratio * 2)))
    h = int(square_length * (1 + (around_ratio * 2)))

    return [x, y, x + w, y + h]

def calc_around_eye_bbox(landmarks: list[tuple[int, int, float]], around_ratio: float = 0.5):
    # 目の周囲のバウンディングボックスを取得

    left_eye_bbox, right_eye_bbox = calc_eye_bbox(landmarks)

    left_eye_bbox = calc_around_eye(left_eye_bbox, around_ratio)
    right_eye_bbox = calc_around_eye(right_eye_bbox, around_ratio)

    return left_eye_bbox, right_eye_bbox

def calc_eye_bbox(landmarks: list[tuple[int, int, float]]):
    # 目に隣接するバウンディングボックスを取得

    left_eye_lm, right_eye_lm = get_eye_landmarks(landmarks)

    left_eye_bbox = calc_bounding_rect(left_eye_lm)
    right_eye_bbox = calc_bounding_rect(right_eye_lm)

    return left_eye_bbox, right_eye_bbox

def calc_landmarks(image: cv.typing.MatLike, landmarks: list[object]):
    image_width, image_height = image.shape[1], image.shape[0]

    landmark_list: list[tuple[int, int, float]] = []
    for _, landmark in enumerate(landmarks):
        landmark_x = min(int(landmark.x * image_width), image_width - 1)
        landmark_y = min(int(landmark.y * image_height), image_height - 1)

        landmark_list.append((landmark_x, landmark_y, landmark.z))
    return landmark_list
