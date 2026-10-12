namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.Helpers
{
    public class RabbitMQHelper
    {
        public const string EXCHANGE_CMS_MONITOR_NAME = "resource.application.monitor.computer";
        public const string EXCHANGE_CMS_OPERATOR_NAME = "resource.application.operator";
        public const string EXCHANGE_PAYMENT_NAME = "event.application.event.transaction.changed";
        public const string EXCHANGE_ACCESS_EVENT_NAME = "access_control.event.monitor";
        public const string EXCHANGE_CONTROL_CENTER = "event.application.control_center.operator";

        public static string queueMonitorDeviceBaseName = "queue.device.monitor.computer";
        public static string queueMonitorPaymentQueueBaseName = "queue.payment.monitor";
        public static string queueOperatorBaseName = "queue.device.operator";
        public static string controllerEventInitQueueName = "queue.ControllerEvent";
        public static string queueControlCenterBaseName = "queue.ControlCenter";
    }
}
