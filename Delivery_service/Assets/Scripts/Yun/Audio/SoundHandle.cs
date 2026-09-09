using System;

namespace DeliveryService.Yun.Audio
{
    /// <summary>
    /// 하나의 재생 인스턴스를 가리키는 핸들.
    /// 풀에서 해당 슬롯이 재사용되면 더 이상 유효하지 않다.
    /// </summary>
    public readonly struct SoundHandle : IEquatable<SoundHandle>
    {
        internal readonly int index;
        internal readonly int generation;

        internal SoundHandle(int index, int generation)
        {
            this.index = index;
            this.generation = generation;
        }

        public static SoundHandle Invalid => new SoundHandle(-1, 0);

        public bool Equals(SoundHandle other)
        {
            return index == other.index && generation == other.generation;
        }

        public override bool Equals(object obj)
        {
            return obj is SoundHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (index * 397) ^ generation;
        }

        public static bool operator ==(SoundHandle left, SoundHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SoundHandle left, SoundHandle right)
        {
            return !left.Equals(right);
        }

        public bool IsValid
        {
            get
            {
                AudioManager manager = AudioManager.Instance;
                return manager != null && manager.IsHandleValid(this);
            }
        }

        public bool IsPlaying
        {
            get
            {
                AudioManager manager = AudioManager.Instance;
                return manager != null && manager.IsPlaying(this);
            }
        }

        public void Stop()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null)
            {
                manager.Stop(this);
            }
        }

        public void SetVolume(float volume)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null)
            {
                manager.SetVolume(this, volume);
            }
        }

        public void SetPitch(float pitch)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null)
            {
                manager.SetPitch(this, pitch);
            }
        }
    }
}
