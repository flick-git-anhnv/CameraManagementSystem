namespace Kztek.Cameras
{
    public class UsingPlugins
    {
        public static UsingPlugin GetType(string UsingPlugin)
        {
            return (UsingPlugin)Enum.Parse(typeof(UsingPlugin), UsingPlugin, true);
        }

        public static UsingPlugin GetType(int index)
        {
            return (UsingPlugin)index;
        }

        public static UsingPlugin GetUsingPlugin(string usingPlugin)
        {
            switch (usingPlugin)
            {
                case "Default":
                    {
                        return UsingPlugin.Default;
                    }
                case "KztekSDK1":
                    {
                        return UsingPlugin.KztekSDK1;
                    }
                case "KztekSDK2":
                    {
                        return UsingPlugin.KztekSDK2;
                    }
                case "KztekSDK3":
                    {
                        return UsingPlugin.KztekSDK3;
                    }
                default:
                    return UsingPlugin.Default;
            }
        }
    }
}
