using Moq;
using Stemkit.Data;
using Stemkit.Models;
using Stemkit.Repositories.Interfaces;
using Stemkit.Utils.Interfaces;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Stemkit.Services.Interfaces;

namespace Stemkit.Tests.Helpers
{
    public static class MockHelper
    {
        // Extension Method for IGenericRepository<User>
        public static void SetupUserRepository(
            this Mock<IGenericRepository<User>> userRepoMock,
            User user)
        {
            userRepoMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(user);
        }

        // Extension Method for IGenericRepository<UserRole>
        public static void SetupUserRoleRepository(
            this Mock<IGenericRepository<UserRole>> userRoleRepoMock,
            List<UserRole> userRoles)
        {
            userRoleRepoMock.Setup(repo => repo.FindAsync(
                    It.IsAny<Expression<Func<UserRole, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(userRoles);
        }

        // Extension Method for IGenericRepository<Role>
        public static void SetupRoleRepository(
            this Mock<IGenericRepository<Role>> roleRepoMock,
            Role role)
        {
            roleRepoMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<Role, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(role);
        }

        // Extension Method for IGenericRepository<RefreshToken>
        public static void SetupRefreshTokenRepository(
            this Mock<IGenericRepository<RefreshToken>> refreshTokenRepoMock)
        {
            refreshTokenRepoMock.Setup(repo => repo.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);
        }

        // Extension Method for IRefreshTokenRepository (Specific Repository)
        public static void SetupRefreshTokenRepository(
            this Mock<IRefreshTokenRepository> refreshTokenRepoMock)
        {
            refreshTokenRepoMock.Setup(repo => repo.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);
        }

        // Extension Method for IJwtTokenGenerator
        public static void SetupJwtTokenGenerator(
            this Mock<IJwtTokenGenerator> jwtMock,
            int userId,
            List<string> roles,
            string jwtToken,
            string refreshToken)
        {
            jwtMock.Setup(gen => gen.GenerateJwtToken(
                    userId,
                    It.IsAny<List<string>>()))
                .Returns(jwtToken);

            jwtMock.Setup(gen => gen.GenerateRefreshToken(
                    userId,
                    It.IsAny<string>()))
                .Returns(new RefreshToken
                {
                    Token = refreshToken,
                    UserId = userId,
                    ExpirationTime = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = "127.0.0.1"
                });
        }

        // Extension Method for IUnitOfWork's CompleteAsync
        public static void SetupUnitOfWorkCompleteAsync(
            this Mock<IUnitOfWork> unitOfWorkMock,
            int returnValue = 1)
        {
            unitOfWorkMock.Setup(uow => uow.CompleteAsync())
                .ReturnsAsync(returnValue);
        }

        // Extension Method for IGoogleTokenValidator
        public static void SetupValidateAsync(
            this Mock<IGoogleTokenValidator> googleValidatorMock,
            string idToken,
            GoogleJsonWebSignature.Payload payload)
        {
            googleValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
                .ReturnsAsync(payload);
        }

        // Extension Method for IUserService.GetUserByEmailAsync
        public static void SetupGetUserByEmailAsync(
            this Mock<IUserService> userServiceMock,
            string email,
            User user)
        {
            userServiceMock.Setup(service => service.GetUserByEmailAsync(email))
                .ReturnsAsync(user);
        }

        // Extension Method for IUserService.CreateUserAsync
        public static void SetupCreateUserAsync(
            this Mock<IUserService> userServiceMock,
            Func<User, Task<User>> createUserFunc)
        {
            userServiceMock.Setup(service => service.CreateUserAsync(It.IsAny<User>()))
                .Returns(createUserFunc);
        }

        // Extension Method for IUserService.AssignRoleAsync
        public static void SetupAssignRoleAsync(
            this Mock<IUserService> userServiceMock,
            int userId,
            string roleName)
        {
            userServiceMock.Setup(service => service.AssignRoleAsync(userId, roleName))
                .Returns(Task.CompletedTask);
        }

        // Extension Method for IUserService.GetUserRolesAsync
        public static void SetupGetUserRolesAsync(
            this Mock<IUserService> userServiceMock,
            int userId,
            List<string> roles)
        {
            userServiceMock.Setup(service => service.GetUserRolesAsync(userId))
                .ReturnsAsync(roles);
        }

        /// <summary>
        /// Sets up the GetByIdAsync method of the repository to return a specified entity.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="repoMock">The mock repository.</param>
        /// <param name="id">The ID to match.</param>
        /// <param name="entity">The entity to return.</param>
        public static void SetupGetByIdAsync<T>(
            this Mock<IGenericRepository<T>> repoMock,
            int id,
            T entity) where T : class
        {
            repoMock.Setup(repo => repo.GetByIdAsync(id))
                    .ReturnsAsync(entity);
        }
    }
}
