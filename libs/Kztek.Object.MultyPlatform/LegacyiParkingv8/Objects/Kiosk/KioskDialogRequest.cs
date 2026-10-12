using Avalonia.Media.Imaging;
using iParkingv8.Object.Objects.Parkings;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.Kiosk
{
    public enum EmImageDialogType
    {
        Error,
        Infor,
        Success
    }
    public class KioskDialogRequest(string Title, string subTitle, string backTitle,
                              EmImageDialogType dialogType, Bitmap? dislayImage)
    {
        public string TitleLanguageTag { get; set; } = Title;
        public string SubTitleLanguageTag { get; set; } = subTitle;
        public string BackTitleTag { get; set; } = backTitle;
        public EmImageDialogType DialogType { get; set; } = dialogType;
        public Bitmap? DisplayImage { get; set; } = dislayImage;
    }

    public class KioskDialogDialyRequest(string title, string subTitle,
                                   EmImageDialogType dialogType,
                                   string detectedPlate, AccessKey? accessKey,
                                   Bitmap? vehicleImage, Bitmap? panoramaImage, int displayDuration)
    {
        public string TitleTag { get; set; } = title;
        public string SubTitleTag { get; set; } = subTitle;

        public EmImageDialogType DialogType { get; set; } = dialogType;

        public Bitmap? VehicleImage { get; set; } = vehicleImage;
        public Bitmap? PanoramaImage { get; set; } = panoramaImage;

        public string DetectedPlate { get; set; } = detectedPlate;
        public AccessKey? AccessKey { get; set; } = accessKey;
        public int DisplayDuration { get; set; } = displayDuration;
    }

    public class KioskDialogMonthlyRequest(string TitleTag, string subTitletag,
                                      EmImageDialogType dialogType,
                                      string detectedPlate, AccessKey? accessKey, AccessKey? registerVehicle,
                                      Bitmap? vehicleImage, Bitmap? panoramaImage, int displayDuration)
    {
        public string TitleTag { get; set; } = TitleTag;
        public string SubTitleTag { get; set; } = subTitletag;
        public EmImageDialogType DialogType { get; set; } = dialogType;

        public Bitmap? VehicleImage { get; set; } = vehicleImage;
        public Bitmap? PanoramaImage { get; set; } = panoramaImage;

        public string DetectedPlate { get; set; } = detectedPlate;
        public AccessKey? AccessKey { get; set; } = accessKey;
        public AccessKey? RegisterVehicle { get; set; } = registerVehicle;
        public int DisplayDuration { get; set; } = displayDuration;
    }
}
