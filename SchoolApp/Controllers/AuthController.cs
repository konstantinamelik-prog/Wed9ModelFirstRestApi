using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolApp.DTO;
using SchoolApp.Exceptions;
using SchoolApp.Services;

namespace SchoolApp.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly IConfiguration _configuration;

        public AuthController(IApplicationService applicationService, IConfiguration configuration)
        {
            _applicationService = applicationService;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new teacher account.
        /// </summary>
        [HttpPost("register/teacher")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserReadOnlyDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserReadOnlyDTO>> RegisterTeacher(
            [FromBody] TeacherSignupDTO teacherSignupDTO)
        {
            var createdUser = await _applicationService.TeacherService
                .SignUpUserAsync(teacherSignupDTO);

            return CreatedAtAction(
                actionName: nameof(UsersController.GetUserById),
                controllerName: "Users",
                routeValues: new { id = createdUser.Id },
                value: createdUser);
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(JwtTokenDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<JwtTokenDTO>> Login(
            [FromBody] UserLoginDTO credentials)
        {
            var user = await _applicationService.UserService
                .VerifyAndGetUserAsync(credentials)
                ?? throw new EntityNotAuthorizedException("User", "Bad Credentials");

            var token = _applicationService.UserService.CreateUserToken(user);

            return Ok(new JwtTokenDTO(token));
        }

    }
}