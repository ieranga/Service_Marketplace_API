using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Service_Marketplace_API.Entities;
using Service_Marketplace_API.Entities.Enums;
using Service_Marketplace_API.Services.Common;

namespace Service_Marketplace_API.Data;

public static class MarketplaceDataSeeder
{
    public static async Task SeedMarketplaceDataAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, IConfiguration? configuration = null)
    {
        // 0. Seed Super Admin if not existing (Configured via appsettings.json)
        var superAdminSection = configuration?.GetSection("SuperAdmin");
        var superAdminEmail = (superAdminSection?["Email"] ?? "ishara11626@gmail.com").Trim();
        var superAdminFullName = (superAdminSection?["FullName"] ?? "ishare eranga").Trim();
        var superAdminPhone = (superAdminSection?["PhoneNumber"] ?? "+94770000000").Trim();
        var superAdminPassword = superAdminSection?["Password"] ?? "SuperAdmin123!";
        var superAdminCity = (superAdminSection?["City"] ?? "Negombo").Trim();
        var superAdminBio = (superAdminSection?["Bio"] ?? "Primary System Super Administrator").Trim();
        var superAdminIdStr = superAdminSection?["Id"];
        var superAdminId = Guid.TryParse(superAdminIdStr, out var parsedGuid)
            ? parsedGuid
            : Guid.Parse("00000000-0000-0000-0000-000000000003");

        var existingSuperAdmin = await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == superAdminEmail.ToLower() || u.Id == superAdminId);

        if (existingSuperAdmin == null)
        {
            var superAdminUser = new User
            {
                Id = superAdminId,
                FullName = superAdminFullName,
                Email = superAdminEmail,
                PhoneNumber = superAdminPhone,
                PasswordHash = passwordHasher.HashPassword(superAdminPassword),
                Role = UserRole.SuperAdmin, // Role ID 3
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = superAdminBio,
                    City = superAdminCity,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddAsync(superAdminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            // Sync user profile fields if updated in appsettings.json
            bool isModified = false;

            if (existingSuperAdmin.FullName != superAdminFullName)
            {
                existingSuperAdmin.FullName = superAdminFullName;
                isModified = true;
            }
            if (existingSuperAdmin.PhoneNumber != superAdminPhone)
            {
                existingSuperAdmin.PhoneNumber = superAdminPhone;
                isModified = true;
            }
            if (existingSuperAdmin.Profile != null)
            {
                if (existingSuperAdmin.Profile.City != superAdminCity)
                {
                    existingSuperAdmin.Profile.City = superAdminCity;
                    isModified = true;
                }
                if (existingSuperAdmin.Profile.Bio != superAdminBio)
                {
                    existingSuperAdmin.Profile.Bio = superAdminBio;
                    isModified = true;
                }
            }

            if (isModified)
            {
                await context.SaveChangesAsync();
            }
        }

        // 1. Seed Tags if empty
        if (!await context.Tags.AnyAsync())
        {
            var tags = new List<Tag>
            {
                new() { Name = "Home Visit", Slug = "home-visit", TagGroup = "LocationType" },
                new() { Name = "Mobile Service", Slug = "mobile-service", TagGroup = "LocationType" },
                new() { Name = "Weekend", Slug = "weekend", TagGroup = "Timing" },
                new() { Name = "Emergency", Slug = "emergency", TagGroup = "Urgency" },
                new() { Name = "Same Day Service", Slug = "same-day-service", TagGroup = "Urgency" },
                new() { Name = "24/7 Available", Slug = "24-7-available", TagGroup = "Timing" },
                new() { Name = "Certified Technician", Slug = "certified-technician", TagGroup = "Skill" },
                new() { Name = "Eco-Friendly", Slug = "eco-friendly", TagGroup = "Feature" },
                new() { Name = "Commercial", Slug = "commercial", TagGroup = "Scope" },
                new() { Name = "Residential", Slug = "residential", TagGroup = "Scope" }
            };

            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
        }

        // 2. Seed Categories, Services, Variants if empty
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new()
                {
                    Name = "Transportation",
                    Slug = "transportation",
                    Description = "Vehicle cleaning, repairs, rentals, and driver services",
                    Icon = "car-sport",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "Vehicle Cleaning",
                            Slug = "vehicle-cleaning",
                            Description = "Car and bike washing, detailing, and interior cleaning",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Car Washing", Slug = "car-washing", SuggestedStartingPrice = 2500m, Description = "Exterior pressure wash, foam cleaning, and tire shine" },
                                new() { Name = "Full Auto Detailing", Slug = "full-auto-detailing", SuggestedStartingPrice = 12000m, Description = "Deep interior shampoo, cut & polish, ceramic coat" },
                                new() { Name = "Motorbike Washing", Slug = "motorbike-washing", SuggestedStartingPrice = 1000m, Description = "Complete motorbike wash and degreasing" }
                            }
                        },
                        new()
                        {
                            Name = "Driver Services",
                            Slug = "driver-services",
                            Description = "Personal chauffeurs and trip drivers",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Personal Chauffeur", Slug = "personal-chauffeur", SuggestedStartingPrice = 4000m, Description = "Daily personal driver for town runs" },
                                new() { Name = "Outstation Trip Driver", Slug = "outstation-trip-driver", SuggestedStartingPrice = 7500m, Description = "Experienced driver for long distance trips" }
                            }
                        },
                        new()
                        {
                            Name = "Tire Services",
                            Slug = "tire-services",
                            Description = "Puncture repair, tire replacement, wheel alignment",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Mobile Puncture Repair", Slug = "mobile-puncture-repair", SuggestedStartingPrice = 1500m, Description = "On-site roadside puncture repair" },
                                new() { Name = "Tire Replacement", Slug = "tire-replacement", SuggestedStartingPrice = 2000m, Description = "Tire fitting and wheel balancing" }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Mechanical",
                    Slug = "mechanical",
                    Description = "Vehicle repair, electronics, computers, and machine servicing",
                    Icon = "construct",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "Vehicle Repair",
                            Slug = "vehicle-repair",
                            Description = "Mechanical diagnostics, engine tuning, electrical repair",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Car Engine Diagnostics", Slug = "car-engine-diagnostics", SuggestedStartingPrice = 4500m, Description = "OBD scanner check and fault troubleshooting" },
                                new() { Name = "Brake Repair & Pad Replacement", Slug = "brake-repair", SuggestedStartingPrice = 3500m, Description = "Brake pad replacement and bleeding" },
                                new() { Name = "Hybrid Battery Service", Slug = "hybrid-battery-service", SuggestedStartingPrice = 18000m, Description = "Hybrid battery cell balancing and diagnostics" }
                            }
                        },
                        new()
                        {
                            Name = "Computer Repair",
                            Slug = "computer-repair",
                            Description = "Laptop repairs, PC maintenance, OS installation",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Laptop Repair", Slug = "laptop-repair", SuggestedStartingPrice = 3000m, Description = "Hardware diagnostics, screen, keyboard, battery repair" },
                                new() { Name = "Desktop PC Troubleshooting", Slug = "desktop-pc-troubleshooting", SuggestedStartingPrice = 2500m, Description = "PC assembly, overheating fix, component replacement" },
                                new() { Name = "OS & Software Installation", Slug = "os-software-installation", SuggestedStartingPrice = 2000m, Description = "Windows/Linux setup, driver updates, virus cleanup" }
                            }
                        },
                        new()
                        {
                            Name = "Machine Repair",
                            Slug = "machine-repair",
                            Description = "Generators, water pumps, domestic equipment",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Water Pump Repair", Slug = "water-pump-repair", SuggestedStartingPrice = 3500m, Description = "Pressure switch, motor winding, impeller fixes" },
                                new() { Name = "Generator Servicing", Slug = "generator-servicing", SuggestedStartingPrice = 6000m, Description = "Oil change, carburetor cleaning, tune-up" }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Cleaning",
                    Slug = "cleaning",
                    Description = "Residential, commercial, and garden cleaning services",
                    Icon = "sparkles",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "House Cleaning",
                            Slug = "house-cleaning",
                            Description = "Deep cleaning, floor scrubbing, post-construction cleanup",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Deep House Cleaning", Slug = "deep-house-cleaning", SuggestedStartingPrice = 8000m, Description = "Complete scrubbing of kitchen, bathrooms, and living areas" },
                                new() { Name = "Sofa & Carpet Shampooing", Slug = "sofa-carpet-shampooing", SuggestedStartingPrice = 5000m, Description = "Wet vacuuming and stain removal for upholstery" }
                            }
                        },
                        new()
                        {
                            Name = "Garden Cleaning",
                            Slug = "garden-cleaning",
                            Description = "Grass cutting, weed removal, tree pruning",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Grass Cutting", Slug = "grass-cutting", SuggestedStartingPrice = 3500m, Description = "Lawn mowing and weed trimming" },
                                new() { Name = "Tree Trimming", Slug = "tree-trimming", SuggestedStartingPrice = 6000m, Description = "Overhanging branch trimming and yard clearing" }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Food Preparation",
                    Slug = "food-preparation",
                    Description = "Home catering, daily meals, desserts and special events",
                    Icon = "restaurant",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "Meal Catering",
                            Slug = "meal-catering",
                            Description = "Event buffets, lunch packs, party snacks",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Lunch Pack Delivery", Slug = "lunch-pack-delivery", SuggestedStartingPrice = 800m, Description = "Freshly prepared rice and curry packets" },
                                new() { Name = "Party Food Catering", Slug = "party-food-catering", SuggestedStartingPrice = 15000m, Description = "Custom buffet preparation for small/medium gatherings" }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Caring",
                    Slug = "caring",
                    Description = "Pet care, elder care, and childcare services",
                    Icon = "heart",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "Pet Care",
                            Slug = "pet-care",
                            Description = "Pet sitting, grooming, and dog walking",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Dog Grooming", Slug = "dog-grooming", SuggestedStartingPrice = 3500m, Description = "Bath, nail clipping, fur trimming" },
                                new() { Name = "Pet Sitting", Slug = "pet-sitting", SuggestedStartingPrice = 2500m, Description = "Feeding, home boarding, dog walking" }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Digital & Professional",
                    Slug = "digital-professional",
                    Description = "Software, design, marketing, and accounting",
                    Icon = "laptop",
                    Services = new List<ServiceEntity>
                    {
                        new()
                        {
                            Name = "Software Development",
                            Slug = "software-development",
                            Description = "Websites, mobile apps, database design",
                            Variants = new List<ServiceVariant>
                            {
                                new() { Name = "Web Application Development", Slug = "web-application-development", SuggestedStartingPrice = 45000m, Description = "Custom web app with modern frameworks" },
                                new() { Name = "WordPress Website Setup", Slug = "wordpress-setup", SuggestedStartingPrice = 20000m, Description = "Quick business website launch" }
                            }
                        }
                    }
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 3. Seed Demo Provider Service Profiles and Service Listings for Negombo & Colombo
        if (!await context.ProviderServiceProfiles.AnyAsync())
        {
            var homeVisitTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "home-visit");
            var mobileServiceTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "mobile-service");
            var weekendTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "weekend");
            var emergencyTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "emergency");
            var certifiedTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "certified-technician");

            var carWashingVariant = await context.ServiceVariants.FirstOrDefaultAsync(v => v.Slug == "car-washing");
            var laptopRepairVariant = await context.ServiceVariants.FirstOrDefaultAsync(v => v.Slug == "laptop-repair");
            var deepCleaningVariant = await context.ServiceVariants.FirstOrDefaultAsync(v => v.Slug == "deep-house-cleaning");

            // Seed Provider 1: Nimal Silva (Car Wash Specialist in Negombo)
            var nimalUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Nimal Silva",
                Email = "nimal.silva@marketplace.lk",
                PhoneNumber = "+94772345678",
                PasswordHash = passwordHasher.HashPassword("Provider123!"),
                NIC = "198523456789",
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    City = "Negombo",
                    Address = "45 Sea Street, Negombo",
                    Bio = "Professional mobile vehicle cleaning with 8+ years experience."
                },
                ServiceProfile = new ProviderServiceProfile
                {
                    BusinessName = "Nimal Mobile Wash & Shine",
                    Bio = "I provide premium doorstep car washing and detailing in Negombo, Katana, and Ja-Ela.",
                    IsVerified = true,
                    VerificationStatus = "Verified",
                    RatingAverage = 4.9,
                    ReviewCount = 38,
                    CompletedJobsCount = 52,
                    ServiceAreas = new List<ProviderServiceArea>
                    {
                        new() { CityName = "Negombo", RadiusKm = 20 },
                        new() { CityName = "Katana", RadiusKm = 15 },
                        new() { CityName = "Ja-Ela", RadiusKm = 15 }
                    }
                }
            };

            await context.Users.AddAsync(nimalUser);
            await context.SaveChangesAsync();

            if (carWashingVariant != null && nimalUser.ServiceProfile != null)
            {
                var nimalCarService = new ProviderService
                {
                    ProviderServiceProfileId = nimalUser.ServiceProfile.Id,
                    ServiceVariantId = carWashingVariant.Id,
                    StartingPrice = 2500m,
                    PriceUnit = "Per Vehicle",
                    Description = "Complete doorstep pressure wash, interior vacuuming, dashboard polish, and tire shine. Available weekends and weekdays.",
                    SupportsHomeVisit = true,
                    IsActive = true
                };

                if (homeVisitTag != null) nimalCarService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = homeVisitTag.Id });
                if (mobileServiceTag != null) nimalCarService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = mobileServiceTag.Id });
                if (weekendTag != null) nimalCarService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = weekendTag.Id });

                await context.ProviderServices.AddAsync(nimalCarService);
            }

            // Seed Provider 2: Priyantha Bandara (Computer Technician in Negombo)
            var priyanthaUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Priyantha Bandara",
                Email = "priyantha.tech@marketplace.lk",
                PhoneNumber = "+94713456789",
                PasswordHash = passwordHasher.HashPassword("Provider123!"),
                NIC = "199234567890",
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    City = "Negombo",
                    Address = "12 Station Road, Negombo",
                    Bio = "Certified IT technician with fast on-site laptop and desktop troubleshooting."
                },
                ServiceProfile = new ProviderServiceProfile
                {
                    BusinessName = "Priyantha Computer Care",
                    Bio = "Specialist in laptop hardware repair, Windows recovery, screen replacement, and motherboard diagnostics.",
                    IsVerified = true,
                    VerificationStatus = "Verified",
                    RatingAverage = 4.8,
                    ReviewCount = 24,
                    CompletedJobsCount = 31,
                    ServiceAreas = new List<ProviderServiceArea>
                    {
                        new() { CityName = "Negombo", RadiusKm = 25 },
                        new() { CityName = "Kochchikade", RadiusKm = 15 },
                        new() { CityName = "Katunayake", RadiusKm = 15 }
                    }
                }
            };

            await context.Users.AddAsync(priyanthaUser);
            await context.SaveChangesAsync();

            if (laptopRepairVariant != null && priyanthaUser.ServiceProfile != null)
            {
                var priyanthaLaptopService = new ProviderService
                {
                    ProviderServiceProfileId = priyanthaUser.ServiceProfile.Id,
                    ServiceVariantId = laptopRepairVariant.Id,
                    StartingPrice = 3000m,
                    PriceUnit = "Per Diagnosis/Repair",
                    Description = "On-site and workshop laptop repair for Dell, Asus, HP, Lenovo, and Mac. Quick turnaround and genuine parts.",
                    SupportsHomeVisit = true,
                    IsActive = true
                };

                if (homeVisitTag != null) priyanthaLaptopService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = homeVisitTag.Id });
                if (certifiedTag != null) priyanthaLaptopService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = certifiedTag.Id });
                if (emergencyTag != null) priyanthaLaptopService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = emergencyTag.Id });

                await context.ProviderServices.AddAsync(priyanthaLaptopService);
            }

            // Seed Provider 3: Kumari Fernando (Home Cleaning in Negombo / Colombo)
            var kumariUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Kumari Fernando",
                Email = "kumari.clean@marketplace.lk",
                PhoneNumber = "+94764567890",
                PasswordHash = passwordHasher.HashPassword("Provider123!"),
                NIC = "198845678901",
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    City = "Negombo",
                    Address = "89 Greens Road, Negombo",
                    Bio = "Leader of an experienced domestic and commercial deep cleaning crew."
                },
                ServiceProfile = new ProviderServiceProfile
                {
                    BusinessName = "CleanCare Pro Services",
                    Bio = "Deep house cleaning, move-in/move-out sanitization, and sofa shampooing with modern equipment.",
                    IsVerified = true,
                    VerificationStatus = "Verified",
                    RatingAverage = 5.0,
                    ReviewCount = 42,
                    CompletedJobsCount = 60,
                    ServiceAreas = new List<ProviderServiceArea>
                    {
                        new() { CityName = "Negombo", RadiusKm = 30 },
                        new() { CityName = "Ja-Ela", RadiusKm = 20 },
                        new() { CityName = "Colombo", RadiusKm = 25 }
                    }
                }
            };

            await context.Users.AddAsync(kumariUser);
            await context.SaveChangesAsync();

            if (deepCleaningVariant != null && kumariUser.ServiceProfile != null)
            {
                var kumariCleanService = new ProviderService
                {
                    ProviderServiceProfileId = kumariUser.ServiceProfile.Id,
                    ServiceVariantId = deepCleaningVariant.Id,
                    StartingPrice = 8500m,
                    PriceUnit = "Per Job",
                    Description = "Full deep house cleaning including bathroom descaling, kitchen degreasing, glass washing, and floor buffing.",
                    SupportsHomeVisit = true,
                    IsActive = true
                };

                if (homeVisitTag != null) kumariCleanService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = homeVisitTag.Id });
                if (weekendTag != null) kumariCleanService.ProviderServiceTags.Add(new ProviderServiceTag { TagId = weekendTag.Id });

                await context.ProviderServices.AddAsync(kumariCleanService);
            }

            await context.SaveChangesAsync();
        }
    }
}
