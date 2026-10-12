using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases;

namespace iParkingv8.Object.Objects.Events
{
    public class BaseEvent
    {
        public List<string> ErrorMessages { get; set; } = [];
        public List<EmAlarmCode> GetAlarmCode()
        {
            if (ErrorMessages.Count == 0) return [];
            var result = new List<EmAlarmCode>();
            foreach (var message in ErrorMessages)
            {
                switch (message)
                {
                    case "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_SYSTEM":
                        result.Add(EmAlarmCode.PLATE_NUMBER_DIFFERENCE_SYSTEM);
                        break;
                    case "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ENTRY":
                        result.Add(EmAlarmCode.PLATE_NUMBER_INVALID);
                        break;
                    case "ERROR.ENTITY.VALIDATION.BLACKLISTED":
                        result.Add(EmAlarmCode.PLATE_NUMBER_BLACKLISTED);
                        break;
                    default:
                        result.Add(EmAlarmCode.SYSTEM_ERROR);
                        break;
                }
            }
            return result;
        }

        public List<string> ToVI()
        {
            if (ErrorMessages.Count == 0) return [];

            var result = new List<string>();
            foreach (var message in ErrorMessages)
            {
                var alarmCode = message switch
                {
                    "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_SYSTEM" => EmAlarmCode.PLATE_NUMBER_DIFFERENCE_SYSTEM,
                    "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ENTRY" => EmAlarmCode.PLATE_NUMBER_INVALID,
                    "ERROR.ENTITY.VALIDATION.BLACKLISTED" => EmAlarmCode.PLATE_NUMBER_BLACKLISTED,
                    _ => EmAlarmCode.SYSTEM_ERROR,
                };

                if (alarmCode == EmAlarmCode.SYSTEM_ERROR)
                {
                    result.Add(message);
                    continue;
                }

                var translated = alarmCode.ToString();
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    result.Add(translated);
                }
            }

            return result;
        }
    }
}
