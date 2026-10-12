using System;

namespace Kztek.Object.MultyPlatform
{
    public static class KztekLocalizationHelper
    {
        public static Func<string, string> Translate { get; set; } = key => key;
    }
}
