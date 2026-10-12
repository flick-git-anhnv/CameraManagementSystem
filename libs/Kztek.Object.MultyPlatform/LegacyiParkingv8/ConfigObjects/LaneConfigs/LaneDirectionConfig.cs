using System.Reflection;

namespace iParkingv8.Object.ConfigObjects.LaneConfigs
{
    /// <summary>
    /// Thông tin hu?ng hi?n th? trên ph?n giao di?n
    /// </summary>
    public class LaneDirectionConfig
    {
        public enum EmDisplayDirection
        {
            HorizontalLeftToRight = 0,
            HorizontalRightToLeft = 1,
            VerTicalLeftToRight = 2,
            VerTicalRightToLeft = 3,
        }
        public static string ToDisplayString(EmDisplayDirection displayDirection)
        {
            switch (displayDirection)
            {
                case EmDisplayDirection.HorizontalLeftToRight:
                    return "Horizontal left to right";
                case EmDisplayDirection.HorizontalRightToLeft:
                    return "Horizontal right to left";
                case EmDisplayDirection.VerTicalLeftToRight:
                    return "Vertical left to right";
                case EmDisplayDirection.VerTicalRightToLeft:
                    return "Vertical right to left";
                default:
                    return displayDirection.ToString();
            }
        }
        public enum EmCameraDirection
        {
            Vertical = 0,
            Horizontal = 1,
            HeroTop = 2,
            HeroLeft = 3,
        }
        public static string ToDisplayString(EmCameraDirection cameraDirection)
        {
            return cameraDirection switch
            {
                EmCameraDirection.Vertical => "Vertical",
                EmCameraDirection.Horizontal => "Horizontal",
                _ => cameraDirection.ToString(),
            };
        }
        public enum EmPicDirection
        {
            Vertical = 0,
            Horizontal = 1,
            HeroTop = 2,
            HeroLeft = 3,
        }
        public static string ToDisplayString(EmPicDirection cameraDirection)
        {
            return cameraDirection switch
            {
                EmPicDirection.Vertical => "Vertical",
                EmPicDirection.Horizontal => "Horizontal",
                _ => cameraDirection.ToString(),
            };
        }

        public enum EmPlateDirection
        {
            Vertical = 0,
            HorizontalLeftToRight = 1,
            HorizontalRightToLeft = 2,
        }
        public static string ToDisplayString(EmPlateDirection direction)
        {
            return direction switch
            {
                EmPlateDirection.Vertical => "Vertical",
                EmPlateDirection.HorizontalLeftToRight => "Horizontal left to right",
                EmPlateDirection.HorizontalRightToLeft => "Horizontal right to left",
                _ => direction.ToString(),
            };
        }

        public enum EmCameraPicFunction
        {
            Vertical = 0,
            HorizontalLeftToRight = 1,
            HorizontalRightToLeft = 2,
        }

        public static string ToDisplayString(EmCameraPicFunction cameraPicDirection)
        {
            return cameraPicDirection switch
            {
                EmCameraPicFunction.Vertical => "Vertical",
                EmCameraPicFunction.HorizontalLeftToRight => "Horizontal left to right",
                EmCameraPicFunction.HorizontalRightToLeft => "Horizontal right to left",
                _ => cameraPicDirection.ToString(),
            };
        }
        public enum EmEventDirection
        {
            Vertical = 0,
            HorizontalLeftToRight = 1,
            HorizontalRightToLeft = 2,
        }

        public static string ToDisplayString(EmEventDirection eventDirection)
        {
            return eventDirection switch
            {
                EmEventDirection.Vertical => "Vertical",
                EmEventDirection.HorizontalLeftToRight => "Horizontal left to right",
                EmEventDirection.HorizontalRightToLeft => "Horizontal right to left",
                _ => eventDirection.ToString(),
            };
        }

        public enum EmViewOption
        {
            OnlyData,
            DataAndMoney,
        }
        public EmDisplayDirection displayDirection = EmDisplayDirection.VerTicalLeftToRight;
        public EmCameraPicFunction cameraPicDirection = EmCameraPicFunction.HorizontalLeftToRight;

        #region CAMERA
        public EmCameraDirection cameraDirection = EmCameraDirection.Vertical;
        public bool IsDisplayCameraTitle { get; set; } = true;
        public bool IsDisplayPanoramaCamera { get; set; } = true;
        public bool IsDisplayFaceCamera { get; set; } = false;
        public bool IsDisplayVehicleCamera { get; set; } = true;
        public bool IsDisplayOtherCamera { get; set; } = false;
        #endregion

        #region IMAGE
        public EmPicDirection picDirection = EmPicDirection.Vertical;
        public bool IsDisplayImageTitle { get; set; } = true;
        public bool IsDisplayPanoramaImage { get; set; } = true;
        public bool IsDisplayFaceImage { get; set; } = false;
        public bool IsDisplayVehicleImage { get; set; } = true;
        public bool IsDisplayOtherImage { get; set; } = false;
        #endregion

        #region EVENT
        public EmEventDirection eventDirection = EmEventDirection.HorizontalLeftToRight;
        public EmViewOption optionViewData = EmViewOption.OnlyData;
        public bool IsDisplayTitle { get; set; } = true;
        #endregion

        #region BUTTONS
        public bool IsDisplayWriteEventButton { get; set; } = true;
        public bool IsDisplayRetakeImageButton { get; set; } = true;
        public bool IsDisplayGuestRegisterButton { get; set; } = true;

        public bool IsDisplayOpenBarrieButton { get; set; } = false;
        public bool IsDisplayPrintButton { get; set; } = false;
        public bool IsDisplayCloseBarrieButton { get; set; } = false;
        public bool IsDisplayIconButtonOnly { get; set; } = false;
        #endregion

        #region PLATES
        public EmPlateDirection PlateInDirection { get; set; } = EmPlateDirection.Vertical;
        public EmPlateDirection PlateOutDirection { get; set; } = EmPlateDirection.Vertical;
        #endregion

        public static LaneDirectionConfig CreateDefaultInConfig()
        {
            return new LaneDirectionConfig()
            {
                displayDirection = EmDisplayDirection.VerTicalLeftToRight,
                cameraPicDirection = EmCameraPicFunction.HorizontalLeftToRight,

                #region CAMERA
                cameraDirection = EmCameraDirection.Vertical,
                IsDisplayCameraTitle = true,
                IsDisplayPanoramaCamera = true,
                IsDisplayVehicleCamera = true,
                IsDisplayFaceCamera = false,
                IsDisplayOtherCamera = false,
                #endregion

                #region IMAGE
                picDirection = EmPicDirection.Vertical,
                IsDisplayImageTitle = true,
                IsDisplayPanoramaImage = true,
                IsDisplayVehicleImage = true,
                IsDisplayFaceImage = false,
                IsDisplayOtherImage = false,
                #endregion

                #region EVENT
                eventDirection = EmEventDirection.HorizontalLeftToRight,
                IsDisplayTitle = true,
                optionViewData = EmViewOption.OnlyData,
                #endregion

                #region BUTTONS
                IsDisplayWriteEventButton = true,
                IsDisplayRetakeImageButton = true,
                IsDisplayOpenBarrieButton = true,
                IsDisplayPrintButton = false,

                IsDisplayGuestRegisterButton = false,
                IsDisplayCloseBarrieButton = false,
                IsDisplayIconButtonOnly = false,
                #endregion
            };
        }
        public static LaneDirectionConfig CreateDefaultInSmallConfig()
        {
            return new LaneDirectionConfig()
            {
                displayDirection = EmDisplayDirection.VerTicalLeftToRight,
                cameraPicDirection = EmCameraPicFunction.Vertical,

                #region CAMERA
                cameraDirection = EmCameraDirection.Vertical,
                IsDisplayCameraTitle = true,
                IsDisplayPanoramaCamera = true,
                IsDisplayVehicleCamera = true,
                IsDisplayFaceCamera = false,
                IsDisplayOtherCamera = false,
                #endregion

                #region IMAGE
                picDirection = EmPicDirection.Vertical,
                IsDisplayImageTitle = true,
                IsDisplayPanoramaImage = true,
                IsDisplayVehicleImage = true,
                IsDisplayFaceImage = false,
                IsDisplayOtherImage = false,
                #endregion

                #region EVENT
                eventDirection = EmEventDirection.Vertical,
                IsDisplayTitle = true,
                optionViewData = EmViewOption.OnlyData,
                #endregion

                #region BUTTONS
                IsDisplayWriteEventButton = true,
                IsDisplayRetakeImageButton = true,
                IsDisplayOpenBarrieButton = true,
                IsDisplayPrintButton = false,

                IsDisplayGuestRegisterButton = false,
                IsDisplayCloseBarrieButton = false,
                IsDisplayIconButtonOnly = false,
                #endregion
            };
        }

        public static LaneDirectionConfig CreateDefaultOutConfig()
        {
            return new LaneDirectionConfig()
            {
                displayDirection = EmDisplayDirection.VerTicalLeftToRight,
                cameraPicDirection = EmCameraPicFunction.Vertical,

                #region CAMERA
                cameraDirection = EmCameraDirection.Vertical,
                IsDisplayCameraTitle = true,
                IsDisplayPanoramaCamera = true,
                IsDisplayVehicleCamera = true,
                IsDisplayFaceCamera = false,
                IsDisplayOtherCamera = false,
                #endregion

                #region IMAGE
                picDirection = EmPicDirection.Vertical,
                IsDisplayImageTitle = true,
                IsDisplayPanoramaImage = true,
                IsDisplayVehicleImage = true,
                IsDisplayFaceImage = false,
                IsDisplayOtherImage = false,
                #endregion

                #region EVENT
                eventDirection = EmEventDirection.HorizontalLeftToRight,
                IsDisplayTitle = true,
                optionViewData = EmViewOption.DataAndMoney,
                #endregion

                #region BUTTONS
                IsDisplayWriteEventButton = true,
                IsDisplayRetakeImageButton = true,
                IsDisplayOpenBarrieButton = true,
                IsDisplayPrintButton = true,

                IsDisplayGuestRegisterButton = false,
                IsDisplayCloseBarrieButton = false,
                IsDisplayIconButtonOnly = false,
                #endregion
            };

        }
        public static LaneDirectionConfig CreateDefaultOutSmallConfig()
        {
            return new LaneDirectionConfig()
            {
                displayDirection = EmDisplayDirection.VerTicalLeftToRight,
                cameraPicDirection = EmCameraPicFunction.Vertical,

                #region CAMERA
                cameraDirection = EmCameraDirection.Vertical,
                IsDisplayCameraTitle = true,
                IsDisplayPanoramaCamera = true,
                IsDisplayVehicleCamera = true,
                IsDisplayFaceCamera = false,
                IsDisplayOtherCamera = false,
                #endregion

                #region IMAGE
                picDirection = EmPicDirection.Vertical,
                IsDisplayImageTitle = true,
                IsDisplayPanoramaImage = true,
                IsDisplayVehicleImage = true,
                IsDisplayFaceImage = false,
                IsDisplayOtherImage = false,
                #endregion

                #region EVENT
                eventDirection = EmEventDirection.Vertical,
                IsDisplayTitle = true,
                optionViewData = EmViewOption.DataAndMoney,
                #endregion

                #region BUTTONS
                IsDisplayWriteEventButton = true,
                IsDisplayRetakeImageButton = true,
                IsDisplayOpenBarrieButton = true,
                IsDisplayPrintButton = true,

                IsDisplayGuestRegisterButton = false,
                IsDisplayCloseBarrieButton = false,
                IsDisplayIconButtonOnly = false,
                #endregion
            };

        }

        public bool IsSameConfig(LaneDirectionConfig config)
        {
            if (ReferenceEquals(this, config)) return true;
            if (config is null) return false;

            var type = typeof(LaneDirectionConfig);

            // L?y t?t c? public instance properties có getter
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(p => p.CanRead);

            foreach (var p in props)
            {
                var v1 = p.GetValue(this);
                var v2 = p.GetValue(config);

                if (!Equals(v1, v2))
                    return false;
            }

            return true;
        }
    }
}

