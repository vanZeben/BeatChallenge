using BeatChallenge.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = BeatChallenge.Utils.Logger;

namespace BeatChallenge.Packets
{
    public class CharacterPosition
    {
        public Vector3 position_head { get; set; } = new Vector3(0, 0, 0);
        public Vector3 position_leftHand { get; set; } = new Vector3(0, 0, 0);
        public Vector3 position_rightHand { get; set; } = new Vector3(0, 0, 0);
        public Quaternion rotation_head { get; set; } = new Quaternion();
        public Quaternion rotation_rightHand { get; set; } = new Quaternion();
        public Quaternion rotation_leftHand { get; set; } = new Quaternion();
    }

    public class ReplayPacket
    {
        private CharacterPosition _characterPosition;
        public CharacterPosition CharacterPosition {
            get { return _characterPosition; }
            set
            {
                _characterPosition = value;
            }
        }

        public ReplayPacket(CharacterPosition characterPosition) {
            CharacterPosition = characterPosition;
        }
        public ReplayPacket(string data)
        {
            try
            {
                FromBytes(DeSerialize(data));
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
            
        private void FromBytes(byte[] data)
        {
            CharacterPosition = new CharacterPosition();
            CharacterPosition.position_rightHand = Serialization.ToVector3(data.Take(6).ToArray());
            CharacterPosition.position_leftHand = Serialization.ToVector3(data.Skip(6).Take(6).ToArray());
            CharacterPosition.position_head = Serialization.ToVector3(data.Skip(12).Take(6).ToArray());

            CharacterPosition.rotation_rightHand = Serialization.ToQuaternion(data.Skip(18).Take(8).ToArray());
            CharacterPosition.rotation_leftHand = Serialization.ToQuaternion(data.Skip(26).Take(8).ToArray());
            CharacterPosition.rotation_head = Serialization.ToQuaternion(data.Skip(34).Take(8).ToArray());
        }

        private byte[] GetBytes()
        {
            List<byte> buffer = new List<byte>();
            
            buffer.AddRange(Serialization.Combine(
                            Serialization.ToBytes(CharacterPosition.position_rightHand),
                            Serialization.ToBytes(CharacterPosition.position_leftHand),
                            Serialization.ToBytes(CharacterPosition.position_head),
                            Serialization.ToBytes(CharacterPosition.rotation_rightHand),
                            Serialization.ToBytes(CharacterPosition.rotation_leftHand),
                            Serialization.ToBytes(CharacterPosition.rotation_head)));
            
            return buffer.ToArray();
        }

        public string Serialize()
        {
            return Convert.ToBase64String(GetBytes());
        }

        public byte[] DeSerialize(string body)
        {
            return Convert.FromBase64String(body);
        }
    }
}
