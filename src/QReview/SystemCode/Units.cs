using FieldDataPluginFramework.Units;

namespace QReview.SystemCode
{
    public static class Units
    {
        public static string Celcius => "degC";

        public static UnitSystem MetricUnitSystem => new UnitSystem
        {
            DistanceUnitId = "m",
            AreaUnitId = "m^2",
            VelocityUnitId = "m/s",
            DischargeUnitId = "m^3/s",
        };

        public static UnitSystem ImperialUnitSystem => new UnitSystem
        {
            DistanceUnitId = "ft",
            AreaUnitId = "ft^2",
            VelocityUnitId = "ft/s",
            DischargeUnitId = "ft^3/s"
        };
    }
}
