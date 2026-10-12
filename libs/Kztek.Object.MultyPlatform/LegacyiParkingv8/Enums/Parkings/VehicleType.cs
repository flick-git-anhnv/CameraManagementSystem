namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Parkings;

public enum EmVehicleType
{
    CAR,
    MOTORBIKE,
    BIKE
}

public static class VehicleType
{
    public static string ToDisplayString(EmVehicleType vehicleType) => vehicleType switch
    {
        EmVehicleType.CAR => "Car",
        EmVehicleType.MOTORBIKE => "Motorbike",
        EmVehicleType.BIKE => "Bike",
        _ => string.Empty,
    };
}
