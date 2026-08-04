using CustomerService.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string? Phone { get; private set; }
        public Address Location { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }
        //public string? ProfilePictureUrl{get;private set; }
        public string? ProfilePictureFileName { get; private set; }

        // Private constructor - EF Core కోసం
        private Customer() { }

        //By using SAS url we save the image
        //public void SetProfilePicture(string url)
        //{
        //    ProfilePictureUrl = url;
        //}
        public void SetProfilePicture(string fileName)
        {
            ProfilePictureFileName = fileName;
        }

        // Public constructor - కొత్త Customer create చేయడానికి
        public Customer(string name, string email, string phone, Address location)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name cannot be empty.");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Customer email cannot be empty.");

            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Phone = phone;
            Location = location;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        // Business methods - Customer update చేయడానికి
        public void UpdateDetails(string name, string phone, Address location)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name cannot be empty.");

            Name = name;
            Phone = phone;
            Location = location;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}
