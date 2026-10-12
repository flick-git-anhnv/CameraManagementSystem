using Kztek.Object;
using System;
using System.Collections.Generic;
using static Kztek.Object.InputTupe;

namespace iParkingv5.Objects.Events
{
    public delegate void ControllerErrorEventHandler(object sender, ControllerErrorEventArgs e);
    public delegate void InputEventHandler(object sender, InputEventArgs e);
    public delegate void CardEventHandler(object sender, CardEventArgs e);

    public delegate void CardOnRFEventHandler(object sender, CardOnRFEventArgs e);
    public delegate void OnCardNotSupportEventHandler(object sender, CardNotSupportEventArgs e);

    public delegate void CancelEventHandler(object sender, CardCancelEventArgs e);
    public delegate void ConnectStatusChangeEventHandler(object sender, ConnectStatusCHangeEventArgs e);
    public delegate void DeviceInfoChangeEventHandler(object sender, DeviceInfoChangeArgs e);
    public delegate void DisplayTextChangeEventHandler(object? sender, TextChangeEventArgs e);
    public delegate void FingerEventHandler(object sender, FingerEventArgs e);
    public delegate void OnChangeLaneEvent(object sender);
    public delegate void OnControlSizeChanged(object sender, ControlSizeChangedEventArgs type);
    public delegate void CardBeTakenHandler(object sender, CardBeTakenEventArgs e);

    public enum EmParkingControllerType
    {
        NomalController,
        Card_Dispenser
    }
    public class ControlSizeChangedEventArgs : EventArgs
    {
        public int Type { get; set; }
        public int NewX { get; set; }
        public int NewY { get; set; }
        public int NewDistance { get; set; }
    }
    public class GeneralEventArgs : EventArgs
    {
        public string IntergratedDeviceId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime EventTime { get; set; } = DateTime.Now;
        public EmParkingControllerType DeviceType { get; set; } = EmParkingControllerType.NomalController;
    }

    public class CardNotSupportEventArgs : GeneralEventArgs
    {

    }
    public class ControllerErrorEventArgs : GeneralEventArgs
    {
        public string ErrorString { get; set; } = string.Empty;
        public string ErrorFunc { get; set; } = string.Empty;
        public string ErrorLine { get; set; } = string.Empty;
        public string CMD { get; set; } = string.Empty;
    }
    public class InputEventArgs : GeneralEventArgs
    {
        public int InputIndex { get; set; }
        public EmInputType InputType { get; set; }
        public string Status { get; set; }
    }
    public class CardOnRFEventArgs : GeneralEventArgs
    {
        public List<string> AllCardFormats { get; set; } = new List<string>();
        public string PreferCard { get; set; } = string.Empty;
        public int ReaderIndex { get; set; }
    }


    public class CardEventArgs : GeneralEventArgs
    {
        public List<string> AllCardFormats { get; set; } = new List<string>();
        public string PreferCard { get; set; } = string.Empty;
        public int ReaderIndex { get; set; }
        public string Doors { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public EmInputType InputType { get; set; } = EmInputType.Card;
        public int ButtonIndex { get; set; }
        public string Note { get; set; } = string.Empty;
        /// <summary>
        /// //- 1 thẻ, 2 QR, 3 Finger, 4 Face
        /// </summary>
        public int Type { get; set; } = 1;
        public bool IsInWaitingTime(CardEventArgs e, int waitingTime, out int thoiGianCho)
        {
            thoiGianCho = 0;
            if (DeviceId != e.DeviceId)
            {
                return false;
            }
            foreach (string format in AllCardFormats)
            {
                if (!e.AllCardFormats.Contains(format))
                {
                    return false;
                }
            }

            double v = (EventTime - e.EventTime).TotalSeconds;
            if (v < waitingTime)
            {
                thoiGianCho = (int)(waitingTime - v);
                return true;
            }
            return false;
        }
    }
    public class CardCancelEventArgs : GeneralEventArgs
    {
        public string PreferCard { get; set; } = "";

        public int FunctionKey { get; set; } = 0;

        public int InputIndex { get; set; }

        public EmInputType InputType { get; set; }

        public string Status { get; set; }

    }

    public class CardBeTakenEventArgs : GeneralEventArgs
    {
        public string PreferCard { get; set; } = "";

        public int FunctionKey { get; set; } = 0;

        public int InputIndex { get; set; }

        public EmInputType InputType { get; set; }

        public string Status { get; set; }

    }
    public class FingerEventArgs : GeneralEventArgs
    {
        public string UserId { get; set; }
        public int ReaderIndex { get; set; }
    }

    public class ConnectStatusCHangeEventArgs : GeneralEventArgs
    {
        public bool CurrentStatus { get; set; }
        public string Reason { get; set; }
        public CardDispenserOnChange DispenserOnChange { get; set; }
    }

    public class DeviceInfoChangeArgs : GeneralEventArgs
    {
        public string InfoKey { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }
    public class TextChangeEventArgs : GeneralEventArgs
    {
        public string CurrentText { get; set; } = string.Empty;
        public string UpdateText { get; set; } = string.Empty;
        public string Cmd { get; set; } = string.Empty;
    }
}
