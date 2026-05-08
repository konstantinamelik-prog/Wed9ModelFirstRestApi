using AutoMapper;

using SchoolApp.DTO;
using SchoolApp.Exceptions;
using SchoolApp.Models;
using SchoolApp.Repositories;
using SchoolApp.Security;


namespace SchoolApp.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEncryptionUtil _encryptionUtil;
        private readonly ILogger<TeacherService> _logger;

        public TeacherService(IUnitOfWork unitOfWork, IMapper mapper, 
            ILogger<TeacherService> logger, IEncryptionUtil encryptionUtil)
        {
            _encryptionUtil = encryptionUtil;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserReadOnlyDTO> SignUpUserAsync(TeacherSignupDTO request)
        {
            var teacher = _mapper.Map<Teacher>(request);
            var user = _mapper.Map<User>(request);
           
            
            var existingUser = await _unitOfWork.UserRepository.GetUserByUsernameAsync(user.Username);

            if (existingUser != null)
            {
                throw new EntityAlreadyExistsException("User", $"User with username {existingUser.Username} already exists");
            }

            user.Teacher = teacher;
            user.Password = _encryptionUtil.Encrypt(user.Password);
            await _unitOfWork.UserRepository.AddAsync(user);
            //await _unitOfWork.TeacherRepository.AddAsync(teacher); // No need to add teacher separately as it will be added through the User entity

            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Teacher {Username} signed up successfully.", user.Username);
            return _mapper.Map<UserReadOnlyDTO>(user);
        }
    }
}
