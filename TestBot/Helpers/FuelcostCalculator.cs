namespace TelegramBot.TestBot.Helpers
{
    using System;

    public class FuelcostCalculator
    {
        public FuelcostCalculator(double tripDistance, double fuelEfficiency, decimal fuelPriceLiter, string currency = Currencies.Euro)
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
            Currency = currency;
        }

        public double TripDistance { get; }

        public double FuelEfficiency { get; }

        public decimal FuelPriceLiter { get; }

        public string Currency { get; }

        public double TripFuelUsedLiters => (TripDistance / 100) * FuelEfficiency;

        public decimal TripCost => (decimal)TripFuelUsedLiters * FuelPriceLiter;

        public string TripCostFormatted =>
                $"<b>Distance:</b> {TripDistance:0.00} km\n" +
                $"<b>Avg. fuel consumption:</b> {FuelEfficiency:0.00} l/100km\n" +
                $"<b>Fuel cost:</b> {FuelPriceLiter:0.00} {Currency}/l\n" +
                $"This trip will require <b>{TripFuelUsedLiters:0.00}</b> liter(s) of fuel, " +
                $"which amounts to a fuel cost of <b>{TripCost:0.00}</b> {Currency}";

        public struct Currencies
        {
            public const string Euro = "EUR";
            public const string UsDollar = "USD";
            public const string BritishPound = "GBP";
        }
    }
}
