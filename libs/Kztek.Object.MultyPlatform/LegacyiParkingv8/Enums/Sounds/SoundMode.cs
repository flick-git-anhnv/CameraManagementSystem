namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Sounds
{
    public enum EmSoundMode
    {
        NO,
        PlaySoundFromPC,
        PlaySoundFromController
    }
    public static class SoundMode
    {
        public static string ToDisplayString(this EmSoundMode mode)
        {
            return mode switch
            {
                EmSoundMode.NO => "Không sử dụng",
                EmSoundMode.PlaySoundFromPC => "Phát âm thanh từ máy tính",
                EmSoundMode.PlaySoundFromController => "Phát âm thanh từ bộ điều khiển",
                _ => "Không sử dụng",
            };
        }
    }
}
