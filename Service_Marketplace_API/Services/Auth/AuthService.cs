using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Data;
using Service_Marketplace_API.DTOs.Auth;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.Entities;
using Service_Marketplace_API.Entities.Enums;
using Service_Marketplace_API.Services.Common;

namespace Service_Marketplace_API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var trimmedPhone = request.PhoneNumber.Trim();

            // 1. Check if email already exists
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingEmail)
            {
                return ApiResponse<AuthResponseDto>.Fail("A user with this email address already exists.");
            }

            // 2. Check if phone number already exists
            var existingPhone = await _context.Users
                .AnyAsync(u => u.PhoneNumber == trimmedPhone);

            if (existingPhone)
            {
                return ApiResponse<AuthResponseDto>.Fail("A user with this phone number already exists.");
            }

            // 3. Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 4. Create user (Single User Account Principle)
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = trimmedPhone,
                PasswordHash = passwordHash,
                NIC = string.IsNullOrWhiteSpace(request.NIC) ? null : request.NIC.Trim(),
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    City = request.City?.Trim(),
                    Address = request.Address?.Trim(),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered successfully: {UserId}, Email: {Email}", user.Id, user.Email);

            // 5. Generate JWT token
            var (token, expiresAt) = _tokenService.GenerateJwtToken(user);

            var authResponse = new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                RoleId = (int)user.Role,
                Status = user.Status.ToString(),
                HasServiceProfile = false,
                ExpiresAt = expiresAt
            };

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Registration successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user registration.");
            return ApiResponse<AuthResponseDto>.Fail("An unexpected error occurred during registration. Please try again.");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Find user by email
            var user = await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ServiceProfile)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");
            }

            // 2. Verify password
            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Failed login attempt for user: {Email}", user.Email);
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");
            }

            // 3. Check account status
            if (user.Status == UserStatus.Suspended)
            {
                return ApiResponse<AuthResponseDto>.Fail("Your account is suspended. Please contact support.");
            }

            if (user.Status == UserStatus.Deactivated)
            {
                return ApiResponse<AuthResponseDto>.Fail("Your account has been deactivated.");
            }

            // 4. Generate JWT token
            var (token, expiresAt) = _tokenService.GenerateJwtToken(user);

            var authResponse = new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                RoleId = (int)user.Role,
                Status = user.Status.ToString(),
                HasServiceProfile = user.ServiceProfile != null,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("User logged in successfully: {UserId}, Email: {Email}", user.Id, user.Email);

            return ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user login.");
            return ApiResponse<AuthResponseDto>.Fail("An unexpected error occurred during login. Please try again.");
        }
    }

    public async Task<ApiResponse<List<UserResponseDto>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ServiceProfile)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    MaskedNIC = MaskNIC(u.NIC),
                    City = u.Profile != null ? u.Profile.City : null,
                    Address = u.Profile != null ? u.Profile.Address : null,
                    Role = u.Role.ToString(),
                    RoleId = (int)u.Role,
                    Status = u.Status.ToString(),
                    HasServiceProfile = u.ServiceProfile != null,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<UserResponseDto>>.Ok(users, "Users retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving users.");
            return ApiResponse<List<UserResponseDto>>.Fail("An unexpected error occurred while retrieving users.");
        }
    }

    public async Task<ApiResponse<UserResponseDto>> CreateAdminAsync(CreateAdminRequestDto request)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var trimmedPhone = request.PhoneNumber.Trim();

            // 1. Check if email already exists
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingEmail)
            {
                return ApiResponse<UserResponseDto>.Fail("A user with this email address already exists.");
            }

            // 2. Check if phone number already exists
            var existingPhone = await _context.Users
                .AnyAsync(u => u.PhoneNumber == trimmedPhone);

            if (existingPhone)
            {
                return ApiResponse<UserResponseDto>.Fail("A user with this phone number already exists.");
            }

            // 3. Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 4. Create Admin User (hard-coded Role = UserRole.Admin / 2)
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = trimmedPhone,
                PasswordHash = passwordHash,
                NIC = string.IsNullOrWhiteSpace(request.NIC) ? null : request.NIC.Trim(),
                Role = UserRole.Admin, // Hard-coded user type 2
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = !string.IsNullOrWhiteSpace(request.Department) ? $"Admin Department: {request.Department.Trim()}" : "Platform Administrator",
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New admin user created successfully: {UserId}, Email: {Email}", adminUser.Id, adminUser.Email);

            var responseDto = new UserResponseDto
            {
                Id = adminUser.Id,
                FullName = adminUser.FullName,
                Email = adminUser.Email,
                PhoneNumber = adminUser.PhoneNumber,
                MaskedNIC = MaskNIC(adminUser.NIC),
                Role = adminUser.Role.ToString(),
                RoleId = (int)adminUser.Role,
                Status = adminUser.Status.ToString(),
                HasServiceProfile = false,
                CreatedAt = adminUser.CreatedAt
            };

            return ApiResponse<UserResponseDto>.Ok(responseDto, "Admin user created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating an admin user.");
            return ApiResponse<UserResponseDto>.Fail("An unexpected error occurred while creating the admin user.");
        }
    }

    public async Task<ApiResponse<ServiceProfileResponseDto>> CreateServiceProfileAsync(Guid userId, CreateServiceProfileDto dto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.ServiceProfile)
                    .ThenInclude(sp => sp!.ServiceAreas)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<ServiceProfileResponseDto>.Fail("User not found.");
            }

            // Exclude general admin accounts from service profile
            if (user.Role == UserRole.Admin)
            {
                return ApiResponse<ServiceProfileResponseDto>.Fail("Administrator accounts cannot have a service profile.");
            }

            if (user.ServiceProfile != null)
            {
                return ApiResponse<ServiceProfileResponseDto>.Fail("Service profile already exists. Use updateServiceProfile instead.");
            }

            user.ServiceProfile = new ProviderServiceProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BusinessName = !string.IsNullOrWhiteSpace(dto.BusinessName) ? dto.BusinessName.Trim() : user.FullName,
                BusinessRegistrationNumber = dto.BusinessRegistrationNumber?.Trim(),
                Bio = dto.Bio?.Trim(),
                IsVerified = false,
                VerificationStatus = "Pending",
                RatingAverage = 5.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (dto.ServiceAreaCities != null && dto.ServiceAreaCities.Any())
            {
                foreach (var city in dto.ServiceAreaCities.Distinct())
                {
                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        user.ServiceProfile.ServiceAreas.Add(new ProviderServiceArea
                        {
                            CityName = city.Trim(),
                            RadiusKm = 15
                        });
                    }
                }
            }

            await _context.ProviderServiceProfiles.AddAsync(user.ServiceProfile);
            await _context.SaveChangesAsync();

            var responseDto = new ServiceProfileResponseDto
            {
                Id = user.ServiceProfile.Id,
                UserId = user.Id,
                UserFullName = user.FullName,
                BusinessName = user.ServiceProfile.BusinessName,
                BusinessRegistrationNumber = user.ServiceProfile.BusinessRegistrationNumber,
                Bio = user.ServiceProfile.Bio,
                IsVerified = user.ServiceProfile.IsVerified,
                VerificationStatus = user.ServiceProfile.VerificationStatus,
                RatingAverage = user.ServiceProfile.RatingAverage,
                ReviewCount = user.ServiceProfile.ReviewCount,
                CompletedJobsCount = user.ServiceProfile.CompletedJobsCount,
                ServiceAreas = user.ServiceProfile.ServiceAreas.Select(sa => sa.CityName).ToList(),
                UpdatedAt = user.ServiceProfile.UpdatedAt ?? DateTime.UtcNow
            };

            return ApiResponse<ServiceProfileResponseDto>.Ok(responseDto, "Service profile created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating service profile.");
            return ApiResponse<ServiceProfileResponseDto>.Fail("An unexpected error occurred while creating service profile.");
        }
    }

    public async Task<ApiResponse<ServiceProfileResponseDto>> UpdateServiceProfileAsync(Guid userId, UpdateServiceProfileDto dto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.ServiceProfile)
                    .ThenInclude(sp => sp!.ServiceAreas)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<ServiceProfileResponseDto>.Fail("User not found.");
            }

            // Exclude general admin accounts from service profile
            if (user.Role == UserRole.Admin)
            {
                return ApiResponse<ServiceProfileResponseDto>.Fail("Administrator accounts cannot have a service profile.");
            }

            if (user.ServiceProfile == null)
            {
                // Auto-create service profile if not exists for smooth experience
                user.ServiceProfile = new ProviderServiceProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    BusinessName = !string.IsNullOrWhiteSpace(dto.BusinessName) ? dto.BusinessName.Trim() : user.FullName,
                    BusinessRegistrationNumber = dto.BusinessRegistrationNumber?.Trim(),
                    Bio = dto.Bio?.Trim(),
                    IsVerified = false,
                    VerificationStatus = "Pending",
                    RatingAverage = 5.0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.ProviderServiceProfiles.AddAsync(user.ServiceProfile);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dto.BusinessName))
                    user.ServiceProfile.BusinessName = dto.BusinessName.Trim();

                if (dto.BusinessRegistrationNumber != null)
                    user.ServiceProfile.BusinessRegistrationNumber = dto.BusinessRegistrationNumber.Trim();

                if (dto.Bio != null)
                    user.ServiceProfile.Bio = dto.Bio.Trim();

                user.ServiceProfile.UpdatedAt = DateTime.UtcNow;
            }

            if (dto.ServiceAreaCities != null && dto.ServiceAreaCities.Any())
            {
                foreach (var city in dto.ServiceAreaCities.Distinct())
                {
                    if (!string.IsNullOrWhiteSpace(city) && !user.ServiceProfile.ServiceAreas.Any(sa => sa.CityName.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        var area = new ProviderServiceArea
                        {
                            Id = Guid.NewGuid(),
                            ProviderServiceProfileId = user.ServiceProfile.Id,
                            CityName = city.Trim(),
                            RadiusKm = 15
                        };
                        await _context.ProviderServiceAreas.AddAsync(area);
                        user.ServiceProfile.ServiceAreas.Add(area);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var responseDto = new ServiceProfileResponseDto
            {
                Id = user.ServiceProfile.Id,
                UserId = user.Id,
                UserFullName = user.FullName,
                BusinessName = user.ServiceProfile.BusinessName,
                BusinessRegistrationNumber = user.ServiceProfile.BusinessRegistrationNumber,
                Bio = user.ServiceProfile.Bio,
                IsVerified = user.ServiceProfile.IsVerified,
                VerificationStatus = user.ServiceProfile.VerificationStatus,
                RatingAverage = user.ServiceProfile.RatingAverage,
                ReviewCount = user.ServiceProfile.ReviewCount,
                CompletedJobsCount = user.ServiceProfile.CompletedJobsCount,
                ServiceAreas = user.ServiceProfile.ServiceAreas.Select(sa => sa.CityName).ToList(),
                UpdatedAt = user.ServiceProfile.UpdatedAt ?? DateTime.UtcNow
            };

            return ApiResponse<ServiceProfileResponseDto>.Ok(responseDto, "Service profile updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating service profile.");
            return ApiResponse<ServiceProfileResponseDto>.Fail("An unexpected error occurred while updating service profile.");
        }
    }

    private static string? MaskNIC(string? nic)
    {
        if (string.IsNullOrWhiteSpace(nic))
            return null;

        var trimmed = nic.Trim();
        if (trimmed.Length <= 4)
            return new string('*', trimmed.Length);

        var start = trimmed[..2];
        var end = trimmed[^2..];
        var maskedMiddle = new string('*', Math.Max(trimmed.Length - 4, 3));
        return $"{start}{maskedMiddle}{end}";
    }
}
