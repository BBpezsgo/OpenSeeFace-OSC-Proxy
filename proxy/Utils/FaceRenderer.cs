using System.Collections.Immutable;
using System.Numerics;
using CLI;

namespace VRChatProxy;

public static class FaceRenderer
{
    static readonly ImmutableArray<(int A, int B)> FACEMESH_LIPS = [(61, 146), (146, 91), (91, 181), (181, 84), (84, 17),
                           (17, 314), (314, 405), (405, 321), (321, 375),
                           (375, 291), (61, 185), (185, 40), (40, 39), (39, 37),
                           (37, 0), (0, 267),
                           (267, 269), (269, 270), (270, 409), (409, 291),
                           (78, 95), (95, 88), (88, 178), (178, 87), (87, 14),
                           (14, 317), (317, 402), (402, 318), (318, 324),
                           (324, 308), (78, 191), (191, 80), (80, 81), (81, 82),
                           (82, 13), (13, 312), (312, 311), (311, 310),
                           (310, 415), (415, 308)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_LEFT_EYE = [(263, 249), (249, 390), (390, 373), (373, 374),
                               (374, 380), (380, 381), (381, 382), (382, 362),
                               (263, 466), (466, 388), (388, 387), (387, 386),
                               (386, 385), (385, 384), (384, 398), (398, 362)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_LEFT_IRIS = [(474, 475), (475, 476), (476, 477),
                                 (477, 474)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_LEFT_EYEBROW = [(276, 283), (283, 282), (282, 295),
                                   (295, 285), (300, 293), (293, 334),
                                   (334, 296), (296, 336)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_RIGHT_EYE = [(33, 7), (7, 163), (163, 144), (144, 145),
                                (145, 153), (153, 154), (154, 155), (155, 133),
                                (33, 246), (246, 161), (161, 160), (160, 159),
                                (159, 158), (158, 157), (157, 173), (173, 133)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_RIGHT_EYEBROW = [(46, 53), (53, 52), (52, 65), (65, 55),
                                    (70, 63), (63, 105), (105, 66), (66, 107)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_RIGHT_IRIS = [(469, 470), (470, 471), (471, 472),
            (472, 469)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_FACE_OVAL = [(10, 338), (338, 297), (297, 332), (332, 284),
        (284, 251), (251, 389), (389, 356), (356, 454),
        (454, 323), (323, 361), (361, 288), (288, 397),
        (397, 365), (365, 379), (379, 378), (378, 400),
        (400, 377), (377, 152), (152, 148), (148, 176),
        (176, 149), (149, 150), (150, 136), (136, 172),
        (172, 58), (58, 132), (132, 93), (93, 234),
        (234, 127), (127, 162), (162, 21), (21, 54),
        (54, 103), (103, 67), (67, 109), (109, 10)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_NOSE = [(168, 6), (6, 197), (197, 195), (195, 5),
        (5, 4), (4, 1), (1, 19), (19, 94), (94, 2), (98, 97),
        (97, 2), (2, 326), (326, 327), (327, 294),
        (294, 278), (278, 344), (344, 440), (440, 275),
        (275, 4), (4, 45), (45, 220), (220, 115), (115, 48),
        (48, 64), (64, 98)];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_CONTOURS = [
        ..FACEMESH_LIPS, ..FACEMESH_LEFT_EYE, ..FACEMESH_LEFT_EYEBROW,.. FACEMESH_RIGHT_EYE,
    ..FACEMESH_RIGHT_EYEBROW, ..FACEMESH_FACE_OVAL
    ];

    static readonly ImmutableArray<(int A, int B)> FACEMESH_IRISES = [..FACEMESH_LEFT_IRIS, ..FACEMESH_RIGHT_IRIS];

    public static void RenderFace(this AnsiRendererExtended renderer, in FaceData face, in Camera camera)
    {
        for (int i = 0; i < 468; i++)
        {
            Vector2 p = renderer.Transform(face.Points[i], camera);
            renderer[p] = new AnsiChar('.', AnsiColor.White);
        }

        for (int i = 0; i < FACEMESH_CONTOURS.Length; i++)
        {
            (int a, int b) = FACEMESH_CONTOURS[i];
            renderer.RenderLine(face.Points[a], face.Points[b], camera, AnsiColor.White);
        }
    }
}
