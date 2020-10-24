using System;

namespace TelegramBot.Helpers
{
    public class FuelcostCalculator
    {
        public double TripDistance { get; private set; }
        public double FuelEfficiency { get; private set; }
        public decimal FuelPriceLiter { get; private set; }
        public double TripFuelUsedLiters => (TripDistance / 100) * FuelEfficiency;
        public decimal TripCost => (decimal)TripFuelUsedLiters * FuelPriceLiter;

        public FuelcostCalculator(double tripDistance, double fuelEfficiency, decimal fuelPriceLiter)
        {
            if (tripDistance <= 0)
            {
                throw new ArgumentException(nameof(tripDistance));
            }

            if (fuelEfficiency <= 0)
            {
                throw new ArgumentException(nameof(fuelEfficiency));
            }

            if (fuelPriceLiter <= 0)
            {
                throw new ArgumentException(nameof(fuelPriceLiter));
            }

            TripDistance = tripDistance;
            FuelEfficiency = fuelEfficiency;
            FuelPriceLiter = fuelPriceLiter;
        }

        public string TripCostFormatted =>
                $"<b>Distance:</b> {TripDistance:0.00} km\n" +
                $"<b>Avg. fuel consumption:</b> {FuelEfficiency:0.00} l/100km\n" +
                $"<b>Fuel cost:</b> {FuelPriceLiter:0.00} EUR/l\n" +
                $"This trip will require <b>{TripFuelUsedLiters:0.00}</b> liter(s) of fuel, " +
                $"which amounts to a fuel cost of <b>{TripCost:0.00}</b> EUR";
    }
}
