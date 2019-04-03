using UnityEngine;
using UnityEngine.XR;
using BeatChallenge.Utils;
using BeatChallenge.Packets;
namespace BeatChallenge.Controllers
{
    class WorldController
    {
        public static Quaternion oculusTouchRotOffset = Quaternion.Euler(-40f, 0f, 0f);
        public static Vector3 oculusTouchPosOffset = new Vector3(0f, 0f, 0.055f);
        public static Quaternion openVrRotOffset = Quaternion.Euler(-4.3f, 0f, 0f);
        public static Vector3 openVrPosOffset = new Vector3(0f, -0.008f, 0f);

        private struct PosRot
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }

            public PosRot(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
        private static PosRot GetXRNodeWorldPosRot(XRNode node)
        {
            var pos = InputTracking.GetLocalPosition(node);
            var rot = InputTracking.GetLocalRotation(node);

            var roomCenter = BeatSaberUtil.GetRoomCenter();
            var roomRotation = BeatSaberUtil.GetRoomRotation();

            pos = roomRotation * pos;
            pos += roomCenter;
            rot = roomRotation * rot;
            return new PosRot(pos, rot);
        }
        private class HandOffset
        {
            public Quaternion LeftHandRot { get; set; }
            public Vector3 LeftHandPos { get; set; }
        }
        private static HandOffset GetLeftHandOffs()
        {

            if (PersistentSingleton<VRPlatformHelper>.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Oculus)
            {
                return new HandOffset
                {
                    LeftHandRot = oculusTouchRotOffset,
                    LeftHandPos = oculusTouchPosOffset
                };
            }
            else if (PersistentSingleton<VRPlatformHelper>.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR)
            {
                return new HandOffset
                {
                    LeftHandRot = openVrRotOffset,
                    LeftHandPos = openVrPosOffset
                };
            }
            return null;
        }
        

        public static CharacterPosition GetCharacterPosition()
        {
            HandOffset leftOffs = GetLeftHandOffs();
            return new CharacterPosition
            {
                position_head = GetXRNodeWorldPosRot(XRNode.Head).Position,
                rotation_head = GetXRNodeWorldPosRot(XRNode.Head).Rotation,
                position_leftHand = GetXRNodeWorldPosRot(XRNode.LeftHand).Position + leftOffs.LeftHandPos,
                rotation_leftHand = GetXRNodeWorldPosRot(XRNode.LeftHand).Rotation * leftOffs.LeftHandRot,
                position_rightHand = GetXRNodeWorldPosRot(XRNode.RightHand).Position,
                rotation_rightHand = GetXRNodeWorldPosRot(XRNode.RightHand).Rotation
            };
        }
    }
}
