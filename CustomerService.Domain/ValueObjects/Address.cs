using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Domain.ValueObjects
{
    public class Address
    {
        public string Street { get; private set; } = null!;
        public string City { get; private set; } = null!;
        public string State { get; private set; } = null!;
        public string ZipCode { get; private set; } = null!;
        public string Country { get; private set; } = null!;

        // For EF Core - private constructor
        private Address() { }

        public Address(string street, string city, string state, string zipCode, string country)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty.");

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty.");

            if (string.IsNullOrWhiteSpace(zipCode))
                throw new ArgumentException("ZipCode cannot be empty.");

            Street = street;
            City = city;
            State = state;
            ZipCode = zipCode;
            Country = country;
        }

        // Value Object equality - not throgh identity , it's through values compare
        public override bool Equals(object? obj)
        {
            if (obj is not Address other)
                return false;

            return Street == other.Street &&
                   City == other.City &&
                   State == other.State &&
                   ZipCode == other.ZipCode &&
                   Country == other.Country;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Street, City, State, ZipCode, Country);
        }

        public override string ToString()
        {
            return $"{Street}, {City}, {State} {ZipCode}, {Country}";
        }
    }
}
