using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using SchoolApp.Core;
using SchoolApp.Core.Filters;
using SchoolApp.DTO;
using SchoolApp.Exceptions;
using SchoolApp.Models;
using SchoolApp.Repositories;
using SchoolApp.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace SchoolApp.Services
{  
    public class UserService : IUserService
    {
        private readonly IEncryptionUtil _encryptionUtil;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, 
            ILogger<UserService> logger, IEncryptionUtil encryptionUtil, IConfiguration configuration)
        {
            _encryptionUtil = encryptionUtil;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserReadOnlyDTO> GetUserByUsernameAsync(string username)
        {
            
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                throw new EntityNotFoundException("User", $"User with username: {username} not found");
            }

            _logger.LogInformation("User found: {Username}", username);
            return _mapper.Map<UserReadOnlyDTO>(user);
        }

        public async Task<UserReadOnlyDTO> GetUserByIdAsync(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new EntityNotFoundException("User", $"User with id {id} not found");
            }

            _logger.LogInformation("User with id {Id} found", id);
            return _mapper.Map<UserReadOnlyDTO>(user);
        }

        public async Task<PaginatedResult<UserReadOnlyDTO>> GetPaginatedUsersFilteredAsync(
            int pageNumber, int pageSize, UserFiltersDTO userFiltersDTO)
        {
            //List<User> users = [];
            List<Expression<Func<User, bool>>> predicates = [];

            if (!string.IsNullOrEmpty(userFiltersDTO.Username))
            {
                predicates.Add(u => u.Username == userFiltersDTO.Username);
            }
            if (!string.IsNullOrEmpty(userFiltersDTO.Email))
            {
                predicates.Add(u => u.Email == userFiltersDTO.Email);
            }
            if (!string.IsNullOrEmpty(userFiltersDTO.UserRole))
            {
                predicates.Add(u => u.Role.Name == userFiltersDTO.UserRole);
            }

            var result = await _unitOfWork.UserRepository.GetUsersAsync(pageNumber, pageSize, 
                predicates);

            var dtoResult = new PaginatedResult<UserReadOnlyDTO>()
            {
                Data = _mapper.Map<List<UserReadOnlyDTO>>(result.Data),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };

            _logger.LogInformation("Retrieved {Count} users", dtoResult.Data.Count);
            return dtoResult;
        }

        public async Task<User> VerifyAndGetUserAsync(UserLoginDTO credentials)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(credentials.Username);

            if (user == null || !_encryptionUtil.IsValidPassword(credentials.Password, user.Password))
            {
                //throw new EntityNotAuthorizedException("User", Resources.ErrorMessages.BadCredentials);
                throw new EntityNotAuthorizedException("User", "Bad Credentials");
            }

            _logger.LogInformation("User with username {Username} verified for login", credentials.Username);
            return user; 
        }

        public string CreateUserToken(User user)
        {
            var secretKey = _configuration["Jwt:Secret"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimsInfo = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claimsInfo,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: signingCredentials
            );

            // Serialize the token to a string
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}
